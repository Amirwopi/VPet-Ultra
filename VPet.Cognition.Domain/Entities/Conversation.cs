namespace VPet.Cognition.Domain.Entities;

/// <summary>
/// A conversation turn (user or companion).
/// </summary>
public sealed record ChatMessage(string Role, string Content);

/// <summary>
/// A conversation session.
/// </summary>
public sealed class Conversation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public List<ChatMessage> Turns { get; } = new();
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
}