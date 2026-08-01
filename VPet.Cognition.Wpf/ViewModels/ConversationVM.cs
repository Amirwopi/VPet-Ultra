using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.LLM;

namespace VPet.Cognition.Wpf.ViewModels;

/// <summary>
/// View-model for the conversation window. Manages message history, streaming, and cancel.
/// </summary>
public sealed partial class ConversationVM : ObservableObject
{
    private readonly ConversationService _conversationService;
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

    [ObservableProperty]
    private string _memoryContext = string.Empty;

    private readonly List<ChatMessage> _history = new();

    public ConversationVM(ConversationService conversationService, ILogger<ConversationVM> logger)
    {
        _conversationService = conversationService;
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
            var result = await _conversationService.StreamConversationAsync(
                SystemPrompt,
                MemoryContext,
                _history,
                userInput,
                Array.Empty<ToolSchema>(),
                token => companionMsg.AppendToken(token),
                cancellationToken: _cts.Token);

            if (result.IsSuccess)
            {
                _history.Add(new ChatMessage("user", userInput));
                _history.Add(new ChatMessage("assistant", result.Value.Text));
                _logger.LogInformation("Conversation turn: {Len} chars", result.Value.Text.Length);
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