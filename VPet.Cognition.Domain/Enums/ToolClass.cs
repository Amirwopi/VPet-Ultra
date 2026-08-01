namespace VPet.Cognition.Domain.Enums;

/// <summary>
/// Classification of tools the companion may execute through the permission system.
/// </summary>
public enum ToolClass
{
    /// <summary>Start an application by path or protocol.</summary>
    Application = 0,

    /// <summary>Run a PowerShell command.</summary>
    PowerShell = 1,

    /// <summary>Run a CMD command.</summary>
    CMD = 2,

    /// <summary>Read/write a single file.</summary>
    File = 3,

    /// <summary>List/move/copy/delete a folder.</summary>
    Folder = 4,

    /// <summary>Get/set the clipboard text.</summary>
    Clipboard = 5,

    /// <summary>Open a URL in the default browser.</summary>
    Browser = 6,

    /// <summary>Limited Win32 API calls (allowlisted).</summary>
    Win32 = 7,
}