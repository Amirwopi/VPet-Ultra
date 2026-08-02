using System.IO;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using VPet.Cognition.AgentRuntime;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Host.EventBus;
using VPet.Cognition.Host.Options;
using VPet.Cognition.LLM;
using VPet.Cognition.LLM.Ollama;
using VPet.Cognition.LLM.OpenAICompatible;
using VPet.Cognition.MemoryStore;
using VPet.Cognition.PersonalityEngine;
using VPet.Cognition.Security;
using VPet.Cognition.Voice.Stt;
using VPet.Cognition.Voice.Tts;
using VPet_Simulator.Windows.Interface;

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
public sealed class CognitionHost
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<CognitionHost> _logger;
    private readonly IOptions<CognitionOptions> _options;
    private readonly ILLMProvider _llmProvider;

    public CognitionHost(
        IEventBus eventBus,
        ILogger<CognitionHost> logger,
        IOptions<CognitionOptions> options,
        ILLMProvider llmProvider)
    {
        _eventBus = eventBus;
        _logger = logger;
        _options = options;
        _llmProvider = llmProvider;
    }

    /// <summary>Attach cognition to the pet's <c>Main</c> control.</summary>
    public void AttachToPet(VPet_Simulator.Core.Main? main)
    {
        if (main is null)
        {
            _logger.LogWarning("AttachToPet called with null Main; skipping.");
            return;
        }
        _logger.LogInformation("Attached to pet. Cognition enabled={Enabled}, LLM={Provider}",
            _options.Value.Enabled, _llmProvider.ProviderId);
    }

    public void DetachFromPet()
    {
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

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Options (Phase 1: defaults; future: bind from appsettings.json + LPS)
        var options = new CognitionOptions();
        services.AddSingleton(Options.Create(options));

        // Event bus
        services.AddSingleton<IEventBus, EventBus.EventBus>();

        // LLM provider selection by config
        services.AddSingleton<ILLMProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var httpClient = new HttpClient();

            return opts.Llm.Provider.ToLowerInvariant() switch
            {
                "ollama" => new OllamaLlmProvider(
                    opts.Llm.Endpoint, opts.Llm.Model, opts.Llm.Timeout,
                    httpClient, loggerFactory.CreateLogger<OllamaLlmProvider>()),

                "openai-compatible" or "openai" => new OpenAICompatibleLlmProvider(
                    "openai-compatible", opts.Llm.Endpoint, opts.Llm.Model,
                    opts.Llm.ApiKey, opts.Llm.Timeout,
                    httpClient, loggerFactory.CreateLogger<OpenAICompatibleLlmProvider>()),

                _ => new OllamaLlmProvider(
                    "http://localhost:11434", "qwen2.5:3b", TimeSpan.FromMinutes(1),
                    httpClient, loggerFactory.CreateLogger<OllamaLlmProvider>()),
            };
        });

        // LLM services
        services.AddSingleton<PromptAssembler>();
        services.AddSingleton<StreamAdapter>();
        services.AddSingleton<ConversationService>();

        // Memory store
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

        // Voice providers (STT)
        services.AddSingleton<ISttProvider>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var stt = opts.Voice.Stt;
            var logger = sp.GetRequiredService<ILogger<WhisperSttProvider>>();
            return new WhisperSttProvider(stt.ExecutablePath, stt.ModelPath, stt.Language, stt.Threads, logger);
        });

        // Voice providers (TTS) — select by config
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

        // Cognition host
        services.AddSingleton<CognitionHost>();

        // Personality + Emotion
        services.AddSingleton<IPersonalityEngine>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<DefaultPersonalityEngine>>();
            return new DefaultPersonalityEngine(
                new PersonalitySeed { Name = opts.Personality.Seed.Name, Description = opts.Personality.Seed.Description },
                opts.Personality.EvolutionRate, opts.Personality.CoreTraitMaxDrift, logger);
        });
        services.AddSingleton<IEmotionEngine>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<DefaultEmotionEngine>>();
            return new DefaultEmotionEngine(
                opts.Emotion.BlendFactor, opts.Emotion.DecayRatePerSec,
                opts.Emotion.AntagonisticSuppression, logger);
        });

        // Agent + Tools + Permission
        services.AddSingleton<IPermissionService, DefaultPermissionService>();
        services.AddSingleton<IToolProvider>(sp =>
        {
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            return new VPet.Cognition.AgentRuntime.Tools.BrowserTool(loggerFactory.CreateLogger<VPet.Cognition.AgentRuntime.Tools.BrowserTool>());
        });
        services.AddSingleton<IAgentRuntime>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<CognitionOptions>>().Value;
            var tool = sp.GetRequiredService<IToolProvider>();
            var perm = sp.GetRequiredService<IPermissionService>();
            var logger = sp.GetRequiredService<ILogger<DefaultAgentRuntime>>();
            return new DefaultAgentRuntime(
                tool, perm, opts.Agent.MaxStepsPerPlan, opts.Agent.MaxRetries,
                opts.Agent.ConfidenceThreshold, logger);
        });

        // WPF module (if available — only on Windows)
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<VPet.Cognition.Wpf.CognitionWpfModule>();
            services.AddSingleton<VPet.Cognition.Wpf.ViewModels.ConversationVM>();
            services.AddSingleton<VPet.Cognition.Wpf.ViewModels.CognitionSettingsVM>();
        }

        return services.BuildServiceProvider();
    }
}