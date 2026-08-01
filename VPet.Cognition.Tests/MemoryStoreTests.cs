using FluentAssertions;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;
using VPet.Cognition.MemoryStore;

namespace VPet.Cognition.Tests;

/// <summary>
/// Tests for the SQLite memory store and MemoryService.
/// </summary>
public class MemoryStoreTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteMemoryStore _store;
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    public MemoryStoreTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"vpet_test_{Guid.NewGuid():N}.db");
        _store = new SqliteMemoryStore(_dbPath, _loggerFactory.CreateLogger<SqliteMemoryStore>());
    }

    public void Dispose()
    {
        _store.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _loggerFactory.Dispose();
        try { File.Delete(_dbPath); } catch { /* ignore */ }
    }

    [Fact]
    public async Task StoreAsync_ShouldReturnMemoryId()
    {
        var mem = new Memory
        {
            Id = MemoryId.New(),
            Type = MemoryType.Episodic,
            Content = "User said hello",
            Source = "test",
            Importance = new Importance(0.8),
        };

        var id = await _store.StoreAsync(mem);
        id.Should().Be(mem.Id);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnStoredMemory()
    {
        var mem = new Memory
        {
            Id = MemoryId.New(),
            Type = MemoryType.Semantic,
            Content = "User likes coffee",
            Source = "test",
            Importance = new Importance(0.9),
        };

        await _store.StoreAsync(mem);
        var retrieved = await _store.GetAsync(mem.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Content.Should().Be("User likes coffee");
        retrieved.Type.Should().Be(MemoryType.Semantic);
        retrieved.Importance.Value.Should().BeApproximately(0.9, 0.001);
    }

    [Fact]
    public async Task GetAsync_NonExistent_ShouldReturnNull()
    {
        var result = await _store.GetAsync(MemoryId.New());
        result.Should().BeNull();
    }

    [Fact]
    public async Task RetrieveAsync_ShouldFilterByType()
    {
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "ep1", Importance = new Importance(0.5) });
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Semantic, Content = "sem1", Importance = new Importance(0.7) });
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Semantic, Content = "sem2", Importance = new Importance(0.6) });

        var result = await _store.RetrieveAsync(new MemoryQuery { TypeFilter = MemoryType.Semantic, TopK = 10 });

        result.Should().HaveCount(2);
        result.Should().OnlyContain(m => m.Type == MemoryType.Semantic);
    }

    [Fact]
    public async Task RetrieveAsync_ShouldOrderByImportanceDesc()
    {
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "low", Importance = new Importance(0.1) });
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "high", Importance = new Importance(0.9) });
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "mid", Importance = new Importance(0.5) });

        var result = await _store.RetrieveAsync(new MemoryQuery { TopK = 3 });

        result[0].Content.Should().Be("high");
        result[1].Content.Should().Be("mid");
        result[2].Content.Should().Be("low");
    }

    [Fact]
    public async Task SearchAsync_ShouldFindByText()
    {
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Semantic, Content = "user likes pizza", Importance = new Importance(0.7) });
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Semantic, Content = "user likes sushi", Importance = new Importance(0.7) });

        var result = await _store.SearchAsync("pizza");

        result.Should().HaveCount(1);
        result[0].Content.Should().Contain("pizza");
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "a", Importance = new Importance(0.5) });
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Semantic, Content = "b", Importance = new Importance(0.5) });

        var total = await _store.CountAsync();
        total.Should().Be(2);

        var semantic = await _store.CountAsync(MemoryType.Semantic);
        semantic.Should().Be(1);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveMemory()
    {
        var id = await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "temp", Importance = new Importance(0.5) });

        await _store.DeleteAsync(id);
        var retrieved = await _store.GetAsync(id);

        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task ConsolidateAsync_ShouldDecayImportance()
    {
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "test", Importance = new Importance(0.8) });

        await _store.ConsolidateAsync();

        var memories = await _store.RetrieveAsync(new MemoryQuery { TopK = 10 });
        memories.Should().HaveCount(1);
        // 0.8 * 0.95 = 0.76
        memories[0].Importance.Value.Should().BeApproximately(0.76, 0.01);
    }

    [Fact]
    public async Task ConsolidateAsync_ShouldForgetVeryLowImportance()
    {
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "tiny", Importance = new Importance(0.005) });

        await _store.ConsolidateAsync();

        var count = await _store.CountAsync();
        count.Should().Be(0); // forgotten, not counted
    }

    [Fact]
    public async Task ForgetAsync_ShouldMarkAllAsForgotten()
    {
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "a", Importance = new Importance(0.9) });
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Episodic, Content = "b", Importance = new Importance(0.9) });

        await _store.ForgetAsync();

        var count = await _store.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task ExportAsync_ShouldWriteJsonFile()
    {
        await _store.StoreAsync(new Memory { Id = MemoryId.New(), Type = MemoryType.Semantic, Content = "fact", Importance = new Importance(0.7) });

        var exportPath = Path.Combine(Path.GetTempPath(), $"vpet_export_{Guid.NewGuid():N}.json");
        await _store.ExportAsync(exportPath);

        File.Exists(exportPath).Should().BeTrue();
        var json = await File.ReadAllTextAsync(exportPath);
        json.Should().Contain("fact");

        File.Delete(exportPath);
    }

    [Fact]
    public async Task MemoryService_RememberConversationAsync_ShouldStoreEpisodic()
    {
        var service = new MemoryService(_store, _loggerFactory.CreateLogger<MemoryService>());

        var id = await service.RememberConversationAsync("hello", "hi there");

        var retrieved = await _store.GetAsync(id);
        retrieved.Should().NotBeNull();
        retrieved!.Type.Should().Be(MemoryType.Episodic);
        retrieved.Content.Should().Contain("hello");
        retrieved.Content.Should().Contain("hi there");
    }

    [Fact]
    public async Task MemoryService_RememberFactAsync_ShouldStoreSemantic()
    {
        var service = new MemoryService(_store, _loggerFactory.CreateLogger<MemoryService>());

        var id = await service.RememberFactAsync("user likes tea");

        var retrieved = await _store.GetAsync(id);
        retrieved.Should().NotBeNull();
        retrieved!.Type.Should().Be(MemoryType.Semantic);
        retrieved.Content.Should().Be("user likes tea");
    }

    [Fact]
    public async Task MemoryService_RetrieveContextAsync_ShouldReturnFormattedContext()
    {
        var service = new MemoryService(_store, _loggerFactory.CreateLogger<MemoryService>());

        await service.RememberFactAsync("user likes coffee");
        await service.RememberConversationAsync("hi", "hello");

        var context = await service.RetrieveContextAsync("coffee", 5);

        context.Should().Contain("coffee");
        context.Should().Contain("[semantic]");
    }

    [Fact]
    public async Task MemoryService_RetrieveContextAsync_NoMemories_ShouldReturnEmpty()
    {
        var service = new MemoryService(_store, _loggerFactory.CreateLogger<MemoryService>());

        var context = await service.RetrieveContextAsync("nothing");

        context.Should().BeEmpty();
    }
}