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

        // Clean text for TTS: remove markdown, emojis, and excessive whitespace.
        text = CleanTextForTts(text);
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("TTS text empty after cleaning");
            yield break;
        }

        var outputPath = Path.Combine(Path.GetTempPath(), $"vpet_piper_{Guid.NewGuid():N}.wav");
        List<AudioChunk> chunks = new();

        try
        {
            var effectiveLengthScale = _lengthScale / Math.Max(0.1, prosody.SpeedMultiplier);

            // Piper doesn't support pitch/volume directly, but we log them for future use
            _logger.LogDebug("TTS prosody: speed={Speed:F2} pitch={Pitch:F2} vol={Vol:F2}",
                prosody.SpeedMultiplier, prosody.PitchMultiplier, prosody.VolumeMultiplier);

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
                // Don't set StandardInputEncoding — we write raw UTF-8 bytes to BaseStream.
            };

            // Write text to a temp file as UTF-8 (without BOM), then pipe it to piper via stdin.
            // This avoids Windows console encoding issues with Persian/Arabic text.
            var inputFilePath = Path.Combine(Path.GetTempPath(), $"vpet_piper_input_{Guid.NewGuid():N}.txt");
            var textBytes = Encoding.UTF8.GetBytes(text);
            await File.WriteAllBytesAsync(inputFilePath, textBytes, cancellationToken);

            try
            {
                // Read the file and pipe to piper's stdin as raw bytes
                var fileBytes = await File.ReadAllBytesAsync(inputFilePath, cancellationToken);
                using var process = new Process { StartInfo = psi };
                process.Start();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_timeout);

                // Write raw UTF-8 bytes directly to stdin stream
                await process.StandardInput.BaseStream.WriteAsync(fileBytes, cts.Token);
                await process.StandardInput.BaseStream.FlushAsync(cts.Token);
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

                // Read the entire WAV file (including header) so the caller can play it directly.
                await using var fs = File.OpenRead(outputPath);
                _logger.LogInformation("Piper output: {Path}, size={Size} bytes", outputPath, new FileInfo(outputPath).Length);

                const int ChunkSize = 4096;
                var buffer = new byte[ChunkSize];
                int read;
                while ((read = await fs.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    chunks.Add(new AudioChunk(buffer[..read], 22050, 1, TimeSpan.Zero));
                }

                _logger.LogInformation("Piper synthesized {Len} chars, {Chunks} chunks, total {Bytes} bytes",
                    text.Length, chunks.Count, chunks.Sum(c => c.PcmData.Length));
            }
            finally
            {
                try { if (File.Exists(inputFilePath)) File.Delete(inputFilePath); } catch { /* ignore */ }
            }
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
        // Piper doesn't provide viseme data directly, but we can estimate
        // visemes from the text using a simple phoneme-to-viseme mapping.
        var visemes = new List<VisemeHint>();
        var visemeMap = new Dictionary<char, int>
        {
            {'a', 0}, {'e', 1}, {'i', 2}, {'o', 3}, {'u', 4},
            {'A', 0}, {'E', 1}, {'I', 2}, {'O', 3}, {'U', 4},
            {'b', 0}, {'p', 0}, {'m', 0},
            {'f', 1}, {'v', 1},
            {'t', 2}, {'d', 2}, {'n', 2}, {'l', 2}, {'r', 2},
            {'k', 3}, {'g', 3}, {'s', 3}, {'z', 3},
            {'w', 4}, {'y', 4},
            {' ', -1}, {'.', -1}, {',', -1}, {'!', -1}, {'?', -1},
        };

        // Estimate ~80ms per character (average speaking rate)
        const int msPerChar = 80;
        for (int i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (visemeMap.TryGetValue(c, out var viseme) && viseme >= 0)
            {
                visemes.Add(new VisemeHint(viseme, TimeSpan.FromMilliseconds(i * msPerChar), TimeSpan.FromMilliseconds(msPerChar)));
            }
        }

        return Task.FromResult<IReadOnlyList<VisemeHint>>(visemes);
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(File.Exists(_executablePath) && File.Exists(_modelPath));
    }

    /// <summary>
    /// Remove markdown, emojis, and formatting that Piper can't phonemize.
    /// </summary>
    private static string CleanTextForTts(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove markdown bold/italic/code markers
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*{1,3}", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"`{1,3}", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"_{1,3}", "");

        // Remove markdown links [text](url) -> text
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");

        // Remove markdown headers
        text = System.Text.RegularExpressions.Regex.Replace(text, @"^#{1,6}\s+", "", System.Text.RegularExpressions.RegexOptions.Multiline);

        // Remove emojis (basic range)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[\uD800-\uDBFF\uDC00-\uDFFF]", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, "[\u2600-\u27BF]", "");

        // Remove code blocks
        text = System.Text.RegularExpressions.Regex.Replace(text, @"```[\s\S]*?```", "");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"~~~[\s\S]*?~~~", "");

        // Collapse whitespace
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}