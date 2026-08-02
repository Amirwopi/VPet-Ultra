using System.Text.Json;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.MemoryStore;

/// <summary>
/// SQLite-backed <see cref="IMemoryStore"/>. Stores episodic and semantic memories with
/// embeddings for cosine-similarity retrieval. Uses Dapper for lightweight mapping.
/// </summary>
public sealed class SqliteMemoryStore : IMemoryStore
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteMemoryStore> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;

    public SqliteMemoryStore(string dbPath, ILogger<SqliteMemoryStore> logger)
    {
        _connectionString = $"Data Source={dbPath}";
        _logger = logger;
    }

    public async Task<MemoryId> StoreAsync(Memory memory, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var embeddingJson = memory.Embedding is { } emb
            ? JsonSerializer.Serialize(emb.Vector)
            : "[]";

        var metadataJson = JsonSerializer.Serialize(memory.Metadata);
        var relatedJson = JsonSerializer.Serialize(memory.RelatedIds.Select(r => r.Value));

        var sql = """
            INSERT INTO memories (id, type, content, summary, source, importance, embedding,
                                  created_at, last_accessed_at, access_count, metadata, related, forgotten, forgotten_at)
            VALUES (@Id, @Type, @Content, @Summary, @Source, @Importance, @Embedding,
                    @CreatedAt, @LastAccessedAt, @AccessCount, @Metadata, @Related, @Forgotten, @ForgottenAt);
            """;

        await conn.ExecuteAsync(new CommandDefinition(sql, new
        {
            Id = memory.Id.Value.ToString("D"),
            Type = memory.Type.ToString(),
            memory.Content,
            memory.Summary,
            memory.Source,
            Importance = memory.Importance.Value,
            Embedding = embeddingJson,
            memory.CreatedAt,
            memory.LastAccessedAt,
            memory.AccessCount,
            Metadata = metadataJson,
            Related = relatedJson,
            memory.Forgotten,
            memory.ForgottenAt,
        }, cancellationToken: cancellationToken));

        _logger.LogDebug("Stored memory {Id} type={Type} importance={Importance}",
            memory.Id.Value, memory.Type, memory.Importance.Value);

        return memory.Id;
    }

    public async Task<Memory?> GetAsync(MemoryId id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var row = await conn.QuerySingleOrDefaultAsync<MemoryRow>(
            new CommandDefinition("SELECT * FROM memories WHERE id = @Id",
            new { Id = id.Value.ToString("D") }, cancellationToken: cancellationToken));

        if (row is not null)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE memories SET access_count = access_count + 1, last_accessed_at = @now WHERE id = @Id",
                new { now = DateTimeOffset.UtcNow, Id = id.Value.ToString("D") }, cancellationToken: cancellationToken));
        }

        return row is null ? null : MapRow(row);
    }

    public async Task<IReadOnlyList<Memory>> RetrieveAsync(MemoryQuery query, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM memories WHERE forgotten = 0";
        var args = new DynamicParameters();

        if (query.TypeFilter.HasValue)
        {
            sql += " AND type = @type";
            args.Add("type", query.TypeFilter.Value.ToString());
        }

        if (query.MinImportance > 0)
        {
            sql += " AND importance >= @minImp";
            args.Add("minImp", query.MinImportance);
        }

        if (query.Since.HasValue)
        {
            sql += " AND created_at >= @since";
            args.Add("since", query.Since.Value);
        }

        if (query.Until.HasValue)
        {
            sql += " AND created_at <= @until";
            args.Add("until", query.Until.Value);
        }

        sql += " ORDER BY importance DESC, created_at DESC LIMIT @limit";
        args.Add("limit", query.TopK);

        var rows = await conn.QueryAsync<MemoryRow>(new CommandDefinition(sql, args, cancellationToken: cancellationToken));
        var memories = rows.Select(MapRow).ToList();

        if (query.QueryEmbedding is { } queryEmb && memories.Count > 0)
        {
            memories = memories
                .Select(m => (Mem: m, Score: m.Embedding?.CosineSimilarity(queryEmb) ?? 0))
                .OrderByDescending(x => x.Score)
                .Take(query.TopK)
                .Select(x => x.Mem)
                .ToList();
        }

        return memories;
    }

    public async Task<IReadOnlyList<Memory>> SearchAsync(string keyword, int limit = 10, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var rows = await conn.QueryAsync<MemoryRow>(new CommandDefinition(
            "SELECT * FROM memories WHERE forgotten = 0 AND content LIKE @kw ORDER BY importance DESC LIMIT @limit",
            new { kw = $"%{keyword}%", limit }, cancellationToken: cancellationToken));

        return rows.Select(MapRow).ToList();
    }

    public async Task ConsolidateAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        // 1. Decay importance of all non-forgotten memories (time-based decay)
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE memories SET importance = importance * 0.97 WHERE forgotten = 0",
            cancellationToken: cancellationToken));

        // 2. Boost importance of recently accessed memories (spaced repetition)
        var recentlyAccessed = DateTimeOffset.UtcNow.AddHours(-1);
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE memories SET importance = MIN(1.0, importance * 1.05) WHERE forgotten = 0 AND last_accessed_at > @cutoff",
            new { cutoff = recentlyAccessed }, cancellationToken: cancellationToken));

        // 3. Prune very low importance memories
        var pruned = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE memories SET forgotten = 1, forgotten_at = @now WHERE importance < 0.01 AND forgotten = 0",
            new { now = DateTimeOffset.UtcNow }, cancellationToken: cancellationToken));

        // 4. Promote high-importance episodic memories to semantic (consolidation)
        var promoted = await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE memories SET type = 'Semantic' WHERE type = 'Episodic' AND importance > 0.8 AND access_count > 3 AND forgotten = 0",
            cancellationToken: cancellationToken));

        _logger.LogInformation("Consolidated: pruned {Pruned}, promoted {Promoted} memories", pruned, promoted);
    }

    public async Task ForgetAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        // Mark memories as forgotten if: importance < 0.05 AND older than 1 day
        // OR importance < 0.1 AND older than 7 days
        var now = DateTimeOffset.UtcNow;
        var oneDayAgo = now.AddDays(-1);
        var sevenDaysAgo = now.AddDays(-7);

        var count = await conn.ExecuteAsync(new CommandDefinition(
            """
            UPDATE memories SET forgotten = 1, forgotten_at = @now
            WHERE forgotten = 0 AND (
                (importance < 0.05 AND created_at < @oneDayAgo)
                OR (importance < 0.1 AND created_at < @sevenDaysAgo)
            )
            """,
            new { now, oneDayAgo, sevenDaysAgo }, cancellationToken: cancellationToken));

        _logger.LogInformation("Forgot {Count} low-importance memories", count);
    }

    public async Task CompressAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var cutoff = DateTimeOffset.UtcNow.AddDays(-7);
        var deleted = await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM memories WHERE forgotten = 1 AND forgotten_at < @cutoff",
            new { cutoff }, cancellationToken: cancellationToken));

        _logger.LogInformation("Compressed: physically deleted {Deleted} forgotten memories", deleted);
    }

    public async Task<int> CountAsync(MemoryType? type = null, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        if (type.HasValue)
        {
            return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(*) FROM memories WHERE forgotten = 0 AND type = @type",
                new { type = type.Value.ToString() }, cancellationToken: cancellationToken));
        }

        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM memories WHERE forgotten = 0", cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(MemoryId id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM memories WHERE id = @Id",
            new { Id = id.Value.ToString("D") }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await conn.ExecuteAsync(new CommandDefinition(
            "DELETE FROM memories", cancellationToken: cancellationToken));

        _logger.LogInformation("Deleted all memories");
    }

    public async Task ExportAsync(string path, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        var rows = await conn.QueryAsync<MemoryRow>(new CommandDefinition(
            "SELECT * FROM memories WHERE forgotten = 0 ORDER BY created_at", cancellationToken: cancellationToken));

        var memories = rows.Select(MapRow).ToList();
        var json = JsonSerializer.Serialize(memories, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(path, json, cancellationToken);

        _logger.LogInformation("Exported {Count} memories to {Path}", memories.Count, path);
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized) return;
        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_initialized) return;

            await using var conn = new SqliteConnection(_connectionString);
            await conn.OpenAsync(cancellationToken);

            var sql = """
                CREATE TABLE IF NOT EXISTS memories (
                    id TEXT PRIMARY KEY,
                    type TEXT NOT NULL,
                    content TEXT NOT NULL,
                    summary TEXT,
                    source TEXT,
                    importance REAL NOT NULL,
                    embedding TEXT NOT NULL DEFAULT '[]',
                    created_at TEXT NOT NULL,
                    last_accessed_at TEXT NOT NULL,
                    access_count INTEGER NOT NULL DEFAULT 0,
                    metadata TEXT NOT NULL DEFAULT '{}',
                    related TEXT NOT NULL DEFAULT '[]',
                    forgotten INTEGER NOT NULL DEFAULT 0,
                    forgotten_at TEXT
                );
                CREATE INDEX IF NOT EXISTS idx_memories_type ON memories(type);
                CREATE INDEX IF NOT EXISTS idx_memories_importance ON memories(importance DESC);
                CREATE INDEX IF NOT EXISTS idx_memories_created ON memories(created_at DESC);
                CREATE INDEX IF NOT EXISTS idx_memories_forgotten ON memories(forgotten);
                """;

            await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
            _initialized = true;
            _logger.LogInformation("SQLite memory store initialized at {Conn}", _connectionString);
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static Memory MapRow(MemoryRow row)
    {
        var embedding = !string.IsNullOrEmpty(row.Embedding) && row.Embedding != "[]"
            ? new Embedding(JsonSerializer.Deserialize<float[]>(row.Embedding) ?? Array.Empty<float>())
            : null;

        var metadata = !string.IsNullOrEmpty(row.Metadata) && row.Metadata != "{}"
            ? JsonSerializer.Deserialize<Dictionary<string, string>>(row.Metadata) ?? new()
            : new();

        var related = !string.IsNullOrEmpty(row.Related) && row.Related != "[]"
            ? JsonSerializer.Deserialize<List<Guid>>(row.Related)?.Select(g => new MemoryId(g)).ToList() ?? new()
            : new();

        return new Memory
        {
            Id = MemoryId.Parse(row.Id),
            Type = Enum.Parse<MemoryType>(row.Type),
            Content = row.Content,
            Summary = row.Summary,
            Source = row.Source,
            Importance = new Importance(row.Importance),
            Embedding = embedding,
            CreatedAt = ParseDateSafe(row.CreatedAt),
            LastAccessedAt = ParseDateSafe(row.LastAccessedAt),
            AccessCount = row.AccessCount,
            Metadata = metadata,
            RelatedIds = related,
            Forgotten = row.Forgotten,
            ForgottenAt = row.ForgottenAt is { } fa ? ParseDateSafe(fa) : null,
        };
    }

    private static DateTimeOffset ParseDateSafe(string s)
    {
        return string.IsNullOrEmpty(s) ? DateTimeOffset.UtcNow : DateTimeOffset.Parse(s);
    }

    private sealed class MemoryRow
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Source { get; set; }
        public double Importance { get; set; }
        public string Embedding { get; set; } = "[]";
        public string CreatedAt { get; set; } = string.Empty;
        public string LastAccessedAt { get; set; } = string.Empty;
        public int AccessCount { get; set; }
        public string Metadata { get; set; } = "{}";
        public string Related { get; set; } = "[]";
        public bool Forgotten { get; set; }
        public string? ForgottenAt { get; set; }
    }

    public ValueTask DisposeAsync()
    {
        _initLock.Dispose();
        return ValueTask.CompletedTask;
    }
}