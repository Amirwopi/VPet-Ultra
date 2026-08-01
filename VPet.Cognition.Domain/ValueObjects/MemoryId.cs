using System.Text.Json;

namespace VPet.Cognition.Domain.ValueObjects;

/// <summary>
/// A strongly-typed memory identifier (GUID).
/// </summary>
public readonly struct MemoryId : IEquatable<MemoryId>
{
    public Guid Value { get; }

    public MemoryId(Guid value) => Value = value;

    public static MemoryId New() => new(Guid.NewGuid());

    public static MemoryId Parse(string s) => new(Guid.Parse(s));

    public bool Equals(MemoryId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is MemoryId id && Equals(id);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");

    public static bool operator ==(MemoryId a, MemoryId b) => a.Equals(b);
    public static bool operator !=(MemoryId a, MemoryId b) => !a.Equals(b);

    public static implicit operator Guid(MemoryId id) => id.Value;
    public static implicit operator MemoryId(Guid g) => new(g);
}