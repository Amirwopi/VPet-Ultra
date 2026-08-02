using System.IO;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using VPet.Cognition.AgentRuntime;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Events;
using VPet.Cognition.Host.Config;
using VPet.Cognition.Host.EventBus;
using VPet.Cognition.Host.Options;
using VPet.Cognition.Host.Services;
using VPet.Cognition.Inference;
using VPet.Cognition.Inference.Abstractions;
using VPet.Cognition.Inference.Local;
using VPet.Cognition.Inference.Models;
using VPet.Cognition.Inference.Online;
using VPet.Cognition.Inference.Vision;
using VPet.Cognition.LLM;
using VPet.Cognition.LLM.Ollama;
using VPet.Cognition.LLM.OpenAICompatible;
using VPet.Cognition.MemoryStore;
using VPet.Cognition.PersonalityEngine;
using VPet.Cognition.Security;
using VPet.Cognition.Voice.Stt;
using VPet.Cognition.Voice.Tts;
using VPet_Simulator.Windows.Interface;
using PersonalitySeed = VPet.Cognition.PersonalityEngine.PersonalitySeed;

namespace VPet.Cognition.Host;

/// <summary>
/// The <see cref="MainPlugin"/> entry point for the cognition host. Discovered and instantiated
/// by the existing <c>CoreMOD</c> plugin loader via <c>Activator.CreateInstance(type, mw)</c>.
/// </summary>
public sealed class CognitionPlugin : MainPlugin
{
    public override string PluginName => "VPetCognition";

    private ServiceProvider? _services;
    private CognitionHost? _host;
    private ILogger<CognitionPlugin>? _logger;
    private List<Microsoft.Extensions.Hosting.IHostedService>? _backgroundServices;

    public CognitionPlugin(IMainWindow mainwin) : base(mainwin)
    {
        // Per MainPlugin contract: do NOT load game/save data here; no UI thread yet.
    }

    public override void LoadPlugin()
    {
        _services = CognitionHostBuilder.Build(MW);
        _logger = _services.GetRequiredService<ILogger<CognitionPlugin>>();
        _host = _services.GetRequiredService<CognitionHost>();
        _logger.LogInformation("CognitionPlugin loaded. LLM provider={Provider}, model={Model}",
            _services.GetRequiredService<IOptions<CognitionOptions>>().Value.Llm.Provider,
            _services.GetRequiredService<IOptions<CognitionOptions>>().Value.Llm.Model);

        // Start background services
        _backgroundServices = _services.GetServices<Microsoft.Extensions.Hosting.IHostedService>().ToList();
        foreach (var svc in _backgroundServices)
        {
            _ = svc.StartAsync(CancellationToken.None);
        }
    }

    public override void GameLoaded()
    {
        _logger?.LogInformation("CognitionPlugin GameLoaded — attaching to pet.");
        _host?.AttachToPet(MW.Main);
    }

    public override void Save()
    {
        _logger?.LogDebug("CognitionPlugin Save.");
    }

    public override void EndGame()
    {
        _logger?.LogInformation("CognitionPlugin EndGame — disposing.");
        _host?.DetachFromPet();

        // Stop background services
        if (_backgroundServices is not null)
        {
            foreach (var svc in _backgroundServices)
            {
                try { _ = svc.StopAsync(CancellationToken.None); }
                catch { /* ignore */ }
            }
        }

        _services?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public override void Setting()
    {
        _logger?.LogInformation("Opening cognition settings window.");
        _services?.GetService<VPet.Cognition.Wpf.CognitionWpfModule>()?.ShowSettingsWindow();
    }

    public override void LoadDIY()
    {
        // Add a "Chat" button to the pet's toolbar (existing extension point)
        if (MW.Main?.ToolBar is { } toolbar)
        {
            toolbar.AddMenuButton(VPet_Simulator.Core.ToolBar.MenuType.DIY, "Chat", () =>
            {
                _services?.GetService<VPet.Cognition.Wpf.CognitionWpfModule>()?.ShowConversationWindow();
            });
            _logger?.LogInformation("Added Chat toolbar button.");
        }
    }
}

/// <summary>
/// The orchestrator that attaches cognition to the pet's <c>Main</c> control.
/// </summary>
public sealed class CognitionHost : ICognitionFacade
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<CognitionHost> _logger;
    private readonly IOptions<CognitionOptions> _options;
    private readonly ILLMProvider _llmProvider;
    private readonly IMemoryStore _memoryStore;
    private readonly MemoryService _memoryService;
    private readonly IPersonalityEngine _personalityEngine;
    private readonly IEmotionEngine _emotionEngine;
    private readonly IAgentRuntime _agentRuntime;
    private readonly ConversationService _conversationService;
    private readonly PromptAssembler _promptAssembler;
    private readonly CognitionStore? _cognitionStore;

