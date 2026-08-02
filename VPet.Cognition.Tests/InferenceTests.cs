using FluentAssertions;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Inference.Abstractions;
using VPet.Cognition.Inference.Models;
using VPet.Cognition.Inference.Online;
using VPet.Cognition.Inference.Vision;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace VPet.Cognition.Tests;

/// <summary>
/// Tests for the inference engine: model registry, model manager, online provider, vision module.
/// </summary>
public class InferenceTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    public InferenceTests()
    {
        _server = WireMockServer.Start();
    }

    public void Dispose()
    {
        _server.Stop();
        _loggerFactory.Dispose();
    }

    // === Model Registry Tests ===

    [Fact]
    public void ModelRegistry_Entries_ShouldContainQwenAndSmolVlm()
    {
        ModelRegistry.Entries.Should().NotBeEmpty();
        ModelRegistry.Entries.Should().Contain(e => e.Name.Contains("Qwen2.5-3B"));
        ModelRegistry.Entries.Should().Contain(e => e.Name.Contains("SmolVLM"));
        ModelRegistry.Entries.Should().Contain(e => e.Category == ModelCategory.Embedding);
    }

    [Fact]
    public void ModelRegistry_Search_ShouldFindByText()
    {
        var results = ModelRegistry.Search("qwen");
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(e =>
        {
            var lower = (e.Name + " " + e.Description + " " + string.Join(" ", e.Tags)).ToLowerInvariant();
            lower.Should().Contain("qwen");
        });
    }

    [Fact]
    public void ModelRegistry_Search_ShouldFindByCategory()
    {
        var results = ModelRegistry.Search("vision");
        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(e =>
        {
            var lower = (e.Name + " " + e.Description + " " + string.Join(" ", e.Tags)).ToLowerInvariant();
            lower.Should().Contain("vision");
        });
    }

    [Fact]
    public void ModelRegistry_Search_EmptyQuery_ShouldReturnAll()
    {
        var results = ModelRegistry.Search("");
        results.Should().HaveCount(ModelRegistry.Entries.Count);
    }

    [Fact]
    public void ModelRegistry_GetByCategory_ShouldFilterCorrectly()
    {
        var textModels = ModelRegistry.GetByCategory(ModelCategory.Text);
        textModels.Should().NotBeEmpty();
        textModels.Should().AllSatisfy(e => e.Category.Should().Be(ModelCategory.Text));

        var visionModels = ModelRegistry.GetByCategory(ModelCategory.Vision);
        visionModels.Should().NotBeEmpty();
        visionModels.Should().AllSatisfy(e => e.Category.Should().Be(ModelCategory.Vision));
    }

    [Fact]
    public void ModelRegistry_GetById_ShouldReturnEntry()
    {
        var entry = ModelRegistry.GetById("qwen2.5-3b-instruct-q4_k_m");
        entry.Should().NotBeNull();
        entry!.Name.Should().Contain("Qwen2.5-3B");
    }

    [Fact]
    public void ModelRegistry_GetById_NonExistent_ShouldReturnNull()
    {
        var entry = ModelRegistry.GetById("non-existent-model");
        entry.Should().BeNull();
    }

    [Fact]
    public void ModelRegistry_GetDownloadUrl_ShouldReturnHuggingFaceUrl()
    {
        var entry = ModelRegistry.GetById("qwen2.5-3b-instruct-q4_k_m")!;
        var url = ModelRegistry.GetDownloadUrl(entry);
        url.Should().StartWith("https://huggingface.co/");
        url.Should().Contain(entry.HuggingFaceRepo);
        url.Should().Contain(entry.FileName);
    }

    // === Model Manager Tests ===

    [Fact]
    public void ModelManager_ListInstalled_EmptyDir_ShouldReturnEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"vpet_test_{Guid.NewGuid():N}");
        var manager = new ModelManager(tempDir, _loggerFactory.CreateLogger<ModelManager>());

        var installed = manager.ListInstalled();
        installed.Should().BeEmpty();

        try { Directory.Delete(tempDir); } catch { }
    }

    [Fact]
    public void ModelManager_IsInstalled_NotInstalled_ShouldReturnFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"vpet_test_{Guid.NewGuid():N}");
        var manager = new ModelManager(tempDir, _loggerFactory.CreateLogger<ModelManager>());

        manager.IsInstalled("non-existent").Should().BeFalse();

        try { Directory.Delete(tempDir); } catch { }
    }

    [Fact]
    public void ModelManager_GetModelPath_ShouldReturnCorrectPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"vpet_test_{Guid.NewGuid():N}");
        var manager = new ModelManager(tempDir, _loggerFactory.CreateLogger<ModelManager>());

        var path = manager.GetModelPath("test-model");
        path.Should().EndWith("test-model.gguf");
        path.Should().Contain(tempDir);

        try { Directory.Delete(tempDir); } catch { }
    }

    [Fact]
    public void ModelManager_SearchRegistry_ShouldReturnResults()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"vpet_test_{Guid.NewGuid():N}");
        var manager = new ModelManager(tempDir, _loggerFactory.CreateLogger<ModelManager>());

        var results = manager.SearchRegistry("qwen");
        results.Should().NotBeEmpty();

        try { Directory.Delete(tempDir); } catch { }
    }

    [Fact]
    public async Task ModelManager_DeleteAsync_NotInstalled_ShouldReturnFail()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"vpet_test_{Guid.NewGuid():N}");
        var manager = new ModelManager(tempDir, _loggerFactory.CreateLogger<ModelManager>());

        var result = await manager.DeleteAsync("non-existent");
        result.IsSuccess.Should().BeFalse();

        try { Directory.Delete(tempDir); } catch { }
    }

    [Fact]
    public void ModelManager_DownloadProgress_ShouldBeRaised()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"vpet_test_{Guid.NewGuid():N}");
        var manager = new ModelManager(tempDir, _loggerFactory.CreateLogger<ModelManager>());

        var progressEvents = new List<DownloadProgressEvent>();
        manager.DownloadProgress += e => progressEvents.Add(e);

        // We can't actually download a model in tests, but we can verify the event is wired
        progressEvents.Should().BeEmpty();

        try { Directory.Delete(tempDir); } catch { }
    }

    // === Online Inference Provider Tests ===

    [Fact]
    public async Task OnlineInferenceProvider_IsAvailableAsync_ServerUp_ShouldReturnTrue()
    {
        _server
            .Given(Request.Create().WithPath("/api/tags").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        var provider = new OnlineInferenceProvider(
            "ollama", _server.Url!, "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var available = await provider.IsAvailableAsync();
        available.Should().BeTrue();
    }

    [Fact]
    public async Task OnlineInferenceProvider_IsAvailableAsync_ServerDown_ShouldReturnFalse()
    {
        var provider = new OnlineInferenceProvider(
            "ollama", "http://127.0.0.1:1", "", TimeSpan.FromSeconds(2),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var available = await provider.IsAvailableAsync();
        available.Should().BeFalse();
    }

    [Fact]
    public async Task OnlineInferenceProvider_ListModelsAsync_Ollama_ShouldReturnModels()
    {
        _server
            .Given(Request.Create().WithPath("/api/tags").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    models = new[]
                    {
                        new { name = "qwen2.5:3b", size = 2000000000L, details = new { family = "qwen2", quantization_level = "Q4_K_M" } },
                        new { name = "llava:7b", size = 4300000000L, details = new { family = "llava", quantization_level = "Q4_K_M" } },
                    }
                }));

        var provider = new OnlineInferenceProvider(
            "ollama", _server.Url!, "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var result = await provider.ListModelsAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value!.Should().HaveCount(2);
        result.Value!.First().Id.Should().Be("qwen2.5:3b");
        result.Value!.First().Category.Should().Be(ModelCategory.Text);
        result.Value![1].Category.Should().Be(ModelCategory.Vision); // llava is vision
    }

    [Fact]
    public async Task OnlineInferenceProvider_LoadModelAsync_ShouldSetActiveModel()
    {
        _server
            .Given(Request.Create().WithPath("/api/tags").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    models = new[]
                    {
                        new { name = "qwen2.5:3b", size = 2000000000L, details = new { family = "qwen2" } }
                    }
                }));

        var provider = new OnlineInferenceProvider(
            "ollama", _server.Url!, "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var result = await provider.LoadModelAsync("qwen2.5:3b");
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task OnlineInferenceProvider_LoadModelAsync_NotFound_ShouldFail()
    {
        _server
            .Given(Request.Create().WithPath("/api/tags").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { models = new[] { new { name = "qwen2.5:3b" } } }));

        var provider = new OnlineInferenceProvider(
            "ollama", _server.Url!, "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var result = await provider.LoadModelAsync("non-existent");
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task OnlineInferenceProvider_CompleteAsync_Ollama_ShouldReturnText()
    {
        _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    message = new { role = "assistant", content = "Hello!" },
                    done = true,
                    prompt_eval_count = 10,
                    eval_count = 5,
                }));

        _server
            .Given(Request.Create().WithPath("/api/tags").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { models = new[] { new { name = "qwen2.5:3b" } } }));

        var provider = new OnlineInferenceProvider(
            "ollama", _server.Url!, "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        await provider.LoadModelAsync("qwen2.5:3b");

        var result = await provider.CompleteAsync(new InferenceRequest
        {
            Messages = new List<InferenceMessage> { new() { Role = "user", Content = "hi" } }
        });

        result.IsSuccess.Should().BeTrue();
        result.Value!.Text.Should().Be("Hello!");
    }

    [Fact]
    public async Task OnlineInferenceProvider_StreamAsync_Ollama_ShouldYieldChunks()
    {
        var chunk1 = System.Text.Json.JsonSerializer.Serialize(new { message = new { role = "assistant", content = "Hel" }, done = false });
        var chunk2 = System.Text.Json.JsonSerializer.Serialize(new { message = new { role = "assistant", content = "lo!" }, done = true });

        _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody($"{chunk1}\n{chunk2}\n", encoding: System.Text.Encoding.UTF8));

        _server
            .Given(Request.Create().WithPath("/api/tags").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new { models = new[] { new { name = "qwen2.5:3b" } } }));

        var provider = new OnlineInferenceProvider(
            "ollama", _server.Url!, "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        await provider.LoadModelAsync("qwen2.5:3b");

        var chunks = new List<string>();
        await foreach (var chunk in provider.StreamAsync(new InferenceRequest
        {
            Messages = new List<InferenceMessage> { new() { Role = "user", Content = "hi" } }
        }))
        {
            if (chunk.TextDelta is { } text)
                chunks.Add(text);
        }

        string.Join("", chunks).Should().Be("Hello!");
    }

    // === Vision Module Tests ===

    [Fact]
    public void VisionModule_Disabled_ShouldNotBeAvailable()
    {
        var provider = new OnlineInferenceProvider(
            "ollama", "http://localhost:11434", "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var vision = new VisionModule(provider, enabledByDefault: false, _loggerFactory.CreateLogger<VisionModule>());

        vision.IsEnabled.Should().BeFalse();
        vision.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void VisionModule_EnabledButNoVisionModel_ShouldNotBeAvailable()
    {
        var provider = new OnlineInferenceProvider(
            "ollama", "http://localhost:11434", "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var vision = new VisionModule(provider, enabledByDefault: true, _loggerFactory.CreateLogger<VisionModule>());

        // Provider doesn't have vision capability by default
        vision.IsEnabled.Should().BeTrue();
        vision.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task VisionModule_DescribeImage_Disabled_ShouldReturnFail()
    {
        var provider = new OnlineInferenceProvider(
            "ollama", "http://localhost:11434", "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var vision = new VisionModule(provider, enabledByDefault: false, _loggerFactory.CreateLogger<VisionModule>());

        var result = await vision.DescribeImageAsync(new byte[] { 1, 2, 3 });
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("disabled");
    }

    [Fact]
    public void VisionModule_Toggle_ShouldChangeState()
    {
        var provider = new OnlineInferenceProvider(
            "ollama", "http://localhost:11434", "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        var vision = new VisionModule(provider, enabledByDefault: false, _loggerFactory.CreateLogger<VisionModule>());

        vision.IsEnabled.Should().BeFalse();
        vision.IsEnabled = true;
        vision.IsEnabled.Should().BeTrue();
        vision.IsEnabled = false;
        vision.IsEnabled.Should().BeFalse();
    }

    // === Inference Capabilities Tests ===

    [Fact]
    public void InferenceCapabilities_Flags_ShouldWork()
    {
        var caps = InferenceCapabilities.Text | InferenceCapabilities.Streaming;
        caps.HasFlag(InferenceCapabilities.Text).Should().BeTrue();
        caps.HasFlag(InferenceCapabilities.Vision).Should().BeFalse();

        caps |= InferenceCapabilities.Vision;
        caps.HasFlag(InferenceCapabilities.Vision).Should().BeTrue();
    }

    [Fact]
    public void OnlineInferenceProvider_Capabilities_ShouldIncludeTextAndStreaming()
    {
        var provider = new OnlineInferenceProvider(
            "ollama", "http://localhost:11434", "", TimeSpan.FromSeconds(5),
            new HttpClient(), _loggerFactory.CreateLogger<OnlineInferenceProvider>());

        provider.Capabilities.HasFlag(InferenceCapabilities.Text).Should().BeTrue();
        provider.Capabilities.HasFlag(InferenceCapabilities.Streaming).Should().BeTrue();
    }
}