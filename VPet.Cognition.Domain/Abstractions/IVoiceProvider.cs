using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// A chunk of PCM audio from the TTS provider.
/// </summary>
public sealed record AudioChunk(byte[] PcmData, int SampleRate, int Channels, TimeSpan Duration);

/// <summary>
/// A viseme hint for lip-sync.
/// </summary>
public sealed record VisemeHint(int Viseme, TimeSpan Start, TimeSpan Duration);

/// <summary>
/// A partial or final transcript chunk from the STT provider.
/// </summary>
public sealed record TranscriptChunk(string Text, double Confidence, TimeSpan Start, TimeSpan Duration);

/// <summary>
/// Event args for wake-word detection.
/// </summary>
public sealed record WakeWordDetectedEventArgs(string WakeWord, double Confidence, DateTimeOffset At);

/// <summary>
/// Abstraction over a wake-word detector (e.g. OpenWakeWord via ONNX).
/// </summary>
public interface IWakeWordProvider : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    event Action<WakeWordDetectedEventArgs>? WakeWordDetected;
}

/// <summary>
/// Abstraction over a streaming speech-to-text provider (Faster Whisper, whisper.cpp, Handy).
/// </summary>
public interface ISttProvider : IAsyncDisposable
{
    Task StartStreamingAsync(CancellationToken cancellationToken = default);
    Task StopStreamingAsync(CancellationToken cancellationToken = default);
    event Action<TranscriptChunk>? PartialTranscript;
    event Action<TranscriptChunk>? FinalTranscript;
    event Action? VoiceActivityDetected;
    event Action? VoiceActivityEnded;

    /// <summary>
    /// Transcribe a complete audio file (non-streaming convenience method).
    /// </summary>
    Task<TranscriptChunk> TranscribeFileAsync(string audioFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quick availability check (executable + model exist).
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction over a text-to-speech provider (Piper, Kokoro, XTTS v2).
/// </summary>
public interface ITtsProvider : IAsyncDisposable
{
    IAsyncEnumerable<AudioChunk> SynthesizeAsync(string text, VoiceProsody prosody, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VisemeHint>> GetVisemesAsync(string text, CancellationToken cancellationToken = default);
}