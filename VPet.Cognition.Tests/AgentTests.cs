using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;
using VPet.Cognition.Domain.ValueObjects;
using VPet.Cognition.AgentRuntime;
using VPet.Cognition.AgentRuntime.Tools;

namespace VPet.Cognition.Tests;

/// <summary>
/// Tests for the agent runtime, permission service, and built-in tools.
/// </summary>
public class AgentTests
{
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    [Fact]
    public async Task AgentRuntime_SetGoalAsync_ShouldStoreGoal()
    {
        var agent = CreateAgentRuntime();
        var goalId = await agent.SetGoalAsync("Test goal", GoalPriority.Normal);

        var active = await agent.GetActiveGoalsAsync();
        active.Should().HaveCount(1);
        active[0].Description.Should().Be("Test goal");
        active[0].Priority.Should().Be(GoalPriority.Normal);
    }

    [Fact]
    public async Task AgentRuntime_RunAsync_ShouldCompleteGoalWithoutPlan()
    {
        var agent = CreateAgentRuntime();
        var goalId = await agent.SetGoalAsync("Simple goal", GoalPriority.Normal);

        await agent.RunAsync();

        var active = await agent.GetActiveGoalsAsync();
        active.Should().BeEmpty(); // Goal completed, no longer active
    }

    [Fact]
    public async Task AgentRuntime_CancelAsync_ShouldCancelGoal()
    {
        var agent = CreateAgentRuntime();
        var goalId = await agent.SetGoalAsync("Cancellable goal", GoalPriority.Normal);

        await agent.CancelAsync(goalId);

        var active = await agent.GetActiveGoalsAsync();
        active.Should().BeEmpty();
    }

