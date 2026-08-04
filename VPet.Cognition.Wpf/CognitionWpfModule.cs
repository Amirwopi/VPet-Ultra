using System.Windows;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.MemoryStore;
using VPet.Cognition.Wpf.Views;

namespace VPet.Cognition.Wpf;

/// <summary>
/// Module for creating and showing WPF windows. Called by CognitionPlugin.
/// </summary>
public sealed class CognitionWpfModule
{
    private readonly IServiceProvider _services;

    public CognitionWpfModule(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Show the conversation window (creates if not already open).
    /// </summary>
    public void ShowConversationWindow()
    {
        var vm = _services.GetService(typeof(ViewModels.ConversationVM)) as ViewModels.ConversationVM
                 ?? throw new InvalidOperationException("ConversationVM not registered");
        var window = new ConversationWindow(vm);
        window.Show();
    }

    /// <summary>
    /// Show the settings window.
    /// </summary>
    public void ShowSettingsWindow()
    {
        var vm = _services.GetService(typeof(ViewModels.CognitionSettingsVM)) as ViewModels.CognitionSettingsVM
                 ?? throw new InvalidOperationException("CognitionSettingsVM not registered");
        var window = new CognitionSettingsWindow(vm);
        window.Show();
    }

    /// <summary>
    /// Show the emotion dashboard window.
    /// </summary>
    public void ShowEmotionDashboard()
    {
        var engine = _services.GetService(typeof(IEmotionEngine)) as IEmotionEngine;
        if (engine is null) return;
        var window = new EmotionDashboardWindow(engine);
        window.Show();
    }

    /// <summary>
    /// Show the personality window.
    /// </summary>
    public void ShowPersonality()
    {
        var engine = _services.GetService(typeof(IPersonalityEngine)) as IPersonalityEngine;
        if (engine is null) return;
        var window = new PersonalityWindow(engine);
        window.Show();
    }

    /// <summary>
    /// Show the memory inspector window.
    /// </summary>
    public void ShowMemoryInspector()
    {
        var store = _services.GetService(typeof(IMemoryStore)) as IMemoryStore;
        var memService = _services.GetService(typeof(MemoryService)) as MemoryService;
        if (store is null || memService is null) return;
        var window = new MemoryInspectorWindow(store, memService);
        window.Show();
    }

    /// <summary>
    /// Prompt the user for a tool permission. Returns the user's choice.
    /// </summary>
    public PermissionResult PromptPermission(ToolSchema schema, string argsPreview)
    {
        var window = new PermissionPromptWindow(schema, argsPreview);
        window.ShowDialog();
        return window.Result;
    }
}