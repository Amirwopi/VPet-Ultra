using FluentAssertions;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Voice.Stt;
using VPet.Cognition.Voice.Tts;

namespace VPet.Cognition.Tests;

/// <summary>
/// Tests for voice providers. These test availability checks and configuration
/// without requiring actual whisper/piper/kokoro executables.
/// </summary>
public class VoiceProviderTests
{
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    [Fact]
    public void WhisperSttProvider_Constructor_ShouldSetProperties()
    {
        var provider = new WhisperSttProvider(
            "/usr/bin/whisper-cli", "/models/ggml-base.en.bin",
            "en", 4, _loggerFactory.CreateLogger<WhisperSttProvider>());

        provider.ProviderId.Should().Be("whisper");
    }

    [Fact]
    public async Task WhisperSttProvider_IsAvailableAsync_NoExe_ShouldReturnFalse()
    {
        var provider = new WhisperSttProvider(
            "/nonexistent/whisper-cli", "/nonexistent/model.bin",
            "en", 4, _loggerFactory.CreateLogger<WhisperSttProvider>());

        var available = await provider.IsAvailableAsync();
        available.Should().BeFalse();
    }

    [Fact]
    public async Task WhisperSttProvider_StartStreamingAsync_NoExe_ShouldThrow()
    {
        var provider = new WhisperSttProvider(
            "/nonexistent/whisper-cli", "/nonexistent/model.bin",
            "en", 4, _loggerFactory.CreateLogger<WhisperSttProvider>());

        var act = () => provider.StartStreamingAsync();

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public void PiperTtsProvider_Constructor_ShouldSetProperties()
    {
        var provider = new PiperTtsProvider(
            "/usr/bin/piper", "/models/en_US-lessac-medium.onnx",
            null, 1.0, 0.667, 0.8, TimeSpan.FromSeconds(15),
            _loggerFactory.CreateLogger<PiperTtsProvider>());

        provider.ProviderId.Should().Be("piper");
    }

    [Fact]
    public async Task PiperTtsProvider_IsAvailableAsync_NoExe_ShouldReturnFalse()
    {
        var provider = new PiperTtsProvider(
            "/nonexistent/piper", "/nonexistent/model.onnx",
            null, 1.0, 0.667, 0.8, TimeSpan.FromSeconds(15),
            _loggerFactory.CreateLogger<PiperTtsProvider>());

        var available = await provider.IsAvailableAsync();
        available.Should().BeFalse();
    }

    [Fact]
    public async Task PiperTtsProvider_SynthesizeAsync_NoExe_ShouldReturnEmpty()
    {
        var provider = new PiperTtsProvider(
            "/nonexistent/piper", "/nonexistent/model.onnx",
            null, 1.0, 0.667, 0.8, TimeSpan.FromSeconds(15),
            _loggerFactory.CreateLogger<PiperTtsProvider>());

        var chunks = new List<AudioChunk>();
        await foreach (var chunk in provider.SynthesizeAsync("hello", new VoiceProsody(1, 1, 1)))
            chunks.Add(chunk);

        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task PiperTtsProvider_GetVisemesAsync_ShouldReturnEmpty()
    {
        var provider = new PiperTtsProvider(
            "/nonexistent/piper", "/nonexistent/model.onnx",
            null, 1.0, 0.667, 0.8, TimeSpan.FromSeconds(15),
            _loggerFactory.CreateLogger<PiperTtsProvider>());

        var visemes = await provider.GetVisemesAsync("hello");
        visemes.Should().BeEmpty();
    }

    [Fact]
    public void KokoroTtsProvider_Constructor_ShouldSetProperties()
    {
        var provider = new KokoroTtsProvider(
            "/usr/bin/kokoro", "/models/kokoro.onnx",
            "af_heart", 1.0f, TimeSpan.FromSeconds(15),
            _loggerFactory.CreateLogger<KokoroTtsProvider>());

        provider.ProviderId.Should().Be("kokoro");
    }

    [Fact]
    public async Task KokoroTtsProvider_IsAvailableAsync_NoExe_ShouldReturnFalse()
    {
        var provider = new KokoroTtsProvider(
            "/nonexistent/kokoro", "/nonexistent/model.onnx",
            "af_heart", 1.0f, TimeSpan.FromSeconds(15),
            _loggerFactory.CreateLogger<KokoroTtsProvider>());

        var available = await provider.IsAvailableAsync();
        available.Should().BeFalse();
    }

    [Fact]
    public async Task KokoroTtsProvider_SynthesizeAsync_NoExe_ShouldReturnEmpty()
    {
        var provider = new KokoroTtsProvider(
            "/nonexistent/kokoro", "/nonexistent/model.onnx",
            "af_heart", 1.0f, TimeSpan.FromSeconds(15),
            _loggerFactory.CreateLogger<KokoroTtsProvider>());

        var chunks = new List<AudioChunk>();
        await foreach (var chunk in provider.SynthesizeAsync("hello", new VoiceProsody(1, 1, 1)))
            chunks.Add(chunk);

        chunks.Should().BeEmpty();
    }

    [Fact]
    public async Task KokoroTtsProvider_GetVisemesAsync_ShouldReturnEmpty()
    {
        var provider = new KokoroTtsProvider(
            "/nonexistent/kokoro", "/nonexistent/model.onnx",
            "af_heart", 1.0f, TimeSpan.FromSeconds(15),
            _loggerFactory.CreateLogger<KokoroTtsProvider>());

        var visemes = await provider.GetVisemesAsync("hello");
        visemes.Should().BeEmpty();
    }
}