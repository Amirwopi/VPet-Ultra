using System.Text.Json;
using VPet.Cognition.Domain.Entities;

namespace VPet.Cognition.LLM.ToolCall;

/// <summary>
/// Normalizes tool-call requests and results across providers.
/// </summary>
public static class ToolCallProtocol
{
    /// <summary>
    /// Parse tool-call arguments JSON defensively. Returns null on failure.
    /// </summary>
    public static JsonElement? ParseArgumentsSafely(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return JsonDocument.Parse("{}").RootElement;

        try
        {
            return JsonDocument.Parse(json).RootElement;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Build a tool-result message to feed back to the LLM.
    /// </summary>
    public static ToolCallResult ToResult(ToolResult result, string id)
        => new(id, result.Success, result.Output, result.Error);
}