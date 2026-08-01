namespace VPet.Cognition.Domain.ValueObjects;

/// <summary>
/// A strongly-typed goal identifier (GUID).
/// </summary>
public readonly struct GoalId : IEquatable<GoalId>
{
    public Guid Value { get; }

    public GoalId(Guid value) => Value = value;

    public static GoalId New() => new(Guid.NewGuid());

    public static GoalId Parse(string s) => new(Guid.Parse(s));

    public bool Equals(GoalId other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is GoalId id && Equals(id);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");

    public static bool operator ==(GoalId a, GoalId b) => a.Equals(b);
    public static bool operator !=(GoalId a, GoalId b) => !a.Equals(b);

    public static implicit operator Guid(GoalId id) => id.Value;
    public static implicit operator GoalId(Guid g) => new(g);
}