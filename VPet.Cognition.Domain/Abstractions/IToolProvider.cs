using System.Text.Json;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// Abstraction over a tool the companion can execute.
/// </summary>
public interface IToolProvider
{
    string ToolName { get; }
    ToolClass Class { get; }
    ToolSchema Schema { get; }
    Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction over the permission service that gates tool execution.
/// </summary>
public interface IPermissionService
{
    Task<PermissionState> CheckAsync(string toolName, CancellationToken cancellationToken = default);
    Task<PermissionState> RequestAsync(string toolName, ToolSchema schema, JsonElement args, CancellationToken cancellationToken = default);
    Task GrantAsync(string toolName, bool alwaysAllow, CancellationToken cancellationToken = default);
    Task DenyAsync(string toolName, CancellationToken cancellationToken = default);
    Task RevokeAsync(string toolName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionGrant>> ListGrantsAsync(CancellationToken cancellationToken = default);
}