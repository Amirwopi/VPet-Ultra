namespace VPet.Cognition.Host.Options;

/// <summary>
/// Root options object for the cognition host. Bound from appsettings.json + Setting.lps cognition lines.
/// </summary>
public sealed class CognitionOptions
{
    public LlmOptions Llm { get; set; } = new();
    public MemoryOptions Memory { get; set; } = new();
    public PersonalityOptions Personality { get; set; } = new();
    public EmotionOptions Emotion { get; set; } = new();
    public VoiceOptions Voice { get; set; } = new();
    public ToolOptions Tools { get; set; } = new();
    public AgentOptions Agent { get; set; } = new();
    public AnimationOptions Animation { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public bool Enabled { get; set; } = true;
}

public sealed class LlmOptions
{
    public string Provider { get; set; } = "ollama";
    public string Endpoint { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5:7b";
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);
    public int MaxTokens { get; set; } = 1024;
    public double Temperature { get; set; } = 0.7;
    public int? Seed { get; set; }
    public string? FallbackProvider { get; set; }
    public string? FallbackEndpoint { get; set; }
    public string? FallbackModel { get; set; }
    public string? FallbackApiKey { get; set; }
}

public sealed class MemoryOptions
{
    public string DbPath { get; set; } = "data/cognition.db";
    public bool Encrypt { get; set; }
    public int VectorDimensions { get; set; } = 768;
    public string EmbeddingProvider { get; set; } = "local";
    public string EmbeddingModel { get; set; } = "models/bge-small.onnx";
    public TimeSpan ConsolidationInterval { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan ForgettingInterval { get; set; } = TimeSpan.FromDays(1);
    public int MaxDbSizeMb { get; set; } = 500;
}

public sealed class PersonalityOptions
{
    public PersonalitySeed Seed { get; set; } = new();
    public double EvolutionRate { get; set; } = 0.02;
    public int ReflectionIntervalTurns { get; set; } = 10;
    public int EvaluationIntervalTurns { get; set; } = 5;
    public double CoreTraitMaxDrift { get; set; } = 0.1;
}

public sealed class PersonalitySeed
{
    public string Name { get; set; } = "Vpet";
    public string Description { get; set; } = "A desktop companion.";
}

public sealed class EmotionOptions
{
    public double BlendFactor { get; set; } = 0.3;
    public double DecayRatePerSec { get; set; } = 0.01;
    public TimeSpan PersistInterval { get; set; } = TimeSpan.FromMinutes(1);
    public double AntagonisticSuppression { get; set; } = 0.5;
    public string SentimentMethod { get; set; } = "rule";
}

public sealed class VoiceOptions
{
    public bool Enabled { get; set; }
    public WakeWordOptions WakeWord { get; set; } = new();
    public SttOptions Stt { get; set; } = new();
    public TtsOptions Tts { get; set; } = new();
    public bool BargeIn { get; set; } = true;
    public bool ContinuousMode { get; set; }
    public int SilenceTimeoutSec { get; set; } = 3;
}

public sealed class WakeWordOptions
{
    public string Provider { get; set; } = "openwakeword";
    public string Model { get; set; } = "hey_vpet.onnx";
    public double Threshold { get; set; } = 0.5;
}

public sealed class SttOptions
{
    public string Provider { get; set; } = "faster-whisper";
    public string Model { get; set; } = "base";
    public string Language { get; set; } = "auto";
    public string Device { get; set; } = "cpu";
}

public sealed class TtsOptions
{
    public string Provider { get; set; } = "piper";
    public string Model { get; set; } = "en_US-amy-medium.onnx";
    public string Device { get; set; } = "cpu";
}

public sealed class ToolOptions
{
    public bool Enabled { get; set; } = true;
    public int TimeoutSec { get; set; } = 30;
    public List<string> AllowedPaths { get; set; } = new();
    public List<string> BlockedCommands { get; set; } = new();
    public List<string> DangerousPatterns { get; set; } = new();
    public List<string> Win32Allowlist { get; set; } = new();
    public int PromptTimeoutSec { get; set; } = 30;
}

public sealed class AgentOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxStepsPerPlan { get; set; } = 10;
    public int MaxRetries { get; set; } = 3;
    public double ConfidenceThreshold { get; set; } = 0.5;
    public double MinConfidence { get; set; } = 0.3;
    public int MaxConcurrentGoals { get; set; } = 3;
}

public sealed class AnimationOptions
{
    public bool EmotionBiasEnabled { get; set; } = true;
    public double EmotionOverrideThreshold { get; set; } = 0.6;
    public bool LipSyncEnabled { get; set; } = true;
    public int LipSyncVisemeCount { get; set; } = 5;
}

public sealed class LoggingOptions
{
    public string Level { get; set; } = "Information";
    public string File { get; set; } = "logs/cognition-.log";
    public bool Console { get; set; } = true;
    public List<string> RedactKeys { get; set; } = new() { "apiKey", "fallbackApiKey" };
}