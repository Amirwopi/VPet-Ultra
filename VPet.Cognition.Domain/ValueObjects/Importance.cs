namespace VPet.Cognition.Domain.ValueObjects;

/// <summary>
/// An importance score in the range [0, 1].
/// </summary>
public readonly struct Importance : IEquatable<Importance>, IComparable<Importance>
{
    public double Value { get; }

    public Importance(double value)
    {
        if (double.IsNaN(value) || value < 0)
            Value = 0;
        else if (value > 1)
            Value = 1;
        else
            Value = value;
    }

    public static Importance Default => new(0.5);

    public bool Equals(Importance other) => Math.Abs(Value - other.Value) < 1e-9;
    public override bool Equals(object? obj) => obj is Importance i && Equals(i);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("F3");

    public int CompareTo(Importance other) => Value.CompareTo(other.Value);

    public static bool operator ==(Importance a, Importance b) => a.Equals(b);
    public static bool operator !=(Importance a, Importance b) => !a.Equals(b);
    public static bool operator >(Importance a, Importance b) => a.Value > b.Value;
    public static bool operator <(Importance a, Importance b) => a.Value < b.Value;
    public static bool operator >=(Importance a, Importance b) => a.Value >= b.Value;
    public static bool operator <=(Importance a, Importance b) => a.Value <= b.Value;

    public static implicit operator double(Importance i) => i.Value;
    public static implicit operator Importance(double d) => new(d);
}