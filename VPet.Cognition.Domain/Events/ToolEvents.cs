using System.Text.Json;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.Domain.Events;

/// <summary>
/// Tool-related domain events.
/// </summary>
public sealed record ToolRequestedEvent(string ToolName, ToolClass Class, JsonElement Arguments) : CognitionEvent;
public sealed record PermissionPromptedEvent(string ToolName) : CognitionEvent;
public sealed record PermissionGrantedEvent(string ToolName, bool AlwaysAllow) : CognitionEvent;
public sealed record PermissionDeniedEvent(string ToolName) : CognitionEvent;
public sealed record ToolExecutedEvent(string ToolName, bool Success, TimeSpan Duration) : CognitionEvent;
public sealed record ToolFailedEvent(string ToolName, string Error) : CognitionEvent;