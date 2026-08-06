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
using VPet.Cognition.Wpf.Helpers;

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
    private string _systemPrompt = @"تو یک حیوان خانگی مجازی هستی روی دسکتاپ کاربر. شخصیت داری که با تعامل رشد می‌کنه و خاطرات گذشته رو به یاد میاری.

قوانین:
- فقط به فارسی جواب بده. هرگز عربی یا انگلیسی نده مگر اینکه کاربر انگلیسی حرف بزنه.
- طبیعی و صمیمی باش، رباتیک و تکراری نباش.
- حتماً از خاطرات و اطلاعات گذشته استفاده کن. اگه کاربر اسمش رو گفته، از اسمش استفاده کن.
- کوتاه جواب بده (۱-۳ جمله) مگر اینکه کاربر جزئیات بخواد.
- از پرسیدن «چطور می‌تونم کمک کنم» خودداری کن — تو یه رفیق هستی، نه یه دستیار.";

    private readonly List<ChatMessage> _history = new();

    public ConversationVM(
        ConversationService conversationService,
        ICognitionFacade cognitionFacade,
        ISttProvider sttProvider,
        ITtsProvider ttsProvider,
        ILoggerFactory loggerFactory,
        ILogger<ConversationVM> logger)
    {
        _conversationService = conversationService;
        _cognitionFacade = cognitionFacade;
        _sttProvider = sttProvider;
        _ttsProvider = ttsProvider;
        _logger = logger;

        _sttTempDir = Path.Combine(Path.GetTempPath(), "vpet_stt");
        Directory.CreateDirectory(_sttTempDir);

        var audioLogger = loggerFactory.CreateLogger<AudioPlayer>();
        var captureLogger = loggerFactory.CreateLogger<AudioCapture>();
        _audioPlayer = new AudioPlayer(audioLogger);
        _audioCapture = new AudioCapture(_sttTempDir, captureLogger);
        _audioCapture.ChunkReady += OnAudioChunkReady;

        // Register global hotkey Alt+Q for push-to-talk
        RegisterPushToTalkHotkey();
    }

    /// <summary>
    /// Register Alt+Q as a global hotkey for push-to-talk.
    /// Press = start recording, Release = stop and send.
    /// </summary>
    private void RegisterPushToTalkHotkey()
    {
        try
        {
            // Use a low-level keyboard hook to detect Alt+Q press/release
            _keyboardHook = new LowLevelKeyboardHook();
            _keyboardHook.AltQPressed += () =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (!IsListeningMic && !IsStreaming)
                    {
                        StartRecording();
                    }
                });
            };
            _keyboardHook.AltQReleased += () =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (IsListeningMic)
                    {
                        StopRecordingAndSend();
                    }
                });
            };
            _keyboardHook.Subscribe();
            _logger.LogInformation("Push-to-talk hotkey Alt+Q registered.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register Alt+Q hotkey.");
        }
    }

    private LowLevelKeyboardHook? _keyboardHook;

    /// <summary>
    /// Start recording audio for push-to-talk. Plays a beep to signal recording started.
    /// </summary>
    private void StartRecording()
    {
        if (IsListeningMic) return; // Already recording, ignore
        try
        {
            Array.ForEach(Directory.GetFiles(_sttTempDir, "*.wav"), File.Delete);
            _audioCapture?.Start();
            IsListeningMic = true;
            _logger.LogInformation("Push-to-talk: recording started (Alt+Q). Speak now...");
            // Play a short beep to signal the user that recording started
            try { _ = _audioPlayer?.PlayBeepAsync(660, 150); } catch { /* ignore */ }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start recording.");
        }
    }

    /// <summary>
    /// Stop recording and send the transcribed text. Plays a beep to signal recording stopped.
    /// </summary>
    private void StopRecordingAndSend()
    {
        if (!IsListeningMic) return; // Not recording, ignore
        _audioCapture?.Stop();
        IsListeningMic = false;
        _logger.LogInformation("Push-to-talk: recording stopped. Transcribing...");
        // Play a short beep to signal the user that recording stopped
        try { _ = _audioPlayer?.PlayBeepAsync(440, 100); } catch { /* ignore */ }
        // OnAudioChunkReady will handle transcription and sending
    }

    /// <summary>
    /// Toggle microphone listening manually (mic button).
    /// </summary>
    [RelayCommand]
    private void ToggleMic()
    {
        if (IsListeningMic)
        {
            StopRecordingAndSend();
        }
        else
        {
            StartRecording();
        }
    }

    private readonly SemaphoreSlim _sttLock = new(1, 1);

    private async void OnAudioChunkReady(string wavFile)
    {
        // Wait for any previous turn to finish — but with a 120s timeout so we don't
        // block forever if Ollama hangs. If the lock can't be acquired, skip this chunk.
        if (!await _sttLock.WaitAsync(TimeSpan.FromSeconds(120)))
        {
            _logger.LogWarning("STT lock timeout, skipping chunk: {File}", Path.GetFileName(wavFile));
            try { File.Delete(wavFile); } catch { /* ignore */ }
            return;
        }

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
            var text = transcript.Text?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogDebug("Empty transcription, ignoring.");
                return;
            }

            // Push-to-talk: just send the transcribed text directly
            _logger.LogInformation("Sending transcribed text: '{Text}'", text);
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                InputText = text;
            });
            await SendAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "STT transcription failed: {Type}", _sttProvider.GetType().Name);
        }
        finally
        {
            _sttLock.Release();
            try { File.Delete(wavFile); } catch { /* ignore */ }
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText))
            return;

        // If already streaming, ignore (shouldn't happen with _sttLock, but safety check)
        if (IsStreaming)
            return;

        var userInput = InputText;

        // All UI collection changes must happen on the Dispatcher thread.
        // SendAsync may be called from the STT background thread (OnAudioChunkReady).
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            InputText = string.Empty;
            Messages.Add(new ChatMessageVm("user", userInput));
            IsStreaming = true;
        });

        _cts = new CancellationTokenSource(TimeSpan.FromSeconds(120)); // 120s overall timeout

        // Add a placeholder for the companion response
        ChatMessageVm companionMsg = null!;
        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            companionMsg = new ChatMessageVm("assistant", string.Empty);
            Messages.Add(companionMsg);
        });

        try
        {
            // 1. Process conversation turn: get personality + emotion + memory context
            //    This returns the full system prompt with identity, traits, emotions, and memories.
            string fullSystemPrompt = SystemPrompt; // fallback
            try
            {
                var cognitionContext = await _cognitionFacade.ProcessConversationTurnAsync(userInput, _cts.Token);
                if (!string.IsNullOrWhiteSpace(cognitionContext))
                    fullSystemPrompt = cognitionContext;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CognitionFacade processing failed; using default system prompt.");
            }

            // 2. Stream the conversation with the full personality+memory system prompt
            var result = await _conversationService.StreamConversationAsync(
                fullSystemPrompt,
                "", // memory context is already included in fullSystemPrompt
                _history,
                userInput,
                Array.Empty<ToolSchema>(),
                token => System.Windows.Application.Current.Dispatcher.Invoke(() => companionMsg.AppendToken(token)),
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
                _logger.LogInformation("TTS input text ({Len} chars): [{Text}]", result.Value.Text.Length, result.Value.Text);
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
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => IsStreaming = false);
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

            // Synthesize to a temp WAV file (PiperTtsProvider returns full WAV with header)
            var tempWav = Path.Combine(Path.GetTempPath(), $"vpet_tts_{Guid.NewGuid():N}.wav");
            await using (var fs = File.Create(tempWav))
            {
                await foreach (var chunk in _ttsProvider.SynthesizeAsync(text, new VoiceProsody(1.0, 1.0, 1.0), cancellationToken))
                {
                    await fs.WriteAsync(chunk.PcmData, cancellationToken);
                }
            }

            var fileSize = File.Exists(tempWav) ? new FileInfo(tempWav).Length : 0;
            _logger.LogInformation("TTS synthesized to {Path}, size={Size} bytes", tempWav, fileSize);

            if (fileSize > 44)
            {
                _logger.LogInformation("Playing TTS audio...");
                await _audioPlayer!.PlayAsync(tempWav, cancellationToken);
                _logger.LogInformation("TTS playback finished.");
                try { File.Delete(tempWav); } catch { }
            }
            else
            {
                _logger.LogWarning("TTS output file missing or too small: {Path} ({Size} bytes)", tempWav, fileSize);
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
        _keyboardHook?.Dispose();
        _audioCapture?.Dispose();
        _audioPlayer?.Dispose();
        _sttLock?.Dispose();
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