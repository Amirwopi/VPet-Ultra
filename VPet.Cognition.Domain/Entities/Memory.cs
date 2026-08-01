using System.Text.Json;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Entities;

/// <summary>
/// A single cognitive memory record.
/// </summary>
public sealed record Memory
{
    public MemoryId Id { get; init; } = MemoryId.New();
    public MemoryType Type { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? Summary { get; set; }
    public Embedding? Embedding { get; set; }
    public Importance Importance { get; set; } = Importance.Default;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastAccessedAt { get; set; } = DateTimeOffset.UtcNow;
    public int AccessCount { get; set; }
    public Dictionary<string, string> Metadata { get; init; } = new();
    public List<MemoryId> RelatedIds { get; init; } = new();
    public string? Source { get; init; }
    public bool Forgotten { get; set; }
    public DateTimeOffset? ForgottenAt { get; set; }
}