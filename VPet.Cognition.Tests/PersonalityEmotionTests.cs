using FluentAssertions;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.PersonalityEngine;

namespace VPet.Cognition.Tests;

/// <summary>
/// Tests for the personality and emotion engines.
/// </summary>
public class PersonalityEmotionTests
{
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    [Fact]
    public async Task PersonalityEngine_GetAsync_ShouldReturnSeededPersonality()
    {
        var engine = new DefaultPersonalityEngine(
            new PersonalitySeed { Name = "TestBot", Description = "A test companion." },
            0.02, 0.1, _loggerFactory.CreateLogger<DefaultPersonalityEngine>());

        var personality = await engine.GetAsync();

        personality.Identity.Name.Should().Be("TestBot");
        personality.Identity.Description.Should().Be("A test companion.");
        personality.CoreTraits.Openness.Should().BeGreaterThan(0);
        personality.Relationship.Trust.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PersonalityEngine_EvolveAsync_ShouldIncreaseTrust()
    {
        var engine = new DefaultPersonalityEngine(
            new PersonalitySeed(), 0.02, 0.1, _loggerFactory.CreateLogger<DefaultPersonalityEngine>());

        var before = (await engine.GetAsync()).Relationship.Trust;

        await engine.EvolveAsync(new PersonalityDelta { TrustDelta = 10, FamiliarityDelta = 5 });

        var after = (await engine.GetAsync()).Relationship.Trust;
        after.Should().BeGreaterThan(before);
    }

    [Fact]
    public async Task PersonalityEngine_RenderSystemPromptAsync_ShouldIncludeIdentity()
    {
        var engine = new DefaultPersonalityEngine(
            new PersonalitySeed { Name = "Luna", Description = "A moon companion." },
            0.02, 0.1, _loggerFactory.CreateLogger<DefaultPersonalityEngine>());

        var prompt = await engine.RenderSystemPromptAsync();

        prompt.Should().Contain("Luna");
        prompt.Should().Contain("A moon companion.");
        prompt.Should().Contain("Trust:");
        prompt.Should().Contain("Openness:");
    }

    [Fact]
    public async Task PersonalityEngine_EvolveAsync_ShouldApplyPreferenceDeltas()
    {
        var engine = new DefaultPersonalityEngine(
            new PersonalitySeed(), 0.02, 0.1, _loggerFactory.CreateLogger<DefaultPersonalityEngine>());

        await engine.EvolveAsync(new PersonalityDelta
        {
            PreferenceDeltas = new() { ["coffee"] = 0.5 }
        });

        var personality = await engine.GetAsync();
        personality.Preferences.Should().ContainKey("coffee");
    }

    [Fact]
    public async Task EmotionEngine_GetAsync_ShouldReturnNeutralInitially()
    {
        var engine = new DefaultEmotionEngine(0.3, 0.01, 0.5, _loggerFactory.CreateLogger<DefaultEmotionEngine>());

        var state = await engine.GetAsync();

        state.Dominant.Should().Be(EmotionType.Calmness);
    }

    [Fact]
    public async Task EmotionEngine_ApplyAsync_ShouldChangeDominantEmotion()
    {
        var engine = new DefaultEmotionEngine(0.8, 0.01, 0.5, _loggerFactory.CreateLogger<DefaultEmotionEngine>());

        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Joy, Intensity = 0.9 });

        var state = await engine.GetAsync();
        state.Dominant.Should().Be(EmotionType.Joy);
        state.Vector.Joy.Should().BeGreaterThan(0.4);
    }

    [Fact]
    public async Task EmotionEngine_ApplyAsync_JoyShouldSuppressSadness()
    {
        var engine = new DefaultEmotionEngine(0.8, 0.01, 0.5, _loggerFactory.CreateLogger<DefaultEmotionEngine>());

        // First make the companion sad
        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Sadness, Intensity = 0.8 });
        var sadState = await engine.GetAsync();
        sadState.Vector.Sadness.Should().BeGreaterThan(0.3);

        // Now apply joy — should suppress sadness
        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Joy, Intensity = 0.9 });
        var joyfulState = await engine.GetAsync();
        joyfulState.Vector.Joy.Should().BeGreaterThan(joyfulState.Vector.Sadness);
    }

    [Fact]
    public async Task EmotionEngine_DecayAsync_ShouldReduceIntensity()
    {
        var engine = new DefaultEmotionEngine(0.5, 0.01, 0.5, _loggerFactory.CreateLogger<DefaultEmotionEngine>());

        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Joy, Intensity = 0.9 });
        var before = (await engine.GetAsync()).Vector.Joy;

        // Wait a bit for decay to have effect
        await Task.Delay(100);
        await engine.DecayAsync();

        var after = (await engine.GetAsync()).Vector.Joy;
        after.Should().BeLessThanOrEqualTo(before);
    }

    [Fact]
    public async Task EmotionEngine_MapToAnimationAsync_ShouldReturnGraphHint()
    {
        var engine = new DefaultEmotionEngine(0.5, 0.01, 0.5, _loggerFactory.CreateLogger<DefaultEmotionEngine>());

        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Joy, Intensity = 0.8 });
        var hint = await engine.MapToAnimationAsync();

        hint.PreferredGraph.Should().NotBeNull();
        hint.Intensity.Should().BeGreaterThan(0.3);
    }

    [Fact]
    public async Task EmotionEngine_MapToVoiceAsync_ShouldReturnProsody()
    {
        var engine = new DefaultEmotionEngine(0.8, 0.01, 0.5, _loggerFactory.CreateLogger<DefaultEmotionEngine>());

        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Excitement, Intensity = 0.9 });
        var prosody = await engine.MapToVoiceAsync();

        // Excitement increases arousal, which increases speed
        prosody.SpeedMultiplier.Should().BeGreaterThan(1.0);
    }

    [Fact]
    public async Task EmotionEngine_MapToDecisionAsync_ShouldReturnBias()
    {
        var engine = new DefaultEmotionEngine(0.5, 0.01, 0.5, _loggerFactory.CreateLogger<DefaultEmotionEngine>());

        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Curiosity, Intensity = 0.8 });
        var bias = await engine.MapToDecisionAsync();

        bias.Proactivity.Should().BeGreaterThan(0.3); // curious → proactive
    }
}