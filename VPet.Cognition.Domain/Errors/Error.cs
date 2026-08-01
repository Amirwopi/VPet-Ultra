namespace VPet.Cognition.Domain.Errors;

/// <summary>
/// Base type for all domain errors. Carries a stable code and a human-readable message.
/// </summary>
public abstract record Error(string Code, string Message)
{
    public override string ToString() => $"[{Code}] {Message}";
}