using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.LLM;
using VPet.Cognition.Voice.Audio;
using VPet.Cognition.Voice.Stt;
using VPet.Cognition.Voice.Tts;

namespace VPet.Cognition.Wpf.ViewModels;

/// <summary>
/// View-model for the conversation window. Manages message history, streaming, cancel,
/// microphone capture (STT), and TTS playback.
/// </summary>
public sealed partial class ConversationVM : ObservableObject, IDisposable
{
    private readonly ConversationService _conversationService;
    private readonly ICognitionFacade _cognitionFacade;
    private readonly ISttProvider _sttProvider;
    private readonly ITtsProvider _ttsProvider;
    private readonly ILogger<ConversationVM> _logger;
    private CancellationTokenSource? _cts;

    private AudioCapture? _audioCapture;
    private AudioPlayer? _audioPlayer;
    private string _sttTempDir;

    public ObservableCollection<ChatMessageVm> Messages { get; } = new();

    [ObservableProperty]
    private string _inputText = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCommand))]
    private bool _isStreaming;

    [ObservableProperty]
    private bool _isListeningMic;

    [ObservableProperty]
    private string _systemPrompt = "You are a helpful desktop companion. Be friendly and concise. You understand and respond in Persian (Farsi) and English.";

    private readonly List<ChatMessage> _history = new();

    public ConversationVM(
        ConversationService conversationService,
        ICognitionFacade cognitionFacade,
        ISttProvider sttProvider,
        ITtsProvider ttsProvider,
        ILogger<ConversationVM> logger)
    {
        _conversationService = conversationService;
        _cognitionFacade = cognitionFacade;
        _sttProvider = sttProvider;
        _ttsProvider = ttsProvider;
        _logger = logger;

        _sttTempDir = Path.Combine(Path.GetTempPath(), "vpet_stt");
        Directory.CreateDirectory(_sttTempDir);

        var audioLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AudioPlayer>.Instance;
        var captureLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<AudioCapture>.Instance;
        _audioPlayer = new AudioPlayer(audioLogger);
        _audioCapture = new AudioCapture(_sttTempDir, captureLogger);
        _audioCapture.ChunkReady += OnAudioChunkReady;
    }

    /// <summary>
    /// Toggle microphone listening. When enabled, captures audio and transcribes via Whisper.
    /// </summary>
    [RelayCommand]
    private void ToggleMic()
    {
        if (IsListeningMic)
        {
            _audioCapture?.Stop();
            IsListeningMic = false;
            _logger.LogInformation("Microphone stopped.");
        }
        else
        {
            // Clear old chunks
            try { Array.ForEach(Directory.GetFiles(_sttTempDir, "*.wav"), File.Delete); } catch { }
            _audioCapture?.Start();
            IsListeningMic = true;
            _logger.LogInformation("Microphone started. Speak now...");
        }
    }

    private async void OnAudioChunkReady(string wavFile)
    {
        try
        {
            // Save a copy to voice-history so the user can verify recording
            var historyDir = Path.Combine(AppContext.BaseDirectory, "voice-history");
            Directory.CreateDirectory(historyDir);
            var historyFile = Path.Combine(historyDir, Path.GetFileName(wavFile));
            try { File.Copy(wavFile, historyFile, true); } catch { /* ignore */ }

            _logger.LogInformation("Transcribing audio chunk: {File} ({Size} bytes), STT provider: {Type}",
                Path.GetFileName(wavFile), new FileInfo(wavFile).Length, _sttProvider.GetType().Name);
            var transcript = await _sttProvider.TranscribeFileAsync(wavFile);

            _logger.LogInformation("Transcribed result: \"{Text}\" (confidence={Conf})", transcript.Text, transcript.Confidence);

            if (!string.IsNullOrWhiteSpace(transcript.Text))
            {
                // Put transcribed text into input box and auto-send
                InputText = transcript.Text.Trim();
                if (CanSend && SendCommand.CanExecute(null))
                {
                    await SendAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STT transcription failed: {Type}", _sttProvider.GetType().Name);
        }
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
            // 1. Retrieve memory context and apply personality/emotion via CognitionFacade
            string memoryContext = "";
            try
            {
                memoryContext = await _cognitionFacade.ProcessConversationTurnAsync(userInput, _cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CognitionFacade processing failed; continuing without memory.");
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
                    await _cognitionFacade.StoreConversationTurnAsync(userInput, result.Value.Text, _cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to store conversation to memory.");
                }

                // 4. Speak the response via TTS
                await SpeakAsync(result.Value.Text, _cts.Token);
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
        catch (Exception ex)
        {
            companionMsg.AppendToken($"[Error: {ex.Message}]");
            _logger.LogError(ex, "Send failed");
        }
        finally
        {
            IsStreaming = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    /// <summary>
    /// Synthesize and play text via TTS (Piper).
    /// </summary>
    private async Task SpeakAsync(string text, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            _logger.LogInformation("Speaking via TTS: {Text}...", text.Substring(0, Math.Min(50, text.Length)));

            // Synthesize to a temp WAV file
            var tempWav = Path.Combine(Path.GetTempPath(), $"vpet_tts_{Guid.NewGuid():N}.wav");
            await using (var fs = File.Create(tempWav))
            {
                await foreach (var chunk in _ttsProvider.SynthesizeAsync(text, new VoiceProsody(1.0, 1.0, 1.0), cancellationToken))
                {
                    await fs.WriteAsync(chunk.PcmData, cancellationToken);
                }
            }

            // Play the audio
            if (File.Exists(tempWav))
            {
                await _audioPlayer!.PlayAsync(tempWav, cancellationToken);
                try { File.Delete(tempWav); } catch { }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TTS playback failed.");
        }
    }

    [RelayCommand(CanExecute = nameof(IsStreaming))]
    private void Cancel()
    {
        _cts?.Cancel();
    }

    private bool CanSend => !IsStreaming && !string.IsNullOrWhiteSpace(InputText);

    public void Dispose()
    {
        _audioCapture?.Dispose();
        _audioPlayer?.Dispose();
    }
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