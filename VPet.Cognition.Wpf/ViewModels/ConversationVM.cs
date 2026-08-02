using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Host;
using VPet.Cognition.LLM;

namespace VPet.Cognition.Wpf.ViewModels;

/// <summary>
/// View-model for the conversation window. Manages message history, streaming, and cancel.
/// Integrates memory retrieval, personality, and emotion via CognitionHost.
/// </summary>
public sealed partial class ConversationVM : ObservableObject
{
    private readonly ConversationService _conversationService;
    private readonly CognitionHost _cognitionHost;
    private readonly ILogger<ConversationVM> _logger;
    private CancellationTokenSource? _cts;

    public ObservableCollection<ChatMessageVm> Messages { get; } = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private bool _isStreaming;

    [ObservableProperty]
    private string _systemPrompt = "You are a helpful desktop companion. Be friendly and concise.";

    private readonly List<ChatMessage> _history = new();

    public ConversationVM(
        ConversationService conversationService,
        CognitionHost cognitionHost,
        ILogger<ConversationVM> logger)
    {
        _conversationService = conversationService;
        _cognitionHost = cognitionHost;
        _logger = logger;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        var userInput = InputText;
        InputText = string.Empty;

        // Add user message to UI
        Messages.Add(new ChatMessageVm("user", userInput));
        IsStreaming = true;
        _cts = new CancellationTokenSource();

        // Add a placeholder for the companion response
        var companionMsg = new ChatMessageVm("assistant", string.Empty);
        Messages.Add(companionMsg);

        try
        {
            // 1. Retrieve memory context and apply personality/emotion via CognitionHost
            string memoryContext = "";
            try
            {
                memoryContext = await _cognitionHost.ProcessConversationTurnAsync(userInput, _cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CognitionHost processing failed; continuing without memory.");
            }

            // 2. Stream the conversation with memory context
            var result = await _conversationService.StreamConversationAsync(
                SystemPrompt,
                memoryContext,
                _history,
                userInput,
                Array.Empty<ToolSchema>(),
                token => companionMsg.AppendToken(token),
                cancellationToken: _cts.Token);

            if (result.IsSuccess && result.Value is not null)
            {
                _history.Add(new ChatMessage("user", userInput));
                _history.Add(new ChatMessage("assistant", result.Value.Text));
                _logger.LogInformation("Conversation turn: {Len} chars", result.Value.Text.Length);

                // 3. Store the conversation turn to memory
                try
                {
                    await _cognitionHost.StoreConversationTurnAsync(userInput, result.Value.Text, _cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to store conversation to memory.");
                }
            }
            else
            {
                companionMsg.AppendToken($"[Error: {result.Error!.Message}]");
                _logger.LogWarning("Conversation failed: {Error}", result.Error);
            }
        }
        catch (OperationCanceledException)
        {
            companionMsg.AppendToken(" [cancelled]");
            _logger.LogInformation("Conversation cancelled");
        }
        finally
        {
            IsStreaming = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(IsStreaming))]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    private bool CanSend => !IsStreaming && !string.IsNullOrWhiteSpace(InputText);
}

/// <summary>
/// A single chat message in the conversation UI.
/// </summary>
public sealed partial class ChatMessageVm : ObservableObject
{
    public string Role { get; }
    public bool IsUser => Role == "user";

    [ObservableProperty]
    private string _content;

    public ChatMessageVm(string role, string content)
    {
        Role = role;
        _content = content;
    }

    public void AppendToken(string token) => Content += token;
}