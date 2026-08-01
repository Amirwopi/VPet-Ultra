using System.Text;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.MemoryStore;

/// <summary>
/// High-level memory operations: store conversation turns, retrieve context for prompts,
/// consolidate (decay + expire), and forget. Wraps <see cref="IMemoryStore"/>.
/// </summary>
public sealed class MemoryService
{
    private readonly IMemoryStore _store;
    private readonly IEmbeddingProvider? _embeddingProvider;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(
        IMemoryStore store,
        ILogger<MemoryService> logger,
        IEmbeddingProvider? embeddingProvider = null)
    {
        _store = store;
        _logger = logger;
        _embeddingProvider = embeddingProvider;
    }

    public async Task<MemoryId> RememberConversationAsync(
        string userMessage, string companionReply, string? source = null,
        Importance? importance = null, CancellationToken cancellationToken = default)
    {
        var content = $"User: {userMessage}\nCompanion: {companionReply}";
        var mem = new Memory
        {
            Id = MemoryId.New(),
            Type = MemoryType.Episodic,
            Content = content,
            Source = source ?? "conversation",
            Importance = importance ?? new Importance(0.5),
        };

        if (_embeddingProvider is { } emb)
        {
            try { mem.Embedding = await emb.EmbedAsync(content, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Embedding failed; storing without it."); }
        }

        return await _store.StoreAsync(mem, cancellationToken);
    }

    public async Task<MemoryId> RememberFactAsync(
        string fact, string? source = null, Importance? importance = null,
        CancellationToken cancellationToken = default)
    {
        var mem = new Memory
        {
            Id = MemoryId.New(),
            Type = MemoryType.Semantic,
            Content = fact,
            Source = source ?? "inferred",
            Importance = importance ?? new Importance(0.7),
        };

        if (_embeddingProvider is { } emb)
        {
            try { mem.Embedding = await emb.EmbedAsync(fact, cancellationToken); }
            catch (Exception ex) { _logger.LogWarning(ex, "Embedding failed; storing without it."); }
        }

        return await _store.StoreAsync(mem, cancellationToken);
    }

    public async Task<MemoryId> RememberPreferenceAsync(
        string preference, Importance? importance = null,
        CancellationToken cancellationToken = default)
    {
        var mem = new Memory
        {
            Id = MemoryId.New(),
            Type = MemoryType.Semantic,
            Content = preference,
            Source = "user",
            Importance = importance ?? new Importance(0.8),
        };
        return await _store.StoreAsync(mem, cancellationToken);
    }

    public async Task<string> RetrieveContextAsync(
        string query, int maxMemories = 5, CancellationToken cancellationToken = default)
    {
        var memoryQuery = new MemoryQuery { TopK = maxMemories, MinImportance = 0.1 };

        if (_embeddingProvider is { } emb)
        {
            try
            {
                var queryEmb = await emb.EmbedAsync(query, cancellationToken);
                memoryQuery = memoryQuery with { QueryEmbedding = queryEmb };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Query embedding failed; falling back to text search.");
                memoryQuery = memoryQuery with { Text = query };
            }
        }
        else
        {
            memoryQuery = memoryQuery with { Text = query };
        }

        var memories = await _store.RetrieveAsync(memoryQuery, cancellationToken);
        if (memories.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var mem in memories)
        {
            var tag = mem.Type switch
            {
                MemoryType.Episodic => "episodic",
                MemoryType.Semantic => "semantic",
                MemoryType.Relationship => "relationship",
                MemoryType.Reflection => "reflection",
                _ => mem.Type.ToString().ToLowerInvariant(),
            };
            sb.AppendLine($"[{tag}] {mem.Content}");
        }
        return sb.ToString().TrimEnd();
    }

    public async Task ConsolidateAsync(CancellationToken cancellationToken = default)
        => await _store.ConsolidateAsync(cancellationToken);

    public async Task ForgetAsync(CancellationToken cancellationToken = default)
        => await _store.ForgetAsync(cancellationToken);

    public async Task CompressAsync(CancellationToken cancellationToken = default)
        => await _store.CompressAsync(cancellationToken);

    public async Task DeleteAsync(MemoryId id, CancellationToken cancellationToken = default)
        => await _store.DeleteAsync(id, cancellationToken);

    public async Task<int> CountAsync(MemoryType? type = null, CancellationToken cancellationToken = default)
        => await _store.CountAsync(type, cancellationToken);

    public async Task ExportAsync(string path, CancellationToken cancellationToken = default)
        => await _store.ExportAsync(path, cancellationToken);
}