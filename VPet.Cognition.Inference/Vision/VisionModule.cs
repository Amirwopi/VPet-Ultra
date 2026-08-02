using Microsoft.Extensions.Logging;
using VPet.Cognition.Inference.Abstractions;
using VPet.Cognition.Inference.Models;

namespace VPet.Cognition.Inference.Vision;

/// <summary>
/// Optional vision module. Can be enabled/disabled at runtime to save RAM on weak systems.
/// Uses a vision-capable model (SmolVLM, LLaVA) to describe images and answer questions about them.
/// </summary>
public sealed class VisionModule : IAsyncDisposable
{
    private readonly IInferenceProvider _provider;
    private readonly ILogger<VisionModule> _logger;
    private bool _enabled;

    /// <summary>Whether the vision module is currently enabled.</summary>
    public bool IsEnabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            _logger.LogInformation("Vision module {State}", value ? "enabled" : "disabled");
        }
    }

    /// <summary>Whether the current provider supports vision.</summary>
    public bool IsAvailable =>
        _enabled && _provider.Capabilities.HasFlag(InferenceCapabilities.Vision);

    public VisionModule(IInferenceProvider provider, bool enabledByDefault, ILogger<VisionModule> logger)
    {
        _provider = provider;
        _enabled = enabledByDefault;
        _logger = logger;
    }

    /// <summary>
    /// Describe an image.
    /// </summary>
    public async Task<Result<string>> DescribeImageAsync(byte[] imageBytes, string? question = null, CancellationToken ct = default)
    {
        if (!_enabled)
            return Result<string>.Fail("Vision module is disabled.");

        if (!_provider.Capabilities.HasFlag(InferenceCapabilities.Vision))
            return Result<string>.Fail("Current model does not support vision.");

        var prompt = string.IsNullOrEmpty(question)
            ? "Describe this image in detail."
            : question;

        var request = new InferenceRequest
        {
            SystemPrompt = "You are a vision-capable assistant. Analyze the image and respond accurately.",
            Messages = new List<InferenceMessage>
            {
                new() { Role = "user", Content = prompt, ImageBytes = imageBytes },
            },
            MaxTokens = 512,
            Temperature = 0.3,
            ImageBytes = imageBytes,
        };

        var result = await _provider.CompleteAsync(request, ct);
        if (!result.IsSuccess)
            return Result<string>.Fail(result.Error!);

        _logger.LogInformation("Vision described image: {Len} chars", result.Value!.Text.Length);
        return Result<string>.Ok(result.Value.Text);
    }

    /// <summary>
    /// Answer a question about an image.
    /// </summary>
    public async Task<Result<string>> AskAboutImageAsync(byte[] imageBytes, string question, CancellationToken ct = default)
    {
        return await DescribeImageAsync(imageBytes, question, ct);
    }

    /// <summary>
    /// Check if a vision model is available (either loaded locally or on the server).
    /// </summary>
    public bool IsVisionModelAvailable()
    {
        return _provider.AvailableModels.Any(m => m.SupportsVision);
    }

    /// <summary>
    /// Get all vision-capable models from the provider.
    /// </summary>
    public IReadOnlyList<ModelInfo> GetVisionModels()
    {
        return _provider.AvailableModels.Where(m => m.SupportsVision).ToList();
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}