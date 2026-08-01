using FluentAssertions;
using VPet.Cognition.Security;

namespace VPet.Cognition.Tests;

/// <summary>
/// Tests for security utilities: secret redaction, path validation, command validation.
/// </summary>
public class SecurityTests
{
    [Fact]
    public void SecretRedactor_Redact_ShouldMaskApiKey()
    {
        var json = """{"apiKey":"sk-secret123","model":"qwen2.5:3b"}""";
        var redacted = SecretRedactor.Redact(json);

        redacted.Should().Contain("\"apiKey\":\"***\"");
        redacted.Should().NotContain("sk-secret123");
        redacted.Should().Contain("qwen2.5:3b");
    }

    [Fact]
    public void SecretRedactor_Redact_ShouldMaskMultipleKeys()
    {
        var json = """{"apiKey":"key1","fallbackApiKey":"key2","token":"tok1","name":"test"}""";
        var redacted = SecretRedactor.Redact(json);

        redacted.Should().Contain("\"apiKey\":\"***\"");
        redacted.Should().Contain("\"fallbackApiKey\":\"***\"");
        redacted.Should().Contain("\"token\":\"***\"");
        redacted.Should().Contain("\"name\":\"test\"");
    }

    [Fact]
    public void SecretRedactor_RedactValue_ShouldMaskSensitiveKey()
    {
        SecretRedactor.RedactValue("apiKey", "secret123").Should().Be("***");
        SecretRedactor.RedactValue("ApiKey", "secret123").Should().Be("***");
        SecretRedactor.RedactValue("APIKEY", "secret123").Should().Be("***");
    }

    [Fact]
    public void SecretRedactor_RedactValue_ShouldNotMaskNonSensitiveKey()
    {
        SecretRedactor.RedactValue("model", "qwen2.5:3b").Should().Be("qwen2.5:3b");
        SecretRedactor.RedactValue("endpoint", "localhost").Should().Be("localhost");
    }

    [Fact]
    public void SecretRedactor_IsSensitive_ShouldReturnTrueForSensitiveKeys()
    {
        SecretRedactor.IsSensitive("apiKey").Should().BeTrue();
        SecretRedactor.IsSensitive("token").Should().BeTrue();
        SecretRedactor.IsSensitive("password").Should().BeTrue();
    }

    [Fact]
    public void SecretRedactor_IsSensitive_ShouldReturnFalseForNonSensitiveKeys()
    {
        SecretRedactor.IsSensitive("model").Should().BeFalse();
        SecretRedactor.IsSensitive("endpoint").Should().BeFalse();
    }

    [Fact]
    public void PathValidator_IsSafePath_ShouldRejectTraversal()
    {
        PathValidator.IsSafePath("../../etc/passwd").Should().BeFalse();
        PathValidator.IsSafePath("~/secret").Should().BeFalse();
    }

    [Fact]
    public void PathValidator_IsSafePath_ShouldAcceptNormalPaths()
    {
        PathValidator.IsSafePath("/tmp/file.txt").Should().BeTrue();
        PathValidator.IsSafePath("C:\\Users\\test\\file.txt").Should().BeTrue();
    }

    [Fact]
    public void PathValidator_IsPathAllowed_ShouldAllowWhenInAllowlist()
    {
        var allowed = new List<string> { "/tmp/allowed" };
        PathValidator.IsPathAllowed("/tmp/allowed/file.txt", allowed).Should().BeTrue();
    }

    [Fact]
    public void PathValidator_IsPathAllowed_ShouldDenyWhenOutsideAllowlist()
    {
        var allowed = new List<string> { "/tmp/allowed" };
        PathValidator.IsPathAllowed("/etc/passwd", allowed).Should().BeFalse();
    }

    [Fact]
    public void PathValidator_IsPathAllowed_EmptyAllowlist_ShouldAllowAll()
    {
        var allowed = new List<string>();
        PathValidator.IsPathAllowed("/anywhere/file.txt", allowed).Should().BeTrue();
    }

    [Fact]
    public void CommandValidator_IsDangerous_ShouldDetectRmRf()
    {
        CommandValidator.IsDangerous("rm -rf /").Should().BeTrue();
        CommandValidator.IsDangerous("format C:").Should().BeTrue();
        CommandValidator.IsDangerous("shutdown /s").Should().BeTrue();
    }

    [Fact]
    public void CommandValidator_IsDangerous_ShouldAcceptSafeCommands()
    {
        CommandValidator.IsDangerous("echo hello").Should().BeFalse();
        CommandValidator.IsDangerous("ls -la").Should().BeFalse();
        CommandValidator.IsDangerous("").Should().BeFalse();
    }

    [Fact]
    public void CommandValidator_IsBlocked_ShouldDetectBlockedCommand()
    {
        var blocked = new List<string> { "cmd.exe", "powershell.exe" };
        CommandValidator.IsBlocked("cmd.exe /c dir", blocked).Should().BeTrue();
        CommandValidator.IsBlocked("echo hello", blocked).Should().BeFalse();
    }

    [Fact]
    public void CommandValidator_IsBlocked_EmptyList_ShouldAllowAll()
    {
        var blocked = new List<string>();
        CommandValidator.IsBlocked("anything", blocked).Should().BeFalse();
    }
}