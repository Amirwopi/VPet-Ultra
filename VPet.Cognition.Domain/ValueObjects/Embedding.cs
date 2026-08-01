namespace VPet.Cognition.Domain.ValueObjects;

/// <summary>
/// An embedding vector (float array) with a dimension count.
/// </summary>
public sealed class Embedding : IEquatable<Embedding>
{
    public float[] Vector { get; }
    public int Dimensions => Vector.Length;

    public Embedding(float[] vector)
    {
        Vector = vector ?? throw new ArgumentNullException(nameof(vector));
    }

    public Embedding(IReadOnlyList<float> vector)
    {
        if (vector is null)
            throw new ArgumentNullException(nameof(vector));
        Vector = new float[vector.Count];
        for (int i = 0; i < vector.Count; i++)
            Vector[i] = vector[i];
    }

    /// <summary>
    /// Cosine similarity to another embedding (range [-1, 1]).
    /// Returns 0 if either vector is zero-length or dimensions mismatch.
    /// </summary>
    public double CosineSimilarity(Embedding other)
    {
        if (other is null || other.Dimensions != Dimensions)
            return 0;

        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < Dimensions; i++)
        {
            float a = Vector[i];
            float b = other.Vector[i];
            dot += a * b;
            magA += a * a;
            magB += b * b;
        }

        double denom = Math.Sqrt(magA) * Math.Sqrt(magB);
        return denom < 1e-12 ? 0 : dot / denom;
    }

    public bool Equals(Embedding? other)
    {
        if (other is null || other.Dimensions != Dimensions)
            return false;
        for (int i = 0; i < Dimensions; i++)
            if (Math.Abs(Vector[i] - other.Vector[i]) > 1e-6f)
                return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is Embedding e && Equals(e);
    public override int GetHashCode() => Vector.GetHashCode();
    public override string ToString() => $"Embedding[{Dimensions}]";
}