using FluentAssertions;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.Errors;
using VPet.Cognition.Domain.Events;
using VPet.Cognition.Domain.ValueObjects;
using VPet.Cognition.Tests.Fakes;

namespace VPet.Cognition.Tests;

/// <summary>
/// Smoke tests verifying the Domain types and fakes wire together correctly.
/// These are not exhaustive per-domain tests (those live in Phase 1+); they prove
/// Phase 0 foundation is sound.
/// </summary>
public class DomainSmokeTests
{
    [Fact]
    public void MemoryId_New_ShouldProduceUniqueIds()
    {
        var a = MemoryId.New();
        var b = MemoryId.New();
        a.Should().NotBe(b);
        ((Guid)a).Should().NotBeEmpty();
    }

    [Fact]
    public void Importance_ShouldClampToZeroToOne()
    {
        new Importance(-5).Should().Be(0);
        new Importance(0.5).Should().Be(0.5);
        new Importance(10).Should().Be(1);
    }

    [Fact]
    public void EmotionVector_Dominant_ShouldReturnHighestDimension()
    {
        var v = new EmotionVector(joy: 0.9, calmness: 0.3);
        v.Dominant.Should().Be(EmotionType.Joy);
    }

    [Fact]
    public void EmotionVector_ArousalAndValence_ShouldBeInExpectedDirection()
    {
        // Excitement is high-arousal
        var excited = new EmotionVector(excitement: 1.0, calmness: 0.0);
        excited.Arousal.Should().BeGreaterThan(0);

        // Joy is positive-valence
        var joyful = new EmotionVector(joy: 1.0);
        joyful.Valence.Should().BeGreaterThan(0);

        // Sadness is negative-valence
        var sad = new EmotionVector(sadness: 1.0);
        sad.Valence.Should().BeLessThan(0);

        // Calmness is low-arousal
        var calm = new EmotionVector(calmness: 1.0);
        calm.Arousal.Should().BeLessThan(0);
    }

    [Fact]
    public void Embedding_CosineSimilarity_ShouldBeOneForIdenticalVectors()
    {
        var a = new Embedding(new float[] { 1, 0, 0 });
        var b = new Embedding(new float[] { 1, 0, 0 });
        a.CosineSimilarity(b).Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void Embedding_CosineSimilarity_ShouldBeZeroForOrthogonalVectors()
    {
        var a = new Embedding(new float[] { 1, 0 });
        var b = new Embedding(new float[] { 0, 1 });
        a.CosineSimilarity(b).Should().BeApproximately(0.0, 1e-6);
    }

    [Fact]
    public void Result_Success_ShouldCarryValue()
    {
        var r = Result<int>.Success(42);
        r.IsSuccess.Should().BeTrue();
        r.Value.Should().Be(42);
    }

    [Fact]
    public void Result_Failure_ShouldCarryError()
    {
        var r = Result<int>.Failure(ProviderError.Timeout("fake"));
        r.IsFailure.Should().BeTrue();
        r.Error!.Code.Should().Be("LLM_TIMEOUT");
    }

    [Fact]
    public async Task FakeLlmProvider_ChatAsync_ShouldReturnCannedResponse()
    {
        var llm = new FakeLlmProvider();
        var result = await llm.ChatAsync(new ChatRequest());
        result.IsSuccess.Should().BeTrue();
        result.Value!.Content.Should().Be("Hello from the fake LLM!");
    }

    [Fact]
    public async Task FakeMemoryStore_StoreAndRetrieve_ShouldRoundTrip()
    {
        var store = new FakeMemoryStore();
        var mem = new Memory { Type = MemoryType.Semantic, Content = "user likes coffee" };
        await store.StoreAsync(mem);
        var retrieved = await store.GetAsync(mem.Id);
        retrieved.Should().NotBeNull();
        retrieved!.Content.Should().Be("user likes coffee");
        retrieved.AccessCount.Should().Be(1);
    }

    [Fact]
    public async Task FakeMemoryStore_DeleteAll_ShouldClearStore()
    {
        var store = new FakeMemoryStore();
        await store.StoreAsync(new Memory { Type = MemoryType.Episodic, Content = "event" });
        (await store.CountAsync()).Should().Be(1);
        await store.DeleteAllAsync();
        (await store.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task FakeEmotionEngine_Apply_ShouldUpdateState()
    {
        var engine = new FakeEmotionEngine();
        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Joy, Intensity = 0.8 });
        engine.State.Vector.Joy.Should().BeApproximately(0.8, 1e-6);
        engine.ApplyCount.Should().Be(1);
    }

    [Fact]
    public async Task FakeEmotionEngine_Decay_ShouldReturnToNeutral()
    {
        var engine = new FakeEmotionEngine();
        await engine.ApplyAsync(new EmotionStimulus { Primary = EmotionType.Joy, Intensity = 0.9 });
        engine.State.Vector.Joy.Should().BeGreaterThan(0);
        await engine.DecayAsync();
        engine.State.Vector.Should().Be(EmotionVector.Neutral);
    }

    [Fact]
    public async Task FakePersonalityEngine_Evolve_ShouldIncrementCount()
    {
        var engine = new FakePersonalityEngine();
        await engine.EvolveAsync(new PersonalityDelta { TrustDelta = 0.1 });
        engine.EvolveCount.Should().Be(1);
        engine.State.EvolutionCount.Should().Be(1);
        engine.State.Relationship.Trust.Should().BeApproximately(0.1, 1e-6);
    }

    [Fact]
    public async Task FakeToolProvider_Execute_ShouldReturnConfiguredResult()
    {
        var tool = new FakeToolProvider { NextResult = new ToolResult(true, "done", null, TimeSpan.FromMilliseconds(10)) };
        var result = await tool.ExecuteAsync(System.Text.Json.JsonSerializer.SerializeToElement(new { }));
        result.Success.Should().BeTrue();
        result.Output.Should().Be("done");
        tool.ExecuteCount.Should().Be(1);
    }

    [Fact]
    public async Task FakePermissionService_GrantAndCheck_ShouldReturnGranted()
    {
        var perm = new FakePermissionService { AutoState = PermissionState.Granted };
        var state = await perm.CheckAsync("test-tool");
        state.Should().Be(PermissionState.Granted);
    }

    [Fact]
    public async Task FakeAgentRuntime_SetGoal_ShouldRecordGoal()
    {
        var agent = new FakeAgentRuntime();
        var id = await agent.SetGoalAsync("test goal", GoalPriority.Normal);
        var active = await agent.GetActiveGoalsAsync();
        active.Should().HaveCount(1);
        active[0].Description.Should().Be("test goal");
    }

    [Fact]
    public void FakeEventBus_Publish_ShouldRecordEvent()
    {
        var bus = new FakeEventBus();
        bus.Publish(new MemoryStoredEvent(MemoryId.New(), MemoryType.Semantic));
        bus.PublishedEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task FakeEventBus_Subscribe_ShouldReceivePublishedEvents()
    {
        var bus = new FakeEventBus();
        MemoryStoredEvent? received = null;
        bus.Subscribe<MemoryStoredEvent>((e, ct) => { received = e; return Task.CompletedTask; });
        var evt = new MemoryStoredEvent(MemoryId.New(), MemoryType.Episodic);
        bus.Publish(evt);
        received.Should().NotBeNull();
        received!.Type.Should().Be(MemoryType.Episodic);
        await Task.CompletedTask;
    }
}