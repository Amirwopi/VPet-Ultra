using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;

namespace VPet.Cognition.Wpf.ViewModels;

/// <summary>
/// View-model for the cognition settings window (LLM tab for Phase 1).
/// </summary>
public sealed partial class CognitionSettingsVM : ObservableObject
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<CognitionSettingsVM> _logger;

    [ObservableProperty] private string _provider = "ollama";
    [ObservableProperty] private string _endpoint = "http://localhost:11434";
    [ObservableProperty] private string _model = "qwen2.5:3b";
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _testResult = string.Empty;
    [ObservableProperty] private bool _isTesting;

    public CognitionSettingsVM(
        ILLMProvider llmProvider,
        ILogger<CognitionSettingsVM> logger)
    {
        _llmProvider = llmProvider;
        _logger = logger;
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestResult = "Testing...";
        try
        {
            var available = await _llmProvider.IsAvailableAsync();
            TestResult = available ? "✓ Connected" : "✗ Not reachable";
        }
        catch (Exception ex)
        {
            TestResult = $"✗ {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }
}