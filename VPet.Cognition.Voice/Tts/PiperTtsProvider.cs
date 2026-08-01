using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;

namespace VPet.Cognition.Voice.Tts;

/// <summary>
/// <see cref="ITtsProvider"/> that wraps the <c>piper</c> CLI (rhasspy/piper).
/// Offline-first: runs locally with ONNX voice models. No internet required.
/// Synthesizes to a temp WAV file, then streams PCM chunks back.
/// </summary>
public sealed class PiperTtsProvider : ITtsProvider
{
    public string ProviderId => "piper";

    private readonly string _executablePath;
    private readonly string _modelPath;
    private readonly int? _speakerId;
    private readonly double _lengthScale;
    private readonly double _noiseScale;
    private readonly double _noiseW;
    private readonly TimeSpan _timeout;
    private readonly ILogger<PiperTtsProvider> _logger;

    public PiperTtsProvider(
        string executablePath,
        string modelPath,
        int? speakerId,
        double lengthScale,
        double noiseScale,
        double noiseW,
        TimeSpan timeout,
        ILogger<PiperTtsProvider> logger)
    {
        _executablePath = executablePath;
        _modelPath = modelPath;
        _speakerId = speakerId;
        _lengthScale = lengthScale;
        _noiseScale = noiseScale;
        _noiseW = noiseW;
        _timeout = timeout;
        _logger = logger;
    }

    public async IAsyncEnumerable<AudioChunk> SynthesizeAsync(
        string text, VoiceProsody prosody, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_executablePath) || !File.Exists(_modelPath))
        {
            _logger.LogError("Piper executable or model not found");
            yield break;
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"vpet_piper_{Guid.NewGuid():N}.wav");
        List<AudioChunk> chunks = new();

        try
        {
            var effectiveLengthScale = _lengthScale / Math.Max(0.1, prosody.SpeedMultiplier);

            var args = $"-m \"{_modelPath}\" -f \"{outputPath}\"";
            if (_speakerId.HasValue)
                args += $" -s {_speakerId.Value}";
            args += $" --length-scale {effectiveLengthScale} --noise-scale {_noiseScale} --noise-w {_noiseW}";

            var psi = new ProcessStartInfo
            {
                FileName = _executablePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardInputEncoding = Encoding.UTF8,
            };

            using var process = new Process { StartInfo = psi };
            process.Start();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_timeout);

            await process.StandardInput.WriteLineAsync(text.AsMemory(), cts.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(cts.Token);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(cts.Token);
                _logger.LogError("piper exited {Code}: {Stderr}", process.ExitCode, stderr);
                yield break;
            }

            if (!File.Exists(outputPath))
            {
                _logger.LogError("Piper output file not created: {Path}", outputPath);
                yield break;
            }

            await using var fs = File.OpenRead(outputPath);
            var header = new byte[44];
            await fs.ReadExactlyAsync(header, cancellationToken);

            const int ChunkSize = 4096;
            var buffer = new byte[ChunkSize];
            int read;
            while ((read = await fs.ReadAsync(buffer, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                chunks.Add(new AudioChunk(buffer[..read], 22050, 1, TimeSpan.Zero));
            }

            _logger.LogDebug("Synthesized {Len} chars via piper", text.Length);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Piper synthesis cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Piper synthesis failed");
        }
        finally
        {
            try { if (File.Exists(outputPath)) File.Delete(outputPath); } catch { /* ignore */ }
        }

        foreach (var chunk in chunks)
            yield return chunk;
    }

    public Task<IReadOnlyList<VisemeHint>> GetVisemesAsync(string text, CancellationToken cancellationToken = default)
    {
        // Piper doesn't provide viseme data directly; return empty for now.
        // Future: use phoneme alignment from piper --output-raw
        return Task.FromResult<IReadOnlyList<VisemeHint>>(Array.Empty<VisemeHint>());
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(File.Exists(_executablePath) && File.Exists(_modelPath));
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}