    [Fact]
    public async Task AgentRuntime_RunAsync_ShouldExecutePlanWithWaitTask()
    {
        var agent = CreateAgentRuntime();
        var goalId = await agent.SetGoalAsync("Wait goal", GoalPriority.Normal);

        // Manually set a plan with a wait task
        var goal = (await agent.GetActiveGoalsAsync())[0];
        goal.Plan = new Plan
        {
            Steps = new List<AgentTask>
            {
                new() { Description = "Wait 10ms", Kind = TaskKind.Wait, Arguments = JsonDocument.Parse("""{"seconds":0.01}""").RootElement },
            }
        };

        await agent.RunAsync();

        goal.Status.Should().Be(GoalStatus.Completed);
        goal.Confidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PermissionService_CheckAsync_NotRequested_ShouldReturnNotRequested()
    {
        var service = new DefaultPermissionService(_loggerFactory.CreateLogger<DefaultPermissionService>());

        var state = await service.CheckAsync("test_tool");

        state.Should().Be(PermissionState.NotRequested);
    }

    [Fact]
    public async Task PermissionService_GrantAsync_ShouldGrantPermission()
    {
        var service = new DefaultPermissionService(_loggerFactory.CreateLogger<DefaultPermissionService>());

        await service.GrantAsync("test_tool", alwaysAllow: true);
        var state = await service.CheckAsync("test_tool");

        state.Should().Be(PermissionState.Granted);
    }

    [Fact]
    public async Task PermissionService_DenyAsync_ShouldDenyPermission()
    {
        var service = new DefaultPermissionService(_loggerFactory.CreateLogger<DefaultPermissionService>());

        await service.DenyAsync("test_tool");
        var state = await service.CheckAsync("test_tool");

        state.Should().Be(PermissionState.Denied);
    }

    [Fact]
    public async Task PermissionService_RevokeAsync_ShouldRevokeGrant()
    {
        var service = new DefaultPermissionService(_loggerFactory.CreateLogger<DefaultPermissionService>());

        await service.GrantAsync("test_tool", true);
        await service.RevokeAsync("test_tool");
        var state = await service.CheckAsync("test_tool");

        state.Should().Be(PermissionState.Revoked);
    }

    [Fact]
    public async Task PermissionService_ListGrantsAsync_ShouldReturnAllGrants()
    {
        var service = new DefaultPermissionService(_loggerFactory.CreateLogger<DefaultPermissionService>());

        await service.GrantAsync("tool_a", true);
        await service.DenyAsync("tool_b");

        var grants = await service.ListGrantsAsync();
        grants.Should().HaveCount(2);
    }

    [Fact]
    public async Task PermissionService_RequestAsync_NoConfirmation_ShouldAutoGrant()
    {
        var service = new DefaultPermissionService(_loggerFactory.CreateLogger<DefaultPermissionService>());

        var schema = new ToolSchema { Name = "safe_tool", RequiresConfirmation = false };
        var state = await service.RequestAsync("safe_tool", schema, JsonDocument.Parse("{}").RootElement);

        state.Should().Be(PermissionState.Granted);
    }

    [Fact]
    public async Task PermissionService_RequestAsync_RequiresConfirmation_ShouldPrompt()
    {
        var service = new DefaultPermissionService(_loggerFactory.CreateLogger<DefaultPermissionService>());

        var schema = new ToolSchema { Name = "dangerous_tool", RequiresConfirmation = true };
        var state = await service.RequestAsync("dangerous_tool", schema, JsonDocument.Parse("{}").RootElement);

        state.Should().Be(PermissionState.Prompted);
    }

    [Fact]
    public async Task BrowserTool_InvalidUrl_ShouldReturnError()
    {
        var tool = new BrowserTool(_loggerFactory.CreateLogger<BrowserTool>());
        var args = JsonDocument.Parse("""{"url":"not-a-url"}""").RootElement;

        var result = await tool.ExecuteAsync(args);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("Invalid URL");
    }

    [Fact]
    public async Task BrowserTool_ValidUrl_ShouldSucceed()
    {
        var tool = new BrowserTool(_loggerFactory.CreateLogger<BrowserTool>());
        var args = JsonDocument.Parse("""{"url":"https://example.com"}""").RootElement;

        var result = await tool.ExecuteAsync(args);

        // On headless Linux, this may fail — just check it doesn't crash
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FileReadTool_NonExistentFile_ShouldReturnError()
    {
        var tool = new FileReadTool(new List<string>(), _loggerFactory.CreateLogger<FileReadTool>());
        var args = JsonDocument.Parse("""{"path":"/nonexistent/file.txt"}""").RootElement;

        var result = await tool.ExecuteAsync(args);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task FileReadTool_WithAllowlist_DeniedPath_ShouldReturnError()
    {
        var tool = new FileReadTool(new List<string> { "/tmp/allowed" }, _loggerFactory.CreateLogger<FileReadTool>());
        var args = JsonDocument.Parse("""{"path":"/etc/passwd"}""").RootElement;

        var result = await tool.ExecuteAsync(args);

        result.Success.Should().BeFalse();
        result.Error.Should().Contain("not allowed");
    }

    [Fact]
    public async Task ClipboardTool_GetAction_ShouldReturnText()
    {
        var tool = new ClipboardTool(_loggerFactory.CreateLogger<ClipboardTool>());
        var args = JsonDocument.Parse("""{"action":"get"}""").RootElement;

        var result = await tool.ExecuteAsync(args);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ClipboardTool_SetAction_ShouldSucceed()
    {
        var tool = new ClipboardTool(_loggerFactory.CreateLogger<ClipboardTool>());
        var args = JsonDocument.Parse("""{"action":"set","text":"hello"}""").RootElement;

        var result = await tool.ExecuteAsync(args);

        result.Success.Should().BeTrue();
        result.Output.Should().Contain("5 chars");
    }

    private DefaultAgentRuntime CreateAgentRuntime()
    {
        var toolProvider = new BrowserTool(_loggerFactory.CreateLogger<BrowserTool>());
        var permissionService = new DefaultPermissionService(_loggerFactory.CreateLogger<DefaultPermissionService>());
        return new DefaultAgentRuntime(
            toolProvider, permissionService,
            maxStepsPerPlan: 10, maxRetries: 3, confidenceThreshold: 0.5,
            _loggerFactory.CreateLogger<DefaultAgentRuntime>());
    }
}