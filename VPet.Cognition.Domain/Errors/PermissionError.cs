namespace VPet.Cognition.Domain.Errors;

/// <summary>
/// Errors raised by the permission system and tool execution.
/// </summary>
public sealed record PermissionError(string Code, string Message) : Error(Code, Message)
{
    public static PermissionError Denied =>
        new("PERM_DENIED", "The user denied permission for this tool.");

    public static PermissionError Revoked =>
        new("PERM_REVOKED", "The previously granted permission was revoked.");

    public static PermissionError Timeout =>
        new("PERM_TIMEOUT", "The permission prompt timed out without a response.");
}