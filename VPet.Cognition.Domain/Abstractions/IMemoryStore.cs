using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.Domain.Abstractions;

/// <summary>
/// A query against the memory store.
/// </summary>
public sealed record MemoryQuery
{
    public string? Text { get; init; }
    public Embedding? QueryEmbedding { get; init; }
    public MemoryType? TypeFilter { get; init; }
    public int TopK { get; init; } = 10;
    public double MinImportance { get; init; } = 0.0;
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
}

/// <summary>
/// Abstraction over the cognitive memory store. Implementations include
/// SQLite + sqlite-vec, FAISS, and (future) Qdrant.
/// </summary>
public interface IMemoryStore : IAsyncDisposable
{
    Task<MemoryId> StoreAsync(Memory memory, CancellationToken cancellationToken = default);
    Task<Memory?> GetAsync(MemoryId id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Memory>> RetrieveAsync(MemoryQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Memory>> SearchAsync(string keyword, int limit = 10, CancellationToken cancellationToken = default);
    Task ConsolidateAsync(CancellationToken cancellationToken = default);
    Task ForgetAsync(CancellationToken cancellationToken = default);
    Task CompressAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(MemoryType? type = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(MemoryId id, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(CancellationToken cancellationToken = default);
    Task ExportAsync(string path, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstraction over an embedding model used to vectorize text for memory retrieval.
/// </summary>
public interface IEmbeddingProvider : IAsyncDisposable
{
    int Dimensions { get; }
    Task<Embedding> EmbedAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Embedding>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}