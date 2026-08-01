namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// The state of a permission grant for a tool class.
/// </summary>
public enum PermissionState
{
    /// <summary>No request has been made yet.</summary>
    NotRequested = 0,

    /// <summary>A prompt is currently displayed to the user.</summary>
    Prompted = 1,

    /// <summary>The user granted permission (once or always).</summary>
    Granted = 2,

    /// <summary>The user denied permission.</summary>
    Denied = 3,

    /// <summary>A previously granted permission was revoked.</summary>
    Revoked = 4,
}