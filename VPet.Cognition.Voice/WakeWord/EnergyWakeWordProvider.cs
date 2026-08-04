using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Voice.WakeWord;

/// <summary>
/// Simple energy-based wake word detector. Monitors audio input levels
/// and triggers when energy exceeds a threshold for a minimum duration.
/// This is a lightweight fallback when OpenWakeWord ONNX model is not available.
/// </summary>
public sealed class EnergyWakeWordProvider : IWakeWordProvider
{
    private readonly ILogger<EnergyWakeWordProvider> _logger;
    private readonly double _threshold;
    private readonly TimeSpan _minDuration;
    private bool _listening;
    private DateTime _energyStart;
    private CancellationTokenSource? _cts;

#pragma warning disable CS0067
    public event Action? WakeWordDetected;
    public event Action? EnergyLevelChanged;
#pragma warning restore CS0067

    public string ProviderId => "energy-vad";

    public EnergyWakeWordProvider(
        double threshold,
        TimeSpan minDuration,
        ILogger<EnergyWakeWordProvider> logger)
    {
        _threshold = threshold;
        _minDuration = minDuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listening = true;
        _logger.LogInformation("Energy VAD wake word detector started. Threshold={Threshold}, MinDuration={Duration}",
            _threshold, _minDuration);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Feed audio samples to the detector. Call this from AudioCapture.
    /// </summary>
    public void FeedSamples(byte[] buffer, int bytesRecorded)
    {
        if (!_listening) return;

        // Calculate RMS energy from 16-bit PCM samples
        double sumSquares = 0;
        int sampleCount = bytesRecorded / 2;
        for (int i = 0; i < bytesRecorded; i += 2)
        {
            short sample = (short)(buffer[i] | (buffer[i + 1] << 8));
            sumSquares += (double)sample * sample;
        }

        var rms = sampleCount > 0 ? Math.Sqrt(sumSquares / sampleCount) : 0;
        var normalizedEnergy = rms / 32768.0; // Normalize to [0, 1]

        if (normalizedEnergy > _threshold)
        {
            if (_energyStart == default)
                _energyStart = DateTime.UtcNow;

            if (DateTime.UtcNow - _energyStart >= _minDuration)
            {
                _logger.LogInformation("Wake word detected: energy={Energy:F3} > threshold={Threshold:F3}",
                    normalizedEnergy, _threshold);
                WakeWordDetected?.Invoke();
                _energyStart = default; // Reset
            }
        }
        else
        {
            _energyStart = default; // Reset if energy drops
        }
    }

    public Task StopAsync()
    {
        _listening = false;
        _cts?.Cancel();
        _logger.LogInformation("Energy VAD wake word detector stopped.");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _cts?.Dispose();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Wake word provider interface for detecting a trigger word/phrase.
/// </summary>
public interface IWakeWordProvider : IAsyncDisposable
{
    string ProviderId { get; }
    event Action? WakeWordDetected;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync();
}