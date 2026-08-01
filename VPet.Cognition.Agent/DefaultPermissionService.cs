using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.Events;

namespace VPet.Cognition.AgentRuntime;

/// <summary>
/// Default <see cref="IPermissionService"/> implementation. Maintains in-memory
/// permission grants. In a full implementation, grants are persisted to SQLite.
/// </summary>
public sealed class DefaultPermissionService : IPermissionService
{
    private readonly ILogger<DefaultPermissionService> _logger;
    private readonly ConcurrentDictionary<string, PermissionGrant> _grants = new();

    public event Action<CognitionEvent>? EventRaised;

    public DefaultPermissionService(ILogger<DefaultPermissionService> logger)
    {
        _logger = logger;
    }

    public Task<PermissionState> CheckAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (_grants.TryGetValue(toolName, out var grant))
        {
            if (grant.RevokedAt is not null)
                return Task.FromResult(PermissionState.Revoked);
            if (grant.State == PermissionState.Granted)
                return Task.FromResult(PermissionState.Granted);
            if (grant.State == PermissionState.Denied)
                return Task.FromResult(PermissionState.Denied);
        }
        return Task.FromResult(PermissionState.NotRequested);
    }

    public Task<PermissionState> RequestAsync(string toolName, ToolSchema schema, JsonElement args, CancellationToken cancellationToken = default)
    {
        // In a full implementation, this would show a dialog to the user.
        // For now, auto-grant if the tool doesn't require confirmation.
        if (!schema.RequiresConfirmation)
        {
            GrantAsync(toolName, schema.AllowAlways, cancellationToken).GetAwaiter().GetResult();
            return Task.FromResult(PermissionState.Granted);
        }

        // If confirmation required, mark as prompted (UI layer will resolve)
        _logger.LogInformation("Permission prompted for tool {Tool}", toolName);
        EventRaised?.Invoke(new PermissionPromptedEvent(toolName));
        return Task.FromResult(PermissionState.Prompted);
    }

    public Task GrantAsync(string toolName, bool alwaysAllow, CancellationToken cancellationToken = default)
    {
        var grant = new PermissionGrant(
            toolName, PermissionState.Granted, alwaysAllow,
            DateTimeOffset.UtcNow, null);

        _grants[toolName] = grant;
        _logger.LogInformation("Permission granted for tool {Tool} (always={Always})", toolName, alwaysAllow);
        EventRaised?.Invoke(new PermissionGrantedEvent(toolName, alwaysAllow));
        return Task.CompletedTask;
    }

    public Task DenyAsync(string toolName, CancellationToken cancellationToken = default)
    {
        var grant = new PermissionGrant(
            toolName, PermissionState.Denied, false,
            DateTimeOffset.UtcNow, null);

        _grants[toolName] = grant;
        _logger.LogInformation("Permission denied for tool {Tool}", toolName);
        EventRaised?.Invoke(new PermissionDeniedEvent(toolName));
        return Task.CompletedTask;
    }

    public Task RevokeAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (_grants.TryGetValue(toolName, out var grant))
        {
            _grants[toolName] = grant with { State = PermissionState.Revoked, RevokedAt = DateTimeOffset.UtcNow };
            _logger.LogInformation("Permission revoked for tool {Tool}", toolName);
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PermissionGrant>> ListGrantsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PermissionGrant>>(_grants.Values.ToList());
    }
}