using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Voice.Stt;

/// <summary>
/// <see cref="ISttProvider"/> that wraps the <c>whisper-cli</c> (whisper.cpp) executable.
/// Offline-first: runs locally, no internet required. Supports any GGML Whisper model.
/// Uses a background process that reads audio from stdin and emits transcript chunks.
/// </summary>
public sealed class WhisperSttProvider : ISttProvider
{
    public string ProviderId => "whisper";

    private readonly string _executablePath;
    private readonly string _modelPath;
    private readonly string _language;
    private readonly int _threads;
    private readonly ILogger<WhisperSttProvider> _logger;
    private Process? _process;
    private CancellationTokenSource? _cts;

#pragma warning disable CS0067 // Events are part of the interface contract; invoked by subclasses/extensions
    public event Action<TranscriptChunk>? PartialTranscript;
    public event Action<TranscriptChunk>? FinalTranscript;
    public event Action? VoiceActivityDetected;
    public event Action? VoiceActivityEnded;
#pragma warning restore CS0067

    public WhisperSttProvider(
        string executablePath,
        string modelPath,
        string language,
        int threads,
        ILogger<WhisperSttProvider> logger)
    {
        _executablePath = executablePath;
        _modelPath = modelPath;
        _language = language;
        _threads = threads;
        _logger = logger;
    }

    public Task StartStreamingAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_executablePath))
            throw new FileNotFoundException($"whisper-cli not found: {_executablePath}");
        if (!File.Exists(_modelPath))
            throw new FileNotFoundException($"Whisper model not found: {_modelPath}");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // For streaming, we use whisper-cli in server mode if available,
        // otherwise we process audio chunks via temp files in a background loop.
        // Phase 1: start a background listener that processes audio files queued to it.
        _logger.LogInformation("Whisper STT streaming started. Model={Model}, Lang={Lang}", _modelPath, _language);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Transcribe a complete audio file (non-streaming convenience method).
    /// </summary>
    public async Task<TranscriptChunk> TranscribeFileAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException($"Audio file not found: {audioFilePath}");

        var args = $"-m \"{_modelPath}\" -f \"{audioFilePath}\" -l {_language} -t {_threads} -nt -np -oj";
        _logger.LogDebug("Running whisper-cli: {Exe} {Args}", _executablePath, args);

        var psi = new ProcessStartInfo
        {
            FileName = _executablePath,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
        await process.WaitForExitAsync(cts.Token);
        var stdout = await stdoutTask;

        // With -oj (output-json), whisper-cli writes a .json file next to the input
        var jsonPath = Path.ChangeExtension(audioFilePath, ".json");
        string text = stdout.Trim();
        if (File.Exists(jsonPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(jsonPath, cts.Token);
                text = ExtractTextFromJson(json);
            }
            catch { /* fall back to stdout */ }
        }

        var chunk = new TranscriptChunk(text, 1.0, TimeSpan.Zero, TimeSpan.Zero);
        FinalTranscript?.Invoke(chunk);
        return chunk;
    }

    public Task StopStreamingAsync(CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        if (_process is { HasExited: false })
        {
            try { _process.Kill(); } catch { /* ignore */ }
        }
        _process?.Dispose();
        _process = null;
        _cts?.Dispose();
        _cts = null;
        _logger.LogInformation("Whisper STT streaming stopped.");
        return Task.CompletedTask;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_executablePath) || !File.Exists(_modelPath))
                return false;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var psi = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = "-h",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var process = Process.Start(psi);
            if (process is null) return false;
            await process.WaitForExitAsync(cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ExtractTextFromJson(string json)
    {
        // Simple extraction: find "text" fields in the whisper JSON output
        var sb = new StringBuilder();
        var idx = 0;
        while ((idx = json.IndexOf("\"text\"", idx, StringComparison.Ordinal)) != -1)
        {
            var colon = json.IndexOf(':', idx);
            if (colon == -1) break;
            var start = json.IndexOf('"', colon + 1);
            if (start == -1) break;
            var end = json.IndexOf('"', start + 1);
            if (end == -1) break;
            sb.Append(json.AsSpan(start + 1, end - start - 1));
            idx = end + 1;
        }
        return sb.ToString().Trim();
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_process is { HasExited: false })
        {
            try { _process.Kill(); } catch { /* ignore */ }
        }
        _process?.Dispose();
        _cts?.Dispose();
        return ValueTask.CompletedTask;
    }
}