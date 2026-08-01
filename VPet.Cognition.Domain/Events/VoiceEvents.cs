namespace VPet.Cognition.Domain.Events;

/// <summary>
/// Voice-related domain events.
/// </summary>
public sealed record WakeWordDetectedEvent(string WakeWord, double Confidence) : CognitionEvent;
public sealed record TranscriptReceivedEvent(string Text, double Confidence, bool IsFinal) : CognitionEvent;
public sealed record TtsChunkReadyEvent(int ChunkIndex, TimeSpan Duration) : CognitionEvent;