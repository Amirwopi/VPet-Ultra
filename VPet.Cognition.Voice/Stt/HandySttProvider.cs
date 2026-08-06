using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Voice.Stt;

/// <summary>
/// <see cref="ISttProvider"/> that wraps the standalone <c>vpet-transcribe.exe</c> CLI.
/// vpet-transcribe is built from the <c>transcribe-cpp</c> Rust crate (same engine as Handy)
/// and supports GGUF v3 models + CPU backends. See: tools/vpet-transcribe/
/// </summary>
public sealed class HandySttProvider : ISttProvider
{
    public string ProviderId => "handy";

    private readonly string _executablePath;
    private readonly string _modelPath;
    private readonly string _language;
    private readonly ILogger<HandySttProvider> _logger;
    private CancellationTokenSource? _cts;

#pragma warning disable CS0067 // Events are part of the interface contract; invoked by subclasses/extensions
    public event Action<TranscriptChunk>? PartialTranscript;
    public event Action<TranscriptChunk>? FinalTranscript;
    public event Action? VoiceActivityDetected;
    public event Action? VoiceActivityEnded;
#pragma warning restore CS0067

    public HandySttProvider(
        string executablePath,
        string modelPath,
        string language,
        ILogger<HandySttProvider> logger)
    {
        _executablePath = executablePath;
        _modelPath = modelPath;
        _language = language;
        _logger = logger;
    }

    public Task StartStreamingAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Background loop: watch a temp directory for WAV chunks and transcribe them.
        _ = Task.Run(async () =>
        {
            var tempDir = Path.Combine(Path.GetTempPath(), "vpet_stt");
            Directory.CreateDirectory(tempDir);

            _logger.LogInformation(
                "Handy STT streaming started. Exe={Exe}, Model={Model}, Lang={Lang}, TempDir={Dir}",
                _executablePath, _modelPath, _language, tempDir);

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var wavFiles = Directory.GetFiles(tempDir, "*.wav");
                    foreach (var wavFile in wavFiles)
                    {
                        try
                        {
                            var transcript = await TranscribeFileAsync(wavFile, _cts.Token);
                            PartialTranscript?.Invoke(transcript);
                            FinalTranscript?.Invoke(transcript);
                            try { File.Delete(wavFile); } catch { /* ignore */ }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to transcribe {File}", wavFile);
                            try { File.Delete(wavFile); } catch { /* ignore */ }
                        }
                    }

                    await Task.Delay(100, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "STT streaming loop error.");
                    await Task.Delay(1000, _cts.Token);
                }
            }

            _logger.LogInformation("Handy STT streaming stopped.");
        }, _cts.Token);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Transcribe a complete audio file using <c>vpet-transcribe.exe</c>.
    /// CLI format: vpet-transcribe &lt;model_path&gt; &lt;wav_path&gt; [language] --json
    /// Expects 16 kHz mono 16-bit PCM WAV.
    /// </summary>
    public async Task<TranscriptChunk> TranscribeFileAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");
        if (!File.Exists(_modelPath))
            throw new FileNotFoundException($"Model file not found: {_modelPath}");

        // Build arguments: <model_path> <wav_path> [language] --json
        var args = new StringBuilder();
        args.Append($"\"{_modelPath}\" \"{audioFilePath}\"");
        if (!string.IsNullOrWhiteSpace(_language) && _language != "auto")
            args.Append($" {_language}");
        args.Append(" --json");

        _logger.LogInformation("Running vpet-transcribe: {Exe} {Args}", _executablePath, args);

        var psi = new ProcessStartInfo
        {
            FileName = _executablePath,
            Arguments = args.ToString(),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        // Set working directory to the exe's folder so it can find ggml DLLs
        var exeDir = Path.GetDirectoryName(_executablePath);
        if (!string.IsNullOrEmpty(exeDir))
            psi.WorkingDirectory = exeDir;

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        // Model load + transcription can take a while on first run; allow 60s.
        cts.CancelAfter(TimeSpan.FromSeconds(60));

        // Read stdout as raw bytes then decode as UTF-8 to avoid encoding issues
        // with Persian/Arabic text (the default console encoding may not be UTF-8).
        var stdoutBytes = new List<byte>();
        var stderrBytes = new List<byte>();
        var stdoutTask = Task.Run(async () =>
        {
            using var stream = process.StandardOutput.BaseStream;
            var buffer = new byte[4096];
            int read;
            while ((read = await stream.ReadAsync(buffer, cts.Token)) > 0)
                stdoutBytes.AddRange(buffer.AsSpan(0, read));
        }, cts.Token);
        var stderrTask = Task.Run(async () =>
        {
            using var stream = process.StandardError.BaseStream;
            var buffer = new byte[4096];
            int read;
            while ((read = await stream.ReadAsync(buffer, cts.Token)) > 0)
                stderrBytes.AddRange(buffer.AsSpan(0, read));
        }, cts.Token);
        await process.WaitForExitAsync(cts.Token);
        await stdoutTask;
        await stderrTask;
        var stdout = Encoding.UTF8.GetString(stdoutBytes.ToArray());
        var stderr = Encoding.UTF8.GetString(stderrBytes.ToArray());

        if (process.ExitCode != 0)
        {
            _logger.LogError("vpet-transcribe exited {Code}. Stderr: {Err}", process.ExitCode,
                stderr.Length > 500 ? stderr[..500] + "..." : stderr);
            throw new InvalidOperationException($"vpet-transcribe exited with code {process.ExitCode}");
        }

        var text = ParseHandyJson(stdout);
        _logger.LogInformation("Handy transcribed {File} -> \"{Text}\"", audioFilePath, text);

        var chunk = new TranscriptChunk(text, 1.0, TimeSpan.Zero, TimeSpan.Zero);
        FinalTranscript?.Invoke(chunk);
        return chunk;
    }

    /// <summary>
    /// Parse the JSON object emitted by <c>handy.exe --json</c>.
    /// Shape: {"model":..., "text":"...", "audio_secs":..., "best_ms":...}
    /// </summary>
    private static string ParseHandyJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("text", out var textEl))
                return textEl.GetString()?.Trim() ?? string.Empty;
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return the raw output trimmed (handy may print
            // non-JSON lines on stderr/stdout in edge cases).
        }

        return json.Trim();
    }

    public Task StopStreamingAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _logger.LogInformation("Handy STT streaming stopped.");
        return Task.CompletedTask;
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_executablePath) || !File.Exists(_modelPath))
                return Task.FromResult(false);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        return ValueTask.CompletedTask;
    }
}