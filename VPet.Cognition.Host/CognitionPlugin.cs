using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Host.EventBus;
using VPet.Cognition.Host.Options;
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
        // Build the DI container and wire core services.
        _services = CognitionHostBuilder.Build(MW);
        _logger = _services.GetRequiredService<ILogger<CognitionPlugin>>();
        _host = _services.GetRequiredService<CognitionHost>();
        _logger.LogInformation("CognitionPlugin loaded (Phase 0 no-op). Provider={Provider}", _services.GetRequiredService<IOptions<CognitionOptions>>().Value.Llm.Provider);
    }

    public override void GameLoaded()
    {
        _logger?.LogInformation("CognitionPlugin GameLoaded — attaching to pet (Phase 0 no-op).");
        _host?.AttachToPet(MW.Main);
    }

    public override void Save()
    {
        _logger?.LogDebug("CognitionPlugin Save (Phase 0 no-op).");
    }

    public override void EndGame()
    {
        _logger?.LogInformation("CognitionPlugin EndGame — disposing.");
        _host?.DetachFromPet();
        _services?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public override void Setting()
    {
        _logger?.LogInformation("CognitionPlugin Setting requested (Phase 0 no-op — settings UI in Phase 1).");
    }

    public override void LoadDIY()
    {
        // Phase 1 will add a "Chat" toolbar button here via MW.Main.ToolBar.AddMenuButton.
        _logger?.LogDebug("CognitionPlugin LoadDIY (Phase 0 no-op).");
    }
}

/// <summary>
/// The orchestrator that attaches cognition to the pet. Phase 0 is a no-op that proves the seam.
/// </summary>
public sealed class CognitionHost
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<CognitionHost> _logger;
    private readonly IOptions<CognitionOptions> _options;

    public CognitionHost(IEventBus eventBus, ILogger<CognitionHost> logger, IOptions<CognitionOptions> options)
    {
        _eventBus = eventBus;
        _logger = logger;
        _options = options;
    }

    /// <summary>Attach cognition to the pet's <c>Main</c> control.</summary>
    public void AttachToPet(VPet_Simulator.Core.Main? main)
    {
        if (main is null)
        {
            _logger.LogWarning("AttachToPet called with null Main; skipping (pet not loaded yet).");
            return;
        }
        _logger.LogInformation("Attached to pet. Cognition enabled={Enabled}", _options.Value.Enabled);
        // Phase 0: no subscriptions. Phase 1+ subscribes to TimeHandle, GraphDisplayHandler, SayProcess.
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

        // Logging (Serilog)
        services.AddLogging(builder =>
        {
            builder.AddSerilog(dispose: true);
        });

        // Options (Phase 0: defaults only; Phase 1 binds from appsettings.json + LPS)
        services.AddOptions<CognitionOptions>();

        // Event bus
        services.AddSingleton<IEventBus, EventBus.EventBus>();

        // Cognition host
        services.AddSingleton<CognitionHost>();

        return services.BuildServiceProvider();
    }
}