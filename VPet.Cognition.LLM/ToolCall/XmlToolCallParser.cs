using System.Text.Json;
using System.Text.RegularExpressions;
using VPet.Cognition.Domain.Entities;

namespace VPet.Cognition.LLM.ToolCall;

/// <summary>
/// Extracts &lt;tool name="..." args='...'&gt; tags from LLM text output.
/// Fallback for local models that lack native tool-calling support.
/// </summary>
public static partial class XmlToolCallParser
{
    [GeneratedRegex(@"<tool\s+name=""(?<name>[^""]+)""\s+args='(?<args>[^']*)'\s*/?>", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ToolRegex();

    /// <summary>
    /// Extract all tool-call requests from a text block. Returns empty if none found.
    /// </summary>
    public static IReadOnlyList<ToolCallRequest> Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
            return Array.Empty<ToolCallRequest>();

        var regex = ToolRegex();
        var matches = regex.Matches(text);
        if (matches.Count == 0)
            return Array.Empty<ToolCallRequest>();

        var result = new List<ToolCallRequest>(matches.Count);
        foreach (Match m in matches)
        {
            var name = m.Groups["name"].Value;
            var argsStr = m.Groups["args"].Value;
            var args = ParseArgsSafely(argsStr);
            result.Add(new ToolCallRequest(Guid.NewGuid().ToString("N"), name, args));
        }
        return result;
    }

    /// <summary>
    /// Remove tool tags from text (for display).
    /// </summary>
    public static string StripToolTags(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return ToolRegex().Replace(text, string.Empty).Trim();
    }

    private static JsonElement ParseArgsSafely(string argsStr)
    {
        if (string.IsNullOrWhiteSpace(argsStr))
            return JsonDocument.Parse("{}").RootElement;

        try
        {
            return JsonDocument.Parse(argsStr).RootElement;
        }
        catch
        {
            // Wrap as a single "value" field
            return JsonDocument.Parse($"{{\"value\":{JsonSerializer.Serialize(argsStr)}}}").RootElement;
        }
    }
}