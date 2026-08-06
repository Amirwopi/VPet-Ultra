using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Tests.Fakes;

/// <summary>
/// A fake <see cref="IWakeWordProvider"/> that can be triggered manually in tests.
/// </summary>
public sealed class FakeWakeWordProvider : IWakeWordProvider
{
    public bool IsRunning { get; private set; }
    public event Action<WakeWordDetectedEventArgs>? WakeWordDetected;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    /// <summary>Simulate a wake-word detection in a test.</summary>
    public void SimulateWakeWord(string word = "hey vpet", double confidence = 0.9) =>
        WakeWordDetected?.Invoke(new WakeWordDetectedEventArgs(word, confidence, DateTimeOffset.UtcNow));

    public ValueTask DisposeAsync()
    {
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// A fake <see cref="ISttProvider"/> that can be fed transcripts in tests.
/// </summary>
public sealed class FakeSttProvider : ISttProvider
{
    public bool IsStreaming { get; private set; }
    public event Action<TranscriptChunk>? PartialTranscript;
    public event Action<TranscriptChunk>? FinalTranscript;
    public event Action? VoiceActivityDetected;
    public event Action? VoiceActivityEnded;

    public Task StartStreamingAsync(CancellationToken cancellationToken = default)
    {
        IsStreaming = true;
        return Task.CompletedTask;
    }

    public Task StopStreamingAsync(CancellationToken cancellationToken = default)
    {
        IsStreaming = false;
        return Task.CompletedTask;
    }

    public Task<TranscriptChunk> TranscribeFileAsync(string audioFilePath, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new TranscriptChunk("fake transcript", 0.95, TimeSpan.Zero, TimeSpan.FromSeconds(2)));
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public void SimulatePartial(string text, double confidence = 0.8) =>
        PartialTranscript?.Invoke(new TranscriptChunk(text, confidence, TimeSpan.Zero, TimeSpan.Zero));

    public void SimulateFinal(string text, double confidence = 0.95) =>
        FinalTranscript?.Invoke(new TranscriptChunk(text, confidence, TimeSpan.Zero, TimeSpan.FromSeconds(2)));

    public void SimulateVadStart() => VoiceActivityDetected?.Invoke();
    public void SimulateVadEnd() => VoiceActivityEnded?.Invoke();

    public ValueTask DisposeAsync()
    {
        IsStreaming = false;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// A fake <see cref="ITtsProvider"/> that returns silent audio chunks.
/// </summary>
public sealed class FakeTtsProvider : ITtsProvider
{
    public int SynthesizeCallCount { get; private set; }

    public async IAsyncEnumerable<AudioChunk> SynthesizeAsync(string text, VoiceProsody prosody,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        SynthesizeCallCount++;
        // Split into "chunks" by word for test realism
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(1, cancellationToken);
            yield return new AudioChunk(new byte[1600], 16000, 1, TimeSpan.FromMilliseconds(100));
        }
    }

    public Task<IReadOnlyList<VisemeHint>> GetVisemesAsync(string text, CancellationToken cancellationToken = default)
    {
        var visemes = new List<VisemeHint>();
        for (int i = 0; i < text.Length; i++)
            visemes.Add(new VisemeHint(i % 5, TimeSpan.FromMilliseconds(i * 100), TimeSpan.FromMilliseconds(100)));
        return Task.FromResult((IReadOnlyList<VisemeHint>)visemes);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}