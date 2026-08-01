using System.Windows;
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
}