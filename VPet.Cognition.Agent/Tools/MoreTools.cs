using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.AgentRuntime.Tools;

/// <summary>
/// Tool to execute PowerShell commands. Requires confirmation.
/// </summary>
public sealed class PowerShellTool : IToolProvider
{
    public string ToolName => "powershell";
    public ToolClass Class => ToolClass.PowerShell;

    public ToolSchema Schema { get; } = new()
    {
        Name = "powershell",
        Description = "Execute a PowerShell command and return the output.",
        ParametersJsonSchema = JsonDocument.Parse("""
            {"type":"object","properties":{"command":{"type":"string","description":"PowerShell command to execute"},"timeout_sec":{"type":"integer","description":"Timeout in seconds (default 30)"}},"required":["command"]}
            """).RootElement,
        RequiresConfirmation = true,
        AllowAlways = false,
    };

    private readonly ILogger<PowerShellTool> _logger;
    private readonly List<string> _blockedCommands;

    public PowerShellTool(ILogger<PowerShellTool> logger, List<string>? blockedCommands = null)
    {
        _logger = logger;
        _blockedCommands = blockedCommands ?? new();
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("command", out var cmdEl) || cmdEl.ValueKind != JsonValueKind.String)
            return new ToolResult(false, string.Empty, "Missing 'command' argument", TimeSpan.Zero);

        var command = cmdEl.GetString()!;
        var timeoutSec = arguments.TryGetProperty("timeout_sec", out var tEl) && tEl.TryGetInt32(out var t) ? t : 30;

        // Check blocklist
        var lowerCmd = command.ToLowerInvariant();
        foreach (var blocked in _blockedCommands)
        {
            if (lowerCmd.Contains(blocked.ToLowerInvariant()))
            {
                _logger.LogWarning("PowerShell command blocked (matches '{Blocked}'): {Cmd}", blocked, command);
                return new ToolResult(false, string.Empty, $"Command blocked: contains '{blocked}'", TimeSpan.Zero);
            }
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "powershell.exe" : "pwsh",
                Arguments = $"-NoProfile -Command \"{command.Replace("\"", "\\\"")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                return new ToolResult(false, string.Empty, "Failed to start process", sw.Elapsed);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

            var stdout = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            var stderr = await proc.StandardError.ReadToEndAsync(cts.Token);

            if (!proc.HasExited)
                proc.Kill(true);

            await proc.WaitForExitAsync(cts.Token);
            sw.Stop();

            var output = stdout.Trim();
            if (!string.IsNullOrEmpty(stderr))
                output += $"\n[STDERR]: {stderr.Trim()}";

            _logger.LogInformation("PowerShell executed: {Cmd} → {Len} chars", command, output.Length);
            return new ToolResult(true, output, null, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PowerShell execution failed: {Cmd}", command);
            return new ToolResult(false, string.Empty, ex.Message, sw.Elapsed);
        }
    }
}

/// <summary>
/// Tool to execute CMD commands (Windows only). Requires confirmation.
/// </summary>
public sealed class CmdTool : IToolProvider
{
    public string ToolName => "cmd";
    public ToolClass Class => ToolClass.CMD;

    public ToolSchema Schema { get; } = new()
    {
        Name = "cmd",
        Description = "Execute a CMD command (Windows only) and return the output.",
        ParametersJsonSchema = JsonDocument.Parse("""
            {"type":"object","properties":{"command":{"type":"string","description":"CMD command to execute"},"timeout_sec":{"type":"integer","description":"Timeout in seconds (default 30)"}},"required":["command"]}
            """).RootElement,
        RequiresConfirmation = true,
        AllowAlways = false,
    };

    private readonly ILogger<CmdTool> _logger;
    private readonly List<string> _blockedCommands;

    public CmdTool(ILogger<CmdTool> logger, List<string>? blockedCommands = null)
    {
        _logger = logger;
        _blockedCommands = blockedCommands ?? new();
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindows())
            return new ToolResult(false, string.Empty, "CMD is only available on Windows", TimeSpan.Zero);

        if (!arguments.TryGetProperty("command", out var cmdEl) || cmdEl.ValueKind != JsonValueKind.String)
            return new ToolResult(false, string.Empty, "Missing 'command' argument", TimeSpan.Zero);

        var command = cmdEl.GetString()!;
        var timeoutSec = arguments.TryGetProperty("timeout_sec", out var tEl) && tEl.TryGetInt32(out var t) ? t : 30;

        var lowerCmd = command.ToLowerInvariant();
        foreach (var blocked in _blockedCommands)
        {
            if (lowerCmd.Contains(blocked.ToLowerInvariant()))
            {
                _logger.LogWarning("CMD command blocked (matches '{Blocked}'): {Cmd}", blocked, command);
                return new ToolResult(false, string.Empty, $"Command blocked: contains '{blocked}'", TimeSpan.Zero);
            }
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc is null)
                return new ToolResult(false, string.Empty, "Failed to start process", sw.Elapsed);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

            var stdout = await proc.StandardOutput.ReadToEndAsync(cts.Token);
            var stderr = await proc.StandardError.ReadToEndAsync(cts.Token);

            if (!proc.HasExited)
                proc.Kill(true);

            await proc.WaitForExitAsync(cts.Token);
            sw.Stop();

            var output = stdout.Trim();
            if (!string.IsNullOrEmpty(stderr))
                output += $"\n[STDERR]: {stderr.Trim()}";

            _logger.LogInformation("CMD executed: {Cmd} → {Len} chars", command, output.Length);
            return new ToolResult(true, output, null, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CMD execution failed: {Cmd}", command);
            return new ToolResult(false, string.Empty, ex.Message, sw.Elapsed);
        }
    }
}

