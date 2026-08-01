using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Tests.Fakes;

/// <summary>
/// A fake <see cref="IMemoryStore"/> backed by an in-memory dictionary.
/// </summary>
public sealed class FakeMemoryStore : IMemoryStore
{
    private readonly Dictionary<MemoryId, Memory> _store = new();

    public Task<MemoryId> StoreAsync(Memory memory, CancellationToken cancellationToken = default)
    {
        _store[memory.Id] = memory with { };
        return Task.FromResult(memory.Id);
    }

    public Task<Memory?> GetAsync(MemoryId id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var m);
        if (m is not null)
        {
            m.AccessCount++;
            m.LastAccessedAt = DateTimeOffset.UtcNow;
        }
        return Task.FromResult(m);
    }

    public Task<IReadOnlyList<Memory>> RetrieveAsync(MemoryQuery query, CancellationToken cancellationToken = default)
    {
        IEnumerable<Memory> results = _store.Values;
        if (query.TypeFilter is { } type)
            results = results.Where(m => m.Type == type);
        if (query.MinImportance > 0)
            results = results.Where(m => m.Importance >= query.MinImportance);
        results = results.OrderByDescending(m => m.Importance).Take(query.TopK);
        return Task.FromResult((IReadOnlyList<Memory>)results.ToList());
    }

    public Task<IReadOnlyList<Memory>> SearchAsync(string keyword, int limit = 10, CancellationToken cancellationToken = default)
    {
        var results = _store.Values
            .Where(m => m.Content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .Take(limit)
            .ToList();
        return Task.FromResult((IReadOnlyList<Memory>)results);
    }

    public Task ConsolidateAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task ForgetAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CompressAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<int> CountAsync(MemoryType? type = null, CancellationToken cancellationToken = default)
    {
        var count = type is null ? _store.Count : _store.Count(m => m.Value.Type == type);
        return Task.FromResult(count);
    }

    public Task DeleteAsync(MemoryId id, CancellationToken cancellationToken = default)
    {
        _store.Remove(id);
        return Task.CompletedTask;
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        _store.Clear();
        return Task.CompletedTask;
    }

    public Task ExportAsync(string path, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public ValueTask DisposeAsync()
    {
        _store.Clear();
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// A fake <see cref="IEmbeddingProvider"/> that produces deterministic hash-based vectors.
/// </summary>
public sealed class FakeEmbeddingProvider : IEmbeddingProvider
{
    public int Dimensions { get; set; } = 8;

    public Task<Embedding> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var vec = new float[Dimensions];
        for (int i = 0; i < Dimensions; i++)
            vec[i] = (float)((text.GetHashCode() >> i) & 0xFF) / 255f;
        return Task.FromResult(new Embedding(vec));
    }

    public async Task<IReadOnlyList<Embedding>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var list = new List<Embedding>();
        foreach (var t in texts)
            list.Add(await EmbedAsync(t, cancellationToken));
        return list;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}