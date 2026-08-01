namespace VPet.Cognition.Domain.Errors;

/// <summary>
/// Errors raised by the memory store.
/// </summary>
public sealed record MemoryError(string Code, string Message) : Error(Code, Message)
{
    public static MemoryError DbLocked =>
        new("MEM_DB_LOCKED", "The memory database is locked by another process.");

    public static MemoryError DiskFull =>
        new("MEM_DISK_FULL", "The disk is full; cannot write to the memory database.");

    public static MemoryError EmbeddingFailed =>
        new("MEM_EMBEDDING_FAIL", "The embedding provider failed to produce a vector.");

    public static MemoryError NotFound =>
        new("MEM_NOT_FOUND", "The requested memory was not found.");

    public static MemoryError Corrupt(string detail) =>
        new("MEM_CORRUPT", $"The memory database is corrupt: {detail}");
}