/// <summary>
/// Tool to write text to a file. Requires confirmation.
/// </summary>
public sealed class FileWriteTool : IToolProvider
{
    public string ToolName => "file_write";
    public ToolClass Class => ToolClass.File;

    public ToolSchema Schema { get; } = new()
    {
        Name = "file_write",
        Description = "Write text content to a file. Creates or overwrites the file.",
        ParametersJsonSchema = JsonDocument.Parse("""
            {"type":"object","properties":{"path":{"type":"string","description":"File path to write"},"content":{"type":"string","description":"Text content to write"},"append":{"type":"boolean","description":"If true, append instead of overwrite (default false)"}},"required":["path","content"]}
            """).RootElement,
        RequiresConfirmation = true,
        AllowAlways = false,
    };

    private readonly ILogger<FileWriteTool> _logger;
    private readonly List<string> _allowedPaths;

    public FileWriteTool(ILogger<FileWriteTool> logger, List<string>? allowedPaths = null)
    {
        _logger = logger;
        _allowedPaths = allowedPaths ?? new();
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return new ToolResult(false, string.Empty, "Missing 'path' argument", TimeSpan.Zero);
        if (!arguments.TryGetProperty("content", out var contentEl) || contentEl.ValueKind != JsonValueKind.String)
            return new ToolResult(false, string.Empty, "Missing 'content' argument", TimeSpan.Zero);

        var path = pathEl.GetString()!;
        var content = contentEl.GetString()!;
        var append = arguments.TryGetProperty("append", out var aEl) && aEl.GetBoolean();

        if (!IsPathAllowed(path))
        {
            _logger.LogWarning("File write denied (path not in allowlist): {Path}", path);
            return new ToolResult(false, string.Empty, $"Path not allowed: {path}", TimeSpan.Zero);
        }

        var sw = Stopwatch.StartNew();
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (append)
                await File.AppendAllTextAsync(path, content, cancellationToken);
            else
                await File.WriteAllTextAsync(path, content, cancellationToken);

            _logger.LogInformation("File written: {Path} ({Len} chars, append={Append})", path, content.Length, append);
            return new ToolResult(true, $"Written {content.Length} chars to {path}", null, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write file: {Path}", path);
            return new ToolResult(false, string.Empty, ex.Message, sw.Elapsed);
        }
    }

    private bool IsPathAllowed(string path)
    {
        if (_allowedPaths.Count == 0) return true;
        var fullPath = Path.GetFullPath(path);
        return _allowedPaths.Any(allowed =>
            fullPath.StartsWith(Path.GetFullPath(allowed), StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Tool to list, create, or delete folders.
/// </summary>
public sealed class FolderTool : IToolProvider
{
    public string ToolName => "folder";
    public ToolClass Class => ToolClass.Folder;

    public ToolSchema Schema { get; } = new()
    {
        Name = "folder",
        Description = "List, create, or delete a folder.",
        ParametersJsonSchema = JsonDocument.Parse("""
            {"type":"object","properties":{"action":{"type":"string","enum":["list","create","delete"],"description":"Action to perform"},"path":{"type":"string","description":"Folder path"}},"required":["action","path"]}
            """).RootElement,
        RequiresConfirmation = true,
        AllowAlways = false,
    };

    private readonly ILogger<FolderTool> _logger;

    public FolderTool(ILogger<FolderTool> logger) => _logger = logger;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("action", out var actionEl) || actionEl.ValueKind != JsonValueKind.String)
            return Task.FromResult(new ToolResult(false, string.Empty, "Missing 'action' argument", TimeSpan.Zero));
        if (!arguments.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return Task.FromResult(new ToolResult(false, string.Empty, "Missing 'path' argument", TimeSpan.Zero));

        var action = actionEl.GetString()!;
        var path = pathEl.GetString()!;
        var sw = Stopwatch.StartNew();

        try
        {
            switch (action.ToLowerInvariant())
            {
                case "list":
                    if (!Directory.Exists(path))
                        return Task.FromResult(new ToolResult(false, string.Empty, $"Folder not found: {path}", sw.Elapsed));
                    var entries = Directory.GetFileSystemEntries(path)
                        .Select(e => (Directory.Exists(e) ? "[DIR] " : "[FILE] ") + Path.GetFileName(e))
                        .Take(200); // Limit to 200 entries
                    var listing = string.Join("\n", entries);
                    _logger.LogInformation("Listed folder: {Path} ({Count} entries)", path, entries.Count());
                    return Task.FromResult(new ToolResult(true, listing, null, sw.Elapsed));

                case "create":
                    Directory.CreateDirectory(path);
                    _logger.LogInformation("Created folder: {Path}", path);
                    return Task.FromResult(new ToolResult(true, $"Created: {path}", null, sw.Elapsed));

                case "delete":
                    if (!Directory.Exists(path))
                        return Task.FromResult(new ToolResult(false, string.Empty, $"Folder not found: {path}", sw.Elapsed));
                    Directory.Delete(path, recursive: true);
                    _logger.LogInformation("Deleted folder: {Path}", path);
                    return Task.FromResult(new ToolResult(true, $"Deleted: {path}", null, sw.Elapsed));

                default:
                    return Task.FromResult(new ToolResult(false, string.Empty, $"Unknown action: {action}", sw.Elapsed));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Folder operation failed: {Action} {Path}", action, path);
            return Task.FromResult(new ToolResult(false, string.Empty, ex.Message, sw.Elapsed));
        }
    }
}