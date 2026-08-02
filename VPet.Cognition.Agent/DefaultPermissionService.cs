using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.Events;
using VPet.Cognition.MemoryStore;

namespace VPet.Cognition.AgentRuntime;

/// <summary>
/// Default <see cref="IPermissionService"/> implementation. Persists grants to SQLite
/// via <see cref="CognitionStore"/>. Auto-grants non-confirming tools; prompts for
/// dangerous ones (UI layer resolves via <see cref="PermissionPromptedEvent"/>).
/// </summary>
public sealed class DefaultPermissionService : IPermissionService
{
    private readonly ILogger<DefaultPermissionService> _logger;
    private readonly CognitionStore? _store;
    private readonly ConcurrentDictionary<string, PermissionGrant> _grants = new();

    public event Action<CognitionEvent>? EventRaised;

    public DefaultPermissionService(ILogger<DefaultPermissionService> logger, CognitionStore? store = null)
    {
        _logger = logger;
        _store = store;
    }

    public async Task<PermissionState> CheckAsync(string toolName, CancellationToken cancellationToken = default)
    {
        // Check in-memory cache first
        if (_grants.TryGetValue(toolName, out var grant))
        {
            if (grant.RevokedAt is not null)
                return PermissionState.Revoked;
            if (grant.State == PermissionState.Granted)
                return PermissionState.Granted;
            if (grant.State == PermissionState.Denied)
                return PermissionState.Denied;
        }

        // Check persisted store
        if (_store is not null)
        {
            try
            {
                if (await _store.IsPermissionGrantedAsync(toolName, cancellationToken))
                {
                    _grants[toolName] = new PermissionGrant(toolName, PermissionState.Granted, true, DateTimeOffset.UtcNow, null);
                    return PermissionState.Granted;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check persisted permission for {Tool}", toolName);
            }
        }

        return PermissionState.NotRequested;
    }

    public async Task<PermissionState> RequestAsync(string toolName, ToolSchema schema, JsonElement args, CancellationToken cancellationToken = default)
    {
        // Auto-grant if the tool doesn't require confirmation
        if (!schema.RequiresConfirmation)
        {
            await GrantAsync(toolName, schema.AllowAlways, cancellationToken);
            return PermissionState.Granted;
        }

        // If confirmation required, mark as prompted (UI layer will resolve)
        _logger.LogInformation("Permission prompted for tool {Tool}", toolName);
        EventRaised?.Invoke(new PermissionPromptedEvent(toolName));
        return PermissionState.Prompted;
    }

    public async Task GrantAsync(string toolName, bool alwaysAllow, CancellationToken cancellationToken = default)
    {
        var grant = new PermissionGrant(
            toolName, PermissionState.Granted, alwaysAllow,
            DateTimeOffset.UtcNow, null);

        _grants[toolName] = grant;
        _logger.LogInformation("Permission granted for tool {Tool} (always={Always})", toolName, alwaysAllow);

        // Persist to SQLite
        if (_store is not null && alwaysAllow)
        {
            try { await _store.GrantPermissionAsync(toolName, "always", null, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to persist permission grant for {Tool}", toolName); }
        }

        EventRaised?.Invoke(new PermissionGrantedEvent(toolName, alwaysAllow));
    }

    public async Task DenyAsync(string toolName, CancellationToken cancellationToken = default)
    {
        var grant = new PermissionGrant(
            toolName, PermissionState.Denied, false,
            DateTimeOffset.UtcNow, null);

        _grants[toolName] = grant;
        _logger.LogInformation("Permission denied for tool {Tool}", toolName);

        // Persist denial by revoking
        if (_store is not null)
        {
            try { await _store.RevokePermissionAsync(toolName, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to persist permission denial for {Tool}", toolName); }
        }

        EventRaised?.Invoke(new PermissionDeniedEvent(toolName));
    }

    public async Task RevokeAsync(string toolName, CancellationToken cancellationToken = default)
    {
        if (_grants.TryGetValue(toolName, out var grant))
        {
            _grants[toolName] = grant with { State = PermissionState.Revoked, RevokedAt = DateTimeOffset.UtcNow };
            _logger.LogInformation("Permission revoked for tool {Tool}", toolName);
        }

        if (_store is not null)
        {
            try { await _store.RevokePermissionAsync(toolName, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to persist permission revoke for {Tool}", toolName); }
        }
    }

    public Task<IReadOnlyList<PermissionGrant>> ListGrantsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PermissionGrant>>(_grants.Values.ToList());
    }
}