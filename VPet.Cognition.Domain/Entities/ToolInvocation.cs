using System.Text.Json;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Domain.Entities;

/// <summary>
/// The result of a tool execution.
/// </summary>
public sealed record ToolResult(bool Success, string Output, string? Error, TimeSpan Duration);

/// <summary>
/// A tool-call request emitted by the LLM.
/// </summary>
public sealed record ToolCallRequest(string Id, string ToolName, JsonElement Arguments);

/// <summary>
/// A tool-call result fed back to the LLM.
/// </summary>
public sealed record ToolCallResult(string Id, bool Success, string Output, string? Error);

/// <summary>
/// The schema describing a tool to the LLM.
/// </summary>
public sealed record ToolSchema
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public JsonElement ParametersJsonSchema { get; init; }
    public bool RequiresConfirmation { get; init; }
    public bool AllowAlways { get; init; } = true;
}

/// <summary>
/// A persisted permission grant.
/// </summary>
public sealed record PermissionGrant(string ToolName, PermissionState State, bool AlwaysAllow, DateTimeOffset GrantedAt, DateTimeOffset? RevokedAt);

/// <summary>
/// An audited tool invocation record.
/// </summary>
public sealed record ToolInvocationRecord(
    Guid InvocationId, string ToolName, string ArgsJson, PermissionState Permission,
    bool Success, string? Output, string? Error, DateTimeOffset StartedAt, long DurationMs, Guid CorrelationId);