using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace VPet.Cognition.MemoryStore;

/// <summary>
/// SQLite-backed persistence for cognitive state: personality, emotion, goals,
/// permission grants, and tool audit log. Uses the same SQLite DB as the memory store.
/// </summary>
public sealed class CognitionStore
{
    private readonly string _connectionString;
    private readonly ILogger<CognitionStore> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public CognitionStore(string dbPath, ILogger<CognitionStore> logger)
    {
        _connectionString = $"Data Source={dbPath}";
        _logger = logger;
    }

    private async Task EnsureInitializedAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_initialized) return;

            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(ct);

            var sql = """
                CREATE TABLE IF NOT EXISTS personality_state (
                    id INTEGER PRIMARY KEY DEFAULT 1,
                    data TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS emotion_state (
                    id INTEGER PRIMARY KEY DEFAULT 1,
                    data TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS goals (
                    id TEXT PRIMARY KEY,
                    data TEXT NOT NULL,
                    status TEXT NOT NULL DEFAULT 'active',
                    priority REAL NOT NULL DEFAULT 0.5,
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_goals_status ON goals(status);

                CREATE TABLE IF NOT EXISTS permission_grants (
                    tool_id TEXT PRIMARY KEY,
                    granted_at TEXT NOT NULL,
                    scope TEXT NOT NULL DEFAULT 'always',
                    expires_at TEXT
                );

                CREATE TABLE IF NOT EXISTS tool_invocations (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    tool_id TEXT NOT NULL,
                    arguments TEXT,
                    result TEXT,
                    success INTEGER NOT NULL DEFAULT 0,
                    invoked_at TEXT NOT NULL,
                    duration_ms INTEGER,
                    hash_prev TEXT
                );
                CREATE INDEX IF NOT EXISTS idx_tool_invocations_tool ON tool_invocations(tool_id);
                CREATE INDEX IF NOT EXISTS idx_tool_invocations_time ON tool_invocations(invoked_at DESC);
                """;

            await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
            _initialized = true;
            _logger.LogInformation("CognitionStore initialized at {Conn}", _connectionString);
        }
        finally
        {
            _initLock.Release();
        }
    }

    // ── Personality persistence ──

    public async Task<string?> LoadPersonalityAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<(string Data, string UpdatedAt)>(
            new CommandDefinition("SELECT data, updated_at FROM personality_state WHERE id = 1", cancellationToken: ct));
        return row.Data;
    }

    public async Task SavePersonalityAsync(string json, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var now = DateTimeOffset.UtcNow.ToString("O");
        var sql = """
            INSERT INTO personality_state (id, data, updated_at) VALUES (1, @Data, @Now)
            ON CONFLICT(id) DO UPDATE SET data = @Data, updated_at = @Now;
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Data = json, Now = now }, cancellationToken: ct));
    }

    // ── Emotion persistence ──

    public async Task<string?> LoadEmotionAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<(string Data, string UpdatedAt)>(
            new CommandDefinition("SELECT data, updated_at FROM emotion_state WHERE id = 1", cancellationToken: ct));
        return row.Data;
    }

    public async Task SaveEmotionAsync(string json, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var now = DateTimeOffset.UtcNow.ToString("O");
        var sql = """
            INSERT INTO emotion_state (id, data, updated_at) VALUES (1, @Data, @Now)
            ON CONFLICT(id) DO UPDATE SET data = @Data, updated_at = @Now;
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new { Data = json, Now = now }, cancellationToken: ct));
    }

    // ── Permission grants ──

    public async Task<bool> IsPermissionGrantedAsync(string toolId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var row = await conn.QueryFirstOrDefaultAsync<(string Scope, string? ExpiresAt)>(
            new CommandDefinition(
                "SELECT scope, expires_at FROM permission_grants WHERE tool_id = @ToolId",
                new { ToolId = toolId }, cancellationToken: ct));
        if (row.Scope is null) return false;
        if (row.ExpiresAt is { } exp && DateTimeOffset.TryParse(exp, out var expDate) && expDate < DateTimeOffset.UtcNow)
            return false;
        return row.Scope == "always";
    }

    public async Task GrantPermissionAsync(string toolId, string scope = "always", TimeSpan? expiry = null, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var now = DateTimeOffset.UtcNow.ToString("O");
        var expires = expiry.HasValue ? DateTimeOffset.UtcNow.Add(expiry.Value).ToString("O") : (string?)null;
        var sql = """
            INSERT INTO permission_grants (tool_id, granted_at, scope, expires_at) VALUES (@ToolId, @Now, @Scope, @Expires)
            ON CONFLICT(tool_id) DO UPDATE SET granted_at = @Now, scope = @Scope, expires_at = @Expires;
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new { ToolId = toolId, Now = now, Scope = scope, Expires = expires }, cancellationToken: ct));
    }

    public async Task RevokePermissionAsync(string toolId, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM permission_grants WHERE tool_id = @ToolId",
            new { ToolId = toolId }, cancellationToken: ct));
    }

    // ── Tool audit log ──

    public async Task LogToolInvocationAsync(string toolId, string? arguments, string? result,
        bool success, long durationMs, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var now = DateTimeOffset.UtcNow.ToString("O");

        // Get previous hash for tamper-evident chain
        var prevHash = await conn.QueryFirstOrDefaultAsync<string>(
            new CommandDefinition(
                "SELECT hash_prev FROM tool_invocations ORDER BY id DESC LIMIT 1",
                cancellationToken: ct));

        // Compute current hash: SHA256(prevHash + toolId + result + now)
        var hashInput = (prevHash ?? "") + toolId + (result ?? "") + now;
        var currentHash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(hashInput));
        var hashHex = Convert.ToHexString(currentHash);

        var sql = """
            INSERT INTO tool_invocations (tool_id, arguments, result, success, invoked_at, duration_ms, hash_prev)
            VALUES (@ToolId, @Args, @Result, @Success, @Now, @Duration, @Hash);
            """;
        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            ToolId = toolId,
            Args = arguments,
            Result = result,
            Success = success ? 1 : 0,
            Now = now,
            Duration = durationMs,
            Hash = hashHex,
        }, cancellationToken: ct));
    }

    public async Task<IEnumerable<ToolInvocationRecord>> GetToolInvocationsAsync(int limit = 50, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);
        var rows = await conn.QueryAsync<ToolInvocationRow>(
            new CommandDefinition(
                "SELECT * FROM tool_invocations ORDER BY id DESC LIMIT @Limit",
                new { Limit = limit }, cancellationToken: ct));
        return rows.Select(r => new ToolInvocationRecord
        {
            ToolId = r.tool_id,
            Arguments = r.arguments,
            Result = r.result,
            Success = r.success != 0,
            InvokedAt = DateTimeOffset.Parse(r.invoked_at),
            DurationMs = r.duration_ms ?? 0,
        });
    }
}

// ── DTOs ──

public sealed class ToolInvocationRecord
{
    public string ToolId { get; set; } = "";
    public string? Arguments { get; set; }
    public string? Result { get; set; }
    public bool Success { get; set; }
    public DateTimeOffset InvokedAt { get; set; }
    public long DurationMs { get; set; }
}

internal sealed class ToolInvocationRow
{
    public string tool_id { get; set; } = "";
    public string? arguments { get; set; }
    public string? result { get; set; }
    public int success { get; set; }
    public string invoked_at { get; set; } = "";
    public long? duration_ms { get; set; }
}