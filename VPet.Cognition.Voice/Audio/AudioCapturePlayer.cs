using System.IO;
using NAudio.Wave;
using Microsoft.Extensions.Logging;

namespace VPet.Cognition.Voice.Audio;

/// <summary>
/// Microphone audio capture using NAudio. Captures 16kHz mono 16-bit PCM
/// suitable for whisper.cpp. Writes WAV chunks to a temp directory for
/// the streaming STT provider to pick up.
/// </summary>
public sealed class AudioCapture : IDisposable
{
    private readonly ILogger<AudioCapture> _logger;
    private readonly string _outputDir;
    private WaveInEvent? _waveIn;
    private WaveFileWriter? _waveWriter;
    private string? _currentFile;
    private DateTime _chunkStart;
    private readonly TimeSpan _chunkDuration = TimeSpan.FromSeconds(10); // 10s chunks
    private bool _capturing;

    public event Action<string>? ChunkReady;

    public AudioCapture(string outputDir, ILogger<AudioCapture> logger)
    {
        _outputDir = outputDir;
        _logger = logger;
        Directory.CreateDirectory(outputDir);
    }

    /// <summary>Start capturing audio from the default microphone.</summary>
    public void Start()
    {
        if (_capturing) return;

        try
        {
            _waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1), // 16kHz mono 16-bit
                BufferMilliseconds = 100,
            };
            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.RecordingStopped += OnRecordingStopped;

            StartNewChunk();
            _waveIn.StartRecording();
            _capturing = true;
            _logger.LogInformation("Audio capture started: 16kHz mono 16-bit");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start audio capture.");
        }
    }

    /// <summary>Stop capturing audio.</summary>
    public void Stop()
    {
        if (!_capturing) return;

        _capturing = false;
        _waveIn?.StopRecording();
        _logger.LogInformation("Audio capture stopped.");
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_waveWriter is null || !_capturing) return;

        try
        {
            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);

            // Check if chunk duration exceeded
            if (DateTime.UtcNow - _chunkStart >= _chunkDuration)
            {
                FinalizeChunk();
                StartNewChunk();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audio data write failed.");
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        FinalizeChunk();
        if (e.Exception is not null)
            _logger.LogError(e.Exception, "Audio capture stopped with error.");
    }

    private void StartNewChunk()
    {
        _currentFile = Path.Combine(_outputDir, $"chunk_{DateTime.UtcNow:yyyyMMddHHmmssfff}.wav");
        _waveWriter = new WaveFileWriter(_currentFile, _waveIn!.WaveFormat);
        _chunkStart = DateTime.UtcNow;
    }

    private void FinalizeChunk()
    {
        try
        {
            _waveWriter?.Dispose();
            _waveWriter = null;

            if (_currentFile is not null && File.Exists(_currentFile))
            {
                var fileInfo = new FileInfo(_currentFile);
                if (fileInfo.Length > 1000) // Only emit if > 1KB (not silence)
                {
                    _logger.LogDebug("Audio chunk ready: {File} ({Size} bytes)", _currentFile, fileInfo.Length);
                    ChunkReady?.Invoke(_currentFile);
                }
                else
                {
                    // Too small, likely silence — delete
                    File.Delete(_currentFile);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to finalize audio chunk.");
        }
    }

    public void Dispose()
    {
        Stop();
        _waveWriter?.Dispose();
        _waveIn?.Dispose();
    }
}

/// <summary>
/// Audio playback using NAudio. Plays WAV files from TTS providers.
/// Supports barge-in (stop current playback when user starts speaking).
/// </summary>
public sealed class AudioPlayer : IDisposable
{
    private readonly ILogger<AudioPlayer> _logger;
    private WaveOutEvent? _waveOut;
    private WaveFileReader? _waveReader;
    private bool _playing;

    public event Action? PlaybackStarted;
    public event Action? PlaybackFinished;
    public event Action? BargeInTriggered;

    public bool IsPlaying => _playing;

    public AudioPlayer(ILogger<AudioPlayer> logger)
    {
        _logger = logger;
    }

    /// <summary>Play a WAV file asynchronously.</summary>
    public async Task PlayAsync(string wavPath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(wavPath))
        {
            _logger.LogWarning("Audio file not found: {Path}", wavPath);
            return;
        }

        Stop();

        try
        {
            _waveReader = new WaveFileReader(wavPath);
            _waveOut = new WaveOutEvent();
            _waveOut.Volume = 1.0f; // Max volume
            _waveOut.Init(_waveReader);

            var tcs = new TaskCompletionSource<bool>();
            EventHandler<StoppedEventArgs>? handler = null;
            handler = (_, e) =>
            {
                _playing = false;
                if (e.Exception is not null)
                    _logger.LogWarning(e.Exception, "Audio playback stopped with error.");
                PlaybackFinished?.Invoke();
                tcs.TrySetResult(true);
            };
            _waveOut.PlaybackStopped += handler;

            _playing = true;
            PlaybackStarted?.Invoke();
            _waveOut.Play();

            _logger.LogInformation("Playing audio: {Path} ({Duration:F1}s, {Rate}Hz, {Ch}ch)",
                wavPath, _waveReader.TotalTime.TotalSeconds, _waveReader.WaveFormat.SampleRate, _waveReader.WaveFormat.Channels);

            // Wait for playback to finish or cancellation
            using var reg = cancellationToken.Register(() =>
            {
                _logger.LogDebug("Barge-in: stopping audio playback.");
                BargeInTriggered?.Invoke();
                Stop();
                tcs.TrySetResult(false);
            });

            await tcs.Task;
            _waveOut.PlaybackStopped -= handler;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Audio playback failed: {Path}", wavPath);
            _playing = false;
        }
    }

    /// <summary>Stop current playback immediately (barge-in).</summary>
    public void Stop()
    {
        if (!_playing) return;

        _playing = false;
        try { _waveOut?.Stop(); } catch { /* ignore */ }
    }

    public void Dispose()
    {
        Stop();
        _waveReader?.Dispose();
        _waveOut?.Dispose();
    }
}