    private VPet_Simulator.Core.Main? _main;
    private int _turnCount;
    private Timer? _emotionDecayTimer;

    public CognitionHost(
        IEventBus eventBus,
        ILogger<CognitionHost> logger,
        IOptions<CognitionOptions> options,
        ILLMProvider llmProvider,
        IMemoryStore memoryStore,
        MemoryService memoryService,
        IPersonalityEngine personalityEngine,
        IEmotionEngine emotionEngine,
        IAgentRuntime agentRuntime,
        ConversationService conversationService,
        PromptAssembler promptAssembler,
        CognitionStore? cognitionStore = null)
    {
        _eventBus = eventBus;
        _logger = logger;
        _options = options;
        _llmProvider = llmProvider;
        _memoryStore = memoryStore;
        _memoryService = memoryService;
        _personalityEngine = personalityEngine;
        _emotionEngine = emotionEngine;
        _agentRuntime = agentRuntime;
        _conversationService = conversationService;
        _promptAssembler = promptAssembler;
        _cognitionStore = cognitionStore;
    }

    /// <summary>Attach cognition to the pet's <c>Main</c> control.</summary>
    public void AttachToPet(VPet_Simulator.Core.Main? main)
    {
        if (main is null)
        {
            _logger.LogWarning("AttachToPet called with null Main; skipping.");
            return;
        }

        _main = main;
        _logger.LogInformation("Attached to pet. Cognition enabled={Enabled}, LLM={Provider}",
            _options.Value.Enabled, _llmProvider.ProviderId);

        // Subscribe to emotion events to drive animation
        _eventBus.Subscribe<EmotionChangedEvent>(
            (evt, ct) => { OnEmotionChanged(evt); return Task.CompletedTask; });

        // Start emotion decay timer (every 10 seconds)
        var decayInterval = TimeSpan.FromSeconds(10);
        _emotionDecayTimer = new Timer(_ => DecayEmotions(), null, decayInterval, decayInterval);

        // Load persisted personality/emotion state from memory
        _ = LoadPersistedStateAsync();
    }

