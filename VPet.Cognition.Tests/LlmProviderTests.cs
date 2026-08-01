using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.LLM;
using VPet.Cognition.LLM.Ollama;
using VPet.Cognition.LLM.OpenAICompatible;
using VPet.Cognition.LLM.ToolCall;
using VPet.Cognition.Tests.Fakes;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace VPet.Cognition.Tests;

/// <summary>
/// Tests for the LLM providers using WireMock.Net to simulate HTTP endpoints.
/// </summary>
public class LlmProviderTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    public LlmProviderTests()
    {
        _server = WireMockServer.Start();
    }

    public void Dispose()
    {
        _server.Stop();
        _loggerFactory.Dispose();
    }

    [Fact]
    public async Task OllamaLlmProvider_ChatAsync_ShouldReturnContent()
    {
        _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    model = "qwen2.5:3b",
                    created_at = "2026-08-01T00:00:00Z",
                    message = new { role = "assistant", content = "Hello!" },
                    done = true,
                    done_reason = "stop",
                    prompt_eval_count = 10,
                    eval_count = 5,
                }));

        var provider = new OllamaLlmProvider(
            _server.Url!, "qwen2.5:3b", TimeSpan.FromSeconds(10),
            new HttpClient(), _loggerFactory.CreateLogger<OllamaLlmProvider>());

        var result = await provider.ChatAsync(new ChatRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("Hello!");
        result.Value.Usage.PromptTokens.Should().Be(10);
        result.Value.Usage.CompletionTokens.Should().Be(5);
    }

    [Fact]
    public async Task OllamaLlmProvider_StreamChatAsync_ShouldYieldTextChunks()
    {
        // Ollama streams newline-delimited JSON objects
        var chunk1 = JsonSerializer.Serialize(new { message = new { role = "assistant", content = "Hel" }, done = false });
        var chunk2 = JsonSerializer.Serialize(new { message = new { role = "assistant", content = "lo!" }, done = false });
        var chunk3 = JsonSerializer.Serialize(new { message = new { role = "assistant", content = "" }, done = true, prompt_eval_count = 10, eval_count = 5 });

        _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody($"{chunk1}\n{chunk2}\n{chunk3}\n", encoding: Encoding.UTF8));

        var provider = new OllamaLlmProvider(
            _server.Url!, "qwen2.5:3b", TimeSpan.FromSeconds(10),
            new HttpClient(), _loggerFactory.CreateLogger<OllamaLlmProvider>());

        var chunks = new List<string>();
        await foreach (var chunk in provider.StreamChatAsync(new ChatRequest()))
        {
            if (chunk.Delta.Text is { } text)
                chunks.Add(text);
        }

        string.Join("", chunks).Should().Be("Hello!");
    }

    [Fact]
    public async Task OllamaLlmProvider_ChatAsync_ServerError_ShouldReturnFailure()
    {
        _server
            .Given(Request.Create().WithPath("/api/chat").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("internal error"));

        var provider = new OllamaLlmProvider(
            _server.Url!, "qwen2.5:3b", TimeSpan.FromSeconds(10),
            new HttpClient(), _loggerFactory.CreateLogger<OllamaLlmProvider>());

        var result = await provider.ChatAsync(new ChatRequest());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("LLM_5XX");
    }

    [Fact]
    public async Task OllamaLlmProvider_IsAvailableAsync_ShouldReturnTrueWhenServerUp()
    {
        _server
            .Given(Request.Create().WithPath("/api/tags").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200));

        var provider = new OllamaLlmProvider(
            _server.Url!, "qwen2.5:3b", TimeSpan.FromSeconds(10),
            new HttpClient(), _loggerFactory.CreateLogger<OllamaLlmProvider>());

        var available = await provider.IsAvailableAsync();
        available.Should().BeTrue();
    }

    [Fact]
    public async Task OpenAICompatibleLlmProvider_ChatAsync_ShouldReturnContent()
    {
        _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    id = "chatcmpl-1",
                    model = "qwen2.5:3b",
                    choices = new[]
                    {
                        new
                        {
                            index = 0,
                            message = new { role = "assistant", content = "Hi there!" },
                            finish_reason = "stop",
                        }
                    },
                    usage = new { prompt_tokens = 8, completion_tokens = 3 },
                }));

        var provider = new OpenAICompatibleLlmProvider(
            "openai-compatible", _server.Url + "/v1/", "qwen2.5:3b", "", TimeSpan.FromSeconds(10),
            new HttpClient(), _loggerFactory.CreateLogger<OpenAICompatibleLlmProvider>());

        var result = await provider.ChatAsync(new ChatRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("Hi there!");
    }

    [Fact]
    public async Task OpenAICompatibleLlmProvider_StreamChatAsync_ShouldYieldTextChunks()
    {
        // SSE format: data: {...}\n\ndata: [DONE]\n\n
        var sse = "data: " + JsonSerializer.Serialize(new
        {
            model = "qwen2.5:3b",
            choices = new[]
            {
                new { index = 0, delta = new { content = "Hel" }, finish_reason = (string?)null }
            }
        }) + "\n\ndata: " + JsonSerializer.Serialize(new
        {
            model = "qwen2.5:3b",
            choices = new[]
            {
                new { index = 0, delta = new { content = "lo!" }, finish_reason = (string?)null }
            }
        }) + "\n\ndata: [DONE]\n\n";

        _server
            .Given(Request.Create().WithPath("/v1/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "text/event-stream")
                .WithBody(sse, encoding: Encoding.UTF8));

        var provider = new OpenAICompatibleLlmProvider(
            "openai-compatible", _server.Url + "/v1/", "qwen2.5:3b", "", TimeSpan.FromSeconds(10),
            new HttpClient(), _loggerFactory.CreateLogger<OpenAICompatibleLlmProvider>());

        var chunks = new List<string>();
        await foreach (var chunk in provider.StreamChatAsync(new ChatRequest()))
        {
            if (chunk.Delta.Text is { } text)
                chunks.Add(text);
        }

        string.Join("", chunks).Should().Be("Hello!");
    }

    [Fact]
    public void XmlToolCallParser_Parse_ShouldExtractToolCalls()
    {
        var text = "Let me check. <tool name=\"get_weather\" args='{\"city\":\"Tehran\"}'/> Done!";
        var calls = XmlToolCallParser.Parse(text);

        calls.Should().HaveCount(1);
        calls[0].ToolName.Should().Be("get_weather");
    }

    [Fact]
    public void XmlToolCallParser_Parse_NoTags_ShouldReturnEmpty()
    {
        var calls = XmlToolCallParser.Parse("just text, no tools");
        calls.Should().BeEmpty();
    }

    [Fact]
    public void XmlToolCallParser_StripToolTags_ShouldRemoveTags()
    {
        var text = "Let me check. <tool name=\"get_weather\" args='{\"city\":\"Tehran\"}'/> Done!";
        var stripped = XmlToolCallParser.StripToolTags(text);
        stripped.Should().Be("Let me check.  Done!");
    }

    [Fact]
    public async Task PromptAssembler_AssembleAsync_ShouldIncludeSystemPromptAndMemory()
    {
        var llm = new FakeLlmProvider();
        var assembler = new PromptAssembler(llm, _loggerFactory.CreateLogger<PromptAssembler>());

        var request = await assembler.AssembleAsync(
            systemPrompt: "You are a companion.",
            memoryContext: "[semantic] user likes coffee",
            history: new List<ChatMessage> { new("user", "hi"), new("assistant", "hello") },
            userInput: "what do you know about me?",
            tools: Array.Empty<ToolSchema>(),
            contextWindow: 8192,
            maxResponseTokens: 1024);

        request.SystemPrompt.Should().Contain("You are a companion.");
        request.SystemPrompt.Should().Contain("# Context (memories)");
        request.SystemPrompt.Should().Contain("user likes coffee");
        request.Messages.Should().HaveCount(3); // 2 history + 1 user
        request.Messages.Last().Content.Should().Be("what do you know about me?");
    }

    [Fact]
    public async Task PromptAssembler_AssembleAsync_ShouldTruncateHistoryWhenOverBudget()
    {
        var llm = new FakeLlmProvider();
        var assembler = new PromptAssembler(llm, _loggerFactory.CreateLogger<PromptAssembler>());

        // Very small budget to force truncation — each message is ~10 tokens (40 chars / 4)
        var history = new List<ChatMessage>
        {
            new("user", "old message 1 that is quite long to exceed budget"),
            new("assistant", "old reply 1 that is also long enough to exceed"),
            new("user", "recent message that is also long"),
            new("assistant", "recent reply that is also long"),
        };

        var request = await assembler.AssembleAsync(
            systemPrompt: "system",
            memoryContext: "",
            history: history,
            userInput: "current",
            tools: Array.Empty<ToolSchema>(),
            contextWindow: 50,  // tiny budget: system(~2t) + user(~2t) = ~4t, leaving ~46t for history
            maxResponseTokens: 10);

        // Should have truncated (fewer than all 4 history messages + 1 user = 5)
        request.Messages.Count.Should().BeLessThanOrEqualTo(5);
        // Current user input should always be present
        request.Messages.Last().Content.Should().Be("current");
    }

    [Fact]
    public async Task StreamAdapter_StreamAsync_ShouldCollectTextAndToolCalls()
    {
        var adapter = new StreamAdapter();
        var chunks = new List<ChatChunk>
        {
            new(new ChatDelta(Text: "Hello ")),
            new(new ChatDelta(Text: "world!")),
        };

        var tokens = new List<string>();
        var (fullText, toolCalls) = await adapter.StreamAsync(
            chunks.ToAsyncEnumerable(), t => tokens.Add(t));

        fullText.Should().Be("Hello world!");
        tokens.Should().Equal("Hello ", "world!");
        toolCalls.Should().BeEmpty();
    }

    [Fact]
    public async Task ConversationService_StreamConversationAsync_ShouldReturnTurn()
    {
        var llm = new FakeLlmProvider
        {
            NextStream = new[]
            {
                new ChatChunk(new ChatDelta(Text: "Hi ")),
                new ChatChunk(new ChatDelta(Text: "there!")),
            }
        };
        var assembler = new PromptAssembler(llm, _loggerFactory.CreateLogger<PromptAssembler>());
        var adapter = new StreamAdapter();
        var service = new ConversationService(llm, assembler, adapter, _loggerFactory.CreateLogger<ConversationService>());

        var tokens = new List<string>();
        var result = await service.StreamConversationAsync(
            "system", "", Array.Empty<ChatMessage>(), "hello",
            Array.Empty<ToolSchema>(), t => tokens.Add(t));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Text.Should().Be("Hi there!");
        string.Join("", tokens).Should().Be("Hi there!");
    }
}

/// <summary>
/// Extension to convert a list to a simple async enumerable for tests.
/// </summary>
internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
        {
            await Task.Yield();
            yield return item;
        }
    }
}