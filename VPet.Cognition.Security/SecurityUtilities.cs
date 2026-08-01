using System.Text;
using System.Text.Json;

namespace VPet.Cognition.Security;

/// <summary>
/// Redacts sensitive values (API keys, tokens) from logs and config dumps.
/// </summary>
public static class SecretRedactor
{
    private static readonly string[] SensitiveKeys = { "apiKey", "fallbackApiKey", "token", "secret", "password", "credential" };

    /// <summary>
    /// Redact sensitive fields in a JSON string, replacing values with "***".
    /// </summary>
    public static string Redact(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return json;

            var sb = new StringBuilder();
            using var writer = new Utf8JsonWriter(new MemoryStream());
            // Simple regex-based redaction is more robust for arbitrary JSON
            foreach (var key in SensitiveKeys)
            {
                var pattern = $"\"{key}\"\\s*:\\s*\"[^\"]*\"";
                json = System.Text.RegularExpressions.Regex.Replace(
                    json, pattern, $"\"{key}\":\"***\"",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            return json;
        }
        catch
        {
            return json;
        }
    }

    /// <summary>
    /// Redact a single value if the key is sensitive.
    /// </summary>
    public static string RedactValue(string key, string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        foreach (var sk in SensitiveKeys)
        {
            if (string.Equals(key, sk, StringComparison.OrdinalIgnoreCase))
                return "***";
        }
        return value;
    }

    /// <summary>
    /// Check if a key name is sensitive.
    /// </summary>
    public static bool IsSensitive(string key)
    {
        foreach (var sk in SensitiveKeys)
        {
            if (string.Equals(key, sk, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}

/// <summary>
/// Validates file paths against an allowlist to prevent path traversal.
/// </summary>
public static class PathValidator
{
    /// <summary>
    /// Check if a path is within any of the allowed directories.
    /// </summary>
    public static bool IsPathAllowed(string path, IReadOnlyList<string> allowedPaths)
    {
        if (allowedPaths.Count == 0) return true; // No allowlist = allow all

        try
        {
            var fullPath = Path.GetFullPath(path);
            foreach (var allowed in allowedPaths)
            {
                var allowedFull = Path.GetFullPath(allowed);
                if (fullPath.StartsWith(allowedFull, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate that a path doesn't contain directory traversal sequences.
    /// </summary>
    public static bool IsSafePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        if (path.Contains("..", StringComparison.OrdinalIgnoreCase)) return false;
        if (path.Contains("~", StringComparison.OrdinalIgnoreCase)) return false;
        return true;
    }
}

/// <summary>
/// Validates commands against a blocklist of dangerous patterns.
/// </summary>
public static class CommandValidator
{
    private static readonly string[] DangerousPatterns =
    {
        "rm -rf", "format", "del /f", "rmdir /s", "shutdown",
        "taskkill", "reg delete", "diskpart", "mkfs",
        "dd if=", ":(){:|:&};:", "fork bomb",
    };

    /// <summary>
    /// Check if a command contains dangerous patterns.
    /// </summary>
    public static bool IsDangerous(string command)
    {
        if (string.IsNullOrEmpty(command)) return false;
        var lower = command.ToLowerInvariant();
        return DangerousPatterns.Any(p => lower.Contains(p));
    }

    /// <summary>
    /// Check if a command is in the blocked list.
    /// </summary>
    public static bool IsBlocked(string command, IReadOnlyList<string> blockedCommands)
    {
        if (blockedCommands.Count == 0) return false;
        var lower = command.ToLowerInvariant().Trim();
        return blockedCommands.Any(b => lower.Contains(b.ToLowerInvariant()));
    }
}