using System.Text.Json;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Tests.Fakes;

/// <summary>
/// A fake <see cref="IToolProvider"/> that returns a configurable result.
/// </summary>
public sealed class FakeToolProvider : IToolProvider
{
    public string ToolName { get; set; } = "fake-tool";
    public ToolClass Class { get; set; } = ToolClass.Application;
    public ToolSchema Schema { get; set; } = new()
    {
        Name = "fake-tool",
        Description = "A fake tool for testing.",
        ParametersJsonSchema = JsonSerializer.SerializeToElement(new { type = "object" }),
        RequiresConfirmation = false,
        AllowAlways = true,
    };
    public ToolResult NextResult { get; set; } = new(true, "ok", null, TimeSpan.FromMilliseconds(50));
    public int ExecuteCount { get; private set; }

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        ExecuteCount++;
        return Task.FromResult(NextResult);
    }
}

/// <summary>
/// A fake <see cref="IPermissionService"/> with configurable auto-grant/deny behavior.
/// </summary>
public sealed class FakePermissionService : IPermissionService
{
    public PermissionState AutoState { get; set; } = PermissionState.Granted;
    public bool AutoAlwaysAllow { get; set; } = false;
    private readonly Dictionary<string, PermissionGrant> _grants = new();

    public Task<PermissionState> CheckAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (_grants.TryGetValue(toolName, out var g) && g.State == PermissionState.Granted)
            return Task.FromResult(PermissionState.Granted);
        return Task.FromResult(AutoState);
    }

    public Task<PermissionState> RequestAsync(string toolName, ToolSchema schema, JsonElement args, CancellationToken cancellationToken = default)
    {
        if (AutoState == PermissionState.Granted)
        {
            _grants[toolName] = new PermissionGrant(toolName, PermissionState.Granted, AutoAlwaysAllow, DateTimeOffset.UtcNow, null);
        }
        return Task.FromResult(AutoState);
    }

    public Task GrantAsync(string toolName, bool alwaysAllow, CancellationToken cancellationToken = default)
    {
        _grants[toolName] = new PermissionGrant(toolName, PermissionState.Granted, alwaysAllow, DateTimeOffset.UtcNow, null);
        return Task.CompletedTask;
    }

    public Task DenyAsync(string toolName, CancellationToken cancellationToken = default)
    {
        _grants[toolName] = new PermissionGrant(toolName, PermissionState.Denied, false, DateTimeOffset.UtcNow, null);
        return Task.CompletedTask;
    }

    public Task RevokeAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (_grants.TryGetValue(toolName, out var g))
            _grants[toolName] = g with { State = PermissionState.Revoked, RevokedAt = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PermissionGrant>> ListGrantsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult((IReadOnlyList<PermissionGrant>)_grants.Values.ToList());
}