    private void OnEmotionChanged(EmotionChangedEvent evt)
    {
        if (_main is null) return;

        try
        {
            // Map emotion to animation and trigger on the pet's Main control
            var (graphType, animatType) = MapEmotionToAnimation(evt.After);
            _logger.LogDebug("Emotion changed: joy={Joy:F2} sadness={Sad:F2} anger={Anger:F2} → {Graph}/{Animat}",
                evt.After.Joy, evt.After.Sadness, evt.After.Anger, graphType, animatType);

            // Trigger animation on the UI thread
            _main.Dispatcher.Invoke(() =>
            {
                // Only override if emotion is strong enough
                if (evt.After.Joy > _options.Value.Animation.EmotionOverrideThreshold)
                    _main.Display(graphType, animatType, (Action<string>?)null);
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply emotion animation.");
        }
    }

    private static (VPet_Simulator.Core.GraphInfo.GraphType, VPet_Simulator.Core.GraphInfo.AnimatType) MapEmotionToAnimation(
        VPet.Cognition.Domain.ValueObjects.EmotionVector emotion)
    {
        // Find the dominant emotion
        var max = new[]
        {
            (Emotion: "Joy", Value: emotion.Joy),
            (Emotion: "Sadness", Value: emotion.Sadness),
            (Emotion: "Anger", Value: emotion.Anger),
            (Emotion: "Curiosity", Value: emotion.Curiosity),
            (Emotion: "Excitement", Value: emotion.Excitement),
            (Emotion: "Fear", Value: emotion.Fear),
            (Emotion: "Calmness", Value: emotion.Calmness),
            (Emotion: "Surprise", Value: emotion.Surprise),
            (Emotion: "Stress", Value: emotion.Stress),
            (Emotion: "Frustration", Value: emotion.Frustration),
        }.OrderByDescending(x => x.Value).First();

        return max.Emotion switch
        {
            "Joy" => (VPet_Simulator.Core.GraphInfo.GraphType.Touch_Head, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Sadness" => (VPet_Simulator.Core.GraphInfo.GraphType.Sleep, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Anger" => (VPet_Simulator.Core.GraphInfo.GraphType.Touch_Body, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Curiosity" => (VPet_Simulator.Core.GraphInfo.GraphType.Idel, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Excitement" => (VPet_Simulator.Core.GraphInfo.GraphType.Idel, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Fear" => (VPet_Simulator.Core.GraphInfo.GraphType.Switch_Down, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Calmness" => (VPet_Simulator.Core.GraphInfo.GraphType.Idel, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Surprise" => (VPet_Simulator.Core.GraphInfo.GraphType.Idel, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Stress" => (VPet_Simulator.Core.GraphInfo.GraphType.Work, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            "Frustration" => (VPet_Simulator.Core.GraphInfo.GraphType.Idel, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
            _ => (VPet_Simulator.Core.GraphInfo.GraphType.Idel, VPet_Simulator.Core.GraphInfo.AnimatType.A_Start),
        };
    }

    private void DecayEmotions()
    {
        try
        {
            _ = _emotionEngine.DecayAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Emotion decay tick failed.");
        }
    }

    private async Task LoadPersistedStateAsync()
    {
        try
        {
            // Load personality state from CognitionStore
            if (_cognitionStore is not null)
            {
                var personalityJson = await _cognitionStore.LoadPersonalityAsync();
                if (!string.IsNullOrEmpty(personalityJson))
                {
                    _logger.LogInformation("Loaded personality state from persistence.");
                }

                var emotionJson = await _cognitionStore.LoadEmotionAsync();
                if (!string.IsNullOrEmpty(emotionJson))
                {
                    _logger.LogInformation("Loaded emotion state from persistence.");
                    // TODO: restore emotion vector from JSON
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load persisted cognitive state.");
        }
    }

    /// <summary>
    /// Process a conversation turn: retrieve memory context, apply personality,
    /// feed emotion stimulus, and store the result.
    /// </summary>
    public async Task<string> ProcessConversationTurnAsync(string userInput, CancellationToken ct = default)
    {
        _turnCount++;

        // 1. Retrieve memory context for the user input
        string memoryContext = "";
        try
        {
            var context = await _memoryService.RetrieveContextAsync(userInput, maxMemories: 5, ct);
            memoryContext = context;
            _logger.LogDebug("Retrieved {Len} chars of memory context.", context.Length);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Memory retrieval failed; continuing without context.");
        }

        // 2. Get personality system prompt
        string personalityPrompt = "";
        try
        {
            personalityPrompt = await _personalityEngine.RenderSystemPromptAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Personality prompt rendering failed.");
        }

        // 3. Analyze sentiment and apply emotion stimulus
        try
        {
            var stimulus = AnalyzeSentiment(userInput);
            if (stimulus is not null)
            {
                await _emotionEngine.ApplyAsync(stimulus, ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Emotion stimulus application failed.");
        }

        // 4. Evolve personality based on interaction
        try
        {
            var delta = new VPet.Cognition.Domain.Abstractions.PersonalityDelta
            {
                FamiliarityDelta = 0.01,
                TrustDelta = 0.005,
                AffectionDelta = 0.003,
                Reason = "conversation_turn",
            };
            await _personalityEngine.EvolveAsync(delta, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Personality evolution failed.");
        }

        // 5. Periodic reflection (every N turns)
        if (_turnCount % _options.Value.Personality.ReflectionIntervalTurns == 0)
        {
            try
            {
                await _personalityEngine.ReflectAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Personality reflection failed.");
            }
        }

        return memoryContext;
    }

    /// <summary>
    /// Simple rule-based sentiment analysis that maps user input to an emotion stimulus.
    /// </summary>
    private static VPet.Cognition.Domain.Entities.EmotionStimulus? AnalyzeSentiment(string input)
    {
        var lower = input.ToLowerInvariant();

        // Check each emotion type and pick the strongest match
        var matches = new List<(VPet.Cognition.Domain.Enums.EmotionType Type, double Intensity)>();

        if (AnyKeyword(lower, "happy", "joy", "great", "awesome", "love", "wonderful", "amazing", "خوشحال", "عالی"))
            matches.Add((VPet.Cognition.Domain.Enums.EmotionType.Joy, 0.6));

        if (AnyKeyword(lower, "sad", "depressed", "unhappy", "cry", "lonely", "غمگین", "ناراحت"))
            matches.Add((VPet.Cognition.Domain.Enums.EmotionType.Sadness, 0.5));

        if (AnyKeyword(lower, "angry", "mad", "furious", "hate", "damn", "عصبانی", "نفرت"))
            matches.Add((VPet.Cognition.Domain.Enums.EmotionType.Anger, 0.5));

        if (AnyKeyword(lower, "scared", "afraid", "worried", "anxious", "ترس", "نگران"))
            matches.Add((VPet.Cognition.Domain.Enums.EmotionType.Fear, 0.4));

        if (AnyKeyword(lower, "what", "why", "how", "curious", "interesting", "چی", "چرا", "چطور"))
            matches.Add((VPet.Cognition.Domain.Enums.EmotionType.Curiosity, 0.4));

        if (AnyKeyword(lower, "wow", "really", "unexpected", "surprising", "واو", "واقعا"))
            matches.Add((VPet.Cognition.Domain.Enums.EmotionType.Surprise, 0.5));

        if (AnyKeyword(lower, "excited", "amazing", "can't wait", "هیجان"))
            matches.Add((VPet.Cognition.Domain.Enums.EmotionType.Excitement, 0.5));

        if (AnyKeyword(lower, "calm", "relax", "peaceful", "quiet", "آرام", "راحت"))
            matches.Add((VPet.Cognition.Domain.Enums.EmotionType.Calmness, 0.4));

        if (matches.Count == 0)
            return null;

        var dominant = matches.OrderByDescending(m => m.Intensity).First();
        var blends = matches.Where(m => m.Type != dominant.Type)
            .ToDictionary(m => m.Type, m => m.Intensity * 0.5);

        return new VPet.Cognition.Domain.Entities.EmotionStimulus
        {
            Primary = dominant.Type,
            Intensity = dominant.Intensity,
            Source = "user_input",
            Blends = blends.Count > 0 ? blends : null,
        };
    }

    private static bool AnyKeyword(string text, params string[] keywords)
    {
        foreach (var kw in keywords)
            if (text.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>
    /// Store a completed conversation turn to memory.
    /// </summary>
    public async Task StoreConversationTurnAsync(string userInput, string assistantResponse, CancellationToken ct = default)
    {
        try
        {
            await _memoryService.RememberConversationAsync(userInput, assistantResponse, "conversation", null, ct);
            _logger.LogDebug("Stored conversation turn to memory.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to store conversation turn to memory.");
        }
    }

    /// <summary>
    /// Stream LLM response directly to the pet's MessageBar (SayInfoWithStream).
    /// This is the in-pet conversation mode (no separate window).
    /// </summary>
    public async Task StreamToPetAsync(string userInput, CancellationToken ct = default)
    {
        if (_main is null)
        {
            _logger.LogWarning("Cannot stream to pet — Main is null.");
            return;
        }

        // 1. Process conversation turn (memory, personality, emotion)
        string memoryContext = await ProcessConversationTurnAsync(userInput, ct);

        // 2. Create SayInfoWithStream for the pet's MessageBar
        var sayInfo = new VPet_Simulator.Core.SayInfoWithStream();

        // 3. Start the Say on the UI thread
        _main.Dispatcher.Invoke(() => _main.Say(sayInfo));

        // 4. Stream LLM tokens to the SayInfo
        try
        {
            var systemPrompt = await _personalityEngine.RenderSystemPromptAsync(ct);
            var request = new ChatRequest
            {
                SystemPrompt = systemPrompt,
                Messages = new[]
                {
                    new ChatMessageDto("user", userInput),
                },
                MaxTokens = _options.Value.Llm.MaxTokens,
                Temperature = _options.Value.Llm.Temperature,
            };

            await foreach (var chunk in _llmProvider.StreamChatAsync(request, ct))
            {
                if (chunk.Delta?.Text is { } token && !string.IsNullOrEmpty(token))
                {
                    // Append token to SayInfo on the UI thread
                    _main.Dispatcher.Invoke(() => sayInfo.UpdateText(token));
                }
            }

            // Finish the stream
            _main.Dispatcher.Invoke(() => sayInfo.FinishGenerate());

            // Store to memory
            var fullText = sayInfo.CurrentText.ToString();
            await StoreConversationTurnAsync(userInput, fullText, ct);
        }
        catch (OperationCanceledException)
        {
            _main.Dispatcher.Invoke(() => sayInfo.FinishGenerate());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StreamToPet failed.");
            _main.Dispatcher.Invoke(() =>
            {
                sayInfo.UpdateText($" [Error: {ex.Message}]");
                sayInfo.FinishGenerate();
            });
        }
    }

    public void DetachFromPet()
    {
        _emotionDecayTimer?.Dispose();
        _emotionDecayTimer = null;
        _main = null;
        _logger.LogInformation("Detached from pet.");
    }
}

/// <summary>
/// DI composition root for the cognition host.
/// </summary>
public static class CognitionHostBuilder
{
    public static ServiceProvider Build(IMainWindow mw)
    {
        var services = new ServiceCollection();

        // ── Configuration: read appsettings.json from the VPet base directory ──
        var baseDir = Path.GetDirectoryName(
            System.Reflection.Assembly.GetAssembly(typeof(MainPlugin))?.Location) ?? ".";
        var configPath = Path.Combine(baseDir, "appsettings.json");

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(baseDir)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
        var configuration = configBuilder.Build();

        // Bind CognitionOptions from the "Cognition" section
        var options = new CognitionOptions();
        configuration.GetSection("Cognition").Bind(options);
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(options));
        services.AddSingleton<IConfiguration>(configuration);

        // ── Logging ──
        var logLevel = options.Logging.Level switch
        {
            "Trace" => LogLevel.Trace,
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            _ => LogLevel.Information,
        };

        // Serilog file + console
        var logDir = Path.GetDirectoryName(options.Logging.File) ?? "logs";
        if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
            Directory.CreateDirectory(logDir);

        var serilogConfig = new LoggerConfiguration()
            .MinimumLevel.Is(logLevel switch
            {
                LogLevel.Trace => Serilog.Events.LogEventLevel.Verbose,
                LogLevel.Debug => Serilog.Events.LogEventLevel.Debug,
                LogLevel.Information => Serilog.Events.LogEventLevel.Information,
                LogLevel.Warning => Serilog.Events.LogEventLevel.Warning,
                LogLevel.Error => Serilog.Events.LogEventLevel.Error,
                _ => Serilog.Events.LogEventLevel.Information,
            })
            .WriteTo.File(
                path: string.IsNullOrEmpty(options.Logging.File) ? "logs/cognition-.log" : options.Logging.File,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.Console();

        Log.Logger = serilogConfig.CreateLogger();

        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
            builder.AddDebug();
            builder.SetMinimumLevel(logLevel);
        });

        // ── HttpClient factory (avoids socket exhaustion) ──
        services.AddHttpClient("cognition", client =>
        {
            client.Timeout = options.Llm.Timeout;
        });

        // ── Config loader ──
        services.AddSingleton<CognitionConfigLoader>();

        // ── Event bus ──
        services.AddSingleton<IEventBus, EventBus.EventBus>();

        // ── LLM provider selection by config (with fallback chain + retry) ──
        services.AddSingleton<ILLMProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            ILLMProvider primary = CreateProvider(opts.Llm, httpClientFactory, loggerFactory);
            ILLMProvider? fallback = null;

            // Build fallback if configured
            if (!string.IsNullOrEmpty(opts.Llm.FallbackProvider))
            {
                var fallbackOpts = new LlmOptions
                {
                    Provider = opts.Llm.FallbackProvider!,
                    Endpoint = opts.Llm.FallbackEndpoint ?? "http://localhost:11434",
                    Model = opts.Llm.FallbackModel ?? "qwen2.5:3b",
                    ApiKey = opts.Llm.FallbackApiKey ?? "",
                    Timeout = opts.Llm.Timeout,
                };
                fallback = CreateProvider(fallbackOpts, httpClientFactory, loggerFactory);
            }

            if (fallback is not null)
            {
                return new FallbackLlmProvider(primary, fallback, maxRetries: 2,
                    loggerFactory.CreateLogger<FallbackLlmProvider>());
            }

            // No fallback — still wrap in retry provider
            return new FallbackLlmProvider(primary, null, maxRetries: 2,
                loggerFactory.CreateLogger<FallbackLlmProvider>());
        });

        // ── Token counter ──
        services.AddSingleton<TokenCounter>();

        // ── LLM services ──
        services.AddSingleton<PromptAssembler>();
        services.AddSingleton<StreamAdapter>();
        services.AddSingleton<ConversationService>();

        // ── Inference engine — local (LLamaSharp) or online ──
        services.AddSingleton<IInferenceProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var llm = opts.Llm;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            return llm.Provider.ToLowerInvariant() switch
            {
                "local" or "local-llama" or "llamasharp" => new LocalInferenceProvider(
                    llm.ModelsDirectory, llm.GpuLayerCount, llm.ContextSize,
                    loggerFactory.CreateLogger<LocalInferenceProvider>()),

                "ollama" => new OnlineInferenceProvider(
                    "ollama", llm.Endpoint, "", llm.Timeout,
                    httpClientFactory.CreateClient("cognition"), loggerFactory.CreateLogger<OnlineInferenceProvider>()),

                "openai-compatible" or "openai" => new OnlineInferenceProvider(
                    "openai-compatible", llm.Endpoint, llm.ApiKey, llm.Timeout,
                    httpClientFactory.CreateClient("cognition"), loggerFactory.CreateLogger<OnlineInferenceProvider>()),

                _ => new LocalInferenceProvider(
                    "models", 0, 4096,
                    loggerFactory.CreateLogger<LocalInferenceProvider>()),
            };
        });

        // ── Model manager ──
        services.AddSingleton<ModelManager>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<ModelManager>>();
            return new ModelManager(opts.Llm.ModelsDirectory, logger);
        });

        // ── Vision module (optional) ──
        services.AddSingleton<VisionModule>(sp =>
        {
            var provider = sp.GetRequiredService<IInferenceProvider>();
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<VisionModule>>();
            return new VisionModule(provider, opts.Llm.EnableVision, logger);
        });

        // ── Memory store ──
        services.AddSingleton<IMemoryStore>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<SqliteMemoryStore>>();
            var dir = Path.GetDirectoryName(opts.Memory.DbPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return new SqliteMemoryStore(opts.Memory.DbPath, logger);
        });
        services.AddSingleton<MemoryService>();

        // ── Embedding provider (local hashing or online) ──
        services.AddSingleton<IEmbeddingProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();

            return opts.Memory.EmbeddingProvider.ToLowerInvariant() switch
            {
                "online" or "ollama" => new OnlineEmbeddingProvider(
                    httpClientFactory.CreateClient("cognition"),
                    opts.Llm.Endpoint,
                    opts.Memory.EmbeddingModel,
                    opts.Llm.ApiKey,
                    opts.Memory.VectorDimensions,
                    loggerFactory.CreateLogger<OnlineEmbeddingProvider>()),

                _ => new LocalEmbeddingProvider(
                    opts.Memory.VectorDimensions,
                    loggerFactory.CreateLogger<LocalEmbeddingProvider>()),
            };
        });

        // ── Memory service (with embedding provider) ──
        services.AddSingleton<MemoryService>(sp =>
        {
            var store = sp.GetRequiredService<IMemoryStore>();
            var emb = sp.GetRequiredService<IEmbeddingProvider>();
            var logger = sp.GetRequiredService<ILogger<MemoryService>>();
            return new MemoryService(store, logger, emb);
        });

        // ── Cognition store (personality/emotion/goals/permissions/audit persistence) ──
        services.AddSingleton<CognitionStore>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<CognitionStore>>();
            return new CognitionStore(opts.Memory.DbPath, logger);
        });

        // ── Background services (memory maintenance, emotion persistence, personality evaluation) ──
        services.AddHostedService<VPet.Cognition.Host.Services.MemoryMaintenanceService>();
        services.AddHostedService<VPet.Cognition.Host.Services.EmotionPersistenceService>();
        services.AddHostedService<VPet.Cognition.Host.Services.PersonalityEvaluationService>();

        // ── Voice providers (STT) ──
        services.AddSingleton<ISttProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var stt = opts.Voice.Stt;
            var logger = sp.GetRequiredService<ILogger<WhisperSttProvider>>();
            return new WhisperSttProvider(stt.ExecutablePath, stt.ModelPath, stt.Language, stt.Threads, logger);
        });

        // ── Voice providers (TTS) ──
        services.AddSingleton<ITtsProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var tts = opts.Voice.Tts;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            return tts.Provider.ToLowerInvariant() switch
            {
                "kokoro" => new KokoroTtsProvider(
                    tts.ExecutablePath, tts.ModelPath, tts.KokoroVoice, tts.KokoroSpeed,
                    TimeSpan.FromSeconds(15), loggerFactory.CreateLogger<KokoroTtsProvider>()),

                _ => new PiperTtsProvider(
                    tts.ExecutablePath, tts.ModelPath, tts.SpeakerId,
                    tts.LengthScale, tts.NoiseScale, tts.NoiseW,
                    TimeSpan.FromSeconds(15), loggerFactory.CreateLogger<PiperTtsProvider>()),
            };
        });

        // ── Cognition host ──
        services.AddSingleton<CognitionHost>();
        services.AddSingleton<VPet.Cognition.Domain.Abstractions.ICognitionFacade>(sp => sp.GetRequiredService<CognitionHost>());

        // ── Personality + Emotion ──
        services.AddSingleton<IPersonalityEngine>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<DefaultPersonalityEngine>>();
            var llm = sp.GetService<ILLMProvider>();
            var eventBus = sp.GetRequiredService<IEventBus>();
            var store = sp.GetService<CognitionStore>();

            var engine = new DefaultPersonalityEngine(
                new PersonalitySeed { Name = opts.Personality.Seed.Name, Description = opts.Personality.Seed.Description },
                opts.Personality.EvolutionRate, opts.Personality.CoreTraitMaxDrift, logger)
            { LlmProvider = llm, EventBus = eventBus };

            // Wire persistence
            if (store is not null)
            {
                engine.LoadAsync = async () => await store.LoadPersonalityAsync();
                engine.PersistAsync = async (json) => await store.SavePersonalityAsync(json);
            }

            return engine;
        });
        services.AddSingleton<IEmotionEngine>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<DefaultEmotionEngine>>();
            var eventBus = sp.GetRequiredService<IEventBus>();
            return new DefaultEmotionEngine(
                opts.Emotion.BlendFactor, opts.Emotion.DecayRatePerSec,
                opts.Emotion.AntagonisticSuppression, logger)
            { EventBus = eventBus };
        });

        // ── Agent + Tools + Permission ──
        services.AddSingleton<IPermissionService>(sp =>
        {
            var store = sp.GetService<CognitionStore>();
            var logger = sp.GetRequiredService<ILogger<DefaultPermissionService>>();
            return new DefaultPermissionService(logger, store);
        });

        // Register all tool providers
        services.AddSingleton<IToolProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new VPet.Cognition.AgentRuntime.Tools.BrowserTool(loggerFactory.CreateLogger<VPet.Cognition.AgentRuntime.Tools.BrowserTool>());
        });
        services.AddSingleton<IToolProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new VPet.Cognition.AgentRuntime.Tools.PowerShellTool(loggerFactory.CreateLogger<VPet.Cognition.AgentRuntime.Tools.PowerShellTool>());
        });
        services.AddSingleton<IToolProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new VPet.Cognition.AgentRuntime.Tools.CmdTool(loggerFactory.CreateLogger<VPet.Cognition.AgentRuntime.Tools.CmdTool>());
        });
        services.AddSingleton<IToolProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new VPet.Cognition.AgentRuntime.Tools.FileWriteTool(loggerFactory.CreateLogger<VPet.Cognition.AgentRuntime.Tools.FileWriteTool>());
        });
        services.AddSingleton<IToolProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new VPet.Cognition.AgentRuntime.Tools.FolderTool(loggerFactory.CreateLogger<VPet.Cognition.AgentRuntime.Tools.FolderTool>());
        });
        services.AddSingleton<IAgentRuntime>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var tools = sp.GetServices<IToolProvider>().ToArray();
            var perm = sp.GetRequiredService<IPermissionService>();
            var llm = sp.GetService<ILLMProvider>();
            var logger = sp.GetRequiredService<ILogger<DefaultAgentRuntime>>();
            return new DefaultAgentRuntime(
                tools.FirstOrDefault() ?? tools[0], perm, opts.Agent.MaxStepsPerPlan, opts.Agent.MaxRetries,
                opts.Agent.ConfidenceThreshold, logger)
            { LlmProvider = llm };
        });

        // ── WPF module (Windows only) ──
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<VPet.Cognition.Wpf.CognitionWpfModule>();
            services.AddSingleton<VPet.Cognition.Wpf.ViewModels.ConversationVM>();
            services.AddSingleton<VPet.Cognition.Wpf.ViewModels.CognitionSettingsVM>();
        }

        return services.BuildServiceProvider();
    }

    private static ILLMProvider CreateProvider(LlmOptions llm, IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        var httpClient = httpClientFactory.CreateClient("cognition");

        return llm.Provider.ToLowerInvariant() switch
        {
            "ollama" => new OllamaLlmProvider(
                llm.Endpoint, llm.Model, llm.Timeout,
                httpClient, loggerFactory.CreateLogger<OllamaLlmProvider>()),

            "openai-compatible" or "openai" => new OpenAICompatibleLlmProvider(
                "openai-compatible", llm.Endpoint, llm.Model,
                llm.ApiKey, llm.Timeout,
                httpClient, loggerFactory.CreateLogger<OpenAICompatibleLlmProvider>()),

            // "local" uses the Inference engine — register an Ollama provider as a bridge
            // (the LocalInferenceProvider is registered separately as IInferenceProvider)
            _ => new OllamaLlmProvider(
                "http://localhost:11434", "qwen2.5:3b", TimeSpan.FromMinutes(2),
                httpClient, loggerFactory.CreateLogger<OllamaLlmProvider>()),
        };
    }
}