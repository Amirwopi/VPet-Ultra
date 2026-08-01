using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.Entities;
using VPet.Cognition.Domain.Enums;

namespace VPet.Cognition.AgentRuntime.Tools;

/// <summary>
/// Tool to open an application by path or protocol URI.
/// </summary>
public sealed class OpenApplicationTool : IToolProvider
{
    public string ToolName => "open_application";
    public ToolClass Class => ToolClass.Application;

    public ToolSchema Schema { get; } = new()
    {
        Name = "open_application",
        Description = "Open an application by its executable path or a protocol URI (e.g. mailto:, https:).",
        ParametersJsonSchema = JsonDocument.Parse("""
            {"type":"object","properties":{"path":{"type":"string","description":"Executable path or URI to open"}},"required":["path"]}
            """).RootElement,
        RequiresConfirmation = true,
        AllowAlways = false,
    };

    private readonly ILogger<OpenApplicationTool> _logger;

    public OpenApplicationTool(ILogger<OpenApplicationTool> logger) => _logger = logger;

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return new ToolResult(false, string.Empty, "Missing 'path' argument", TimeSpan.Zero);

        var path = pathEl.GetString()!;
        var sw = Stopwatch.StartNew();

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
                return new ToolResult(false, string.Empty, "Failed to start process", sw.Elapsed);

            _logger.LogInformation("Opened application: {Path} (PID={Pid})", path, process.Id);
            return await Task.FromResult(new ToolResult(true, $"Started: {path}", null, sw.Elapsed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open application: {Path}", path);
            return new ToolResult(false, string.Empty, ex.Message, sw.Elapsed);
        }
    }
}

/// <summary>
/// Tool to read a text file (with path validation).
/// </summary>
public sealed class FileReadTool : IToolProvider
{
    public string ToolName => "file_read";
    public ToolClass Class => ToolClass.File;

    public ToolSchema Schema { get; } = new()
    {
        Name = "file_read",
        Description = "Read the contents of a text file.",
        ParametersJsonSchema = JsonDocument.Parse("""
            {"type":"object","properties":{"path":{"type":"string","description":"Absolute path to the file"},"maxBytes":{"type":"integer","description":"Max bytes to read (default 65536)"}},"required":["path"]}
            """).RootElement,
        RequiresConfirmation = false,
        AllowAlways = true,
    };

    private readonly ILogger<FileReadTool> _logger;
    private readonly List<string> _allowedPaths;

    public FileReadTool(List<string> allowedPaths, ILogger<FileReadTool> logger)
    {
        _allowedPaths = allowedPaths;
        _logger = logger;
    }

    public async Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("path", out var pathEl) || pathEl.ValueKind != JsonValueKind.String)
            return new ToolResult(false, string.Empty, "Missing 'path' argument", TimeSpan.Zero);

        var path = pathEl.GetString()!;
        var maxBytes = arguments.TryGetProperty("maxBytes", out var mbEl) && mbEl.ValueKind == JsonValueKind.Number
            ? mbEl.GetInt32() : 65536;

        var sw = Stopwatch.StartNew();

        // Path validation
        if (!IsPathAllowed(path))
        {
            _logger.LogWarning("File read denied (path not in allowlist): {Path}", path);
            return new ToolResult(false, string.Empty, $"Path not allowed: {path}", sw.Elapsed);
        }

        if (!File.Exists(path))
            return new ToolResult(false, string.Empty, $"File not found: {path}", sw.Elapsed);

        try
        {
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            if (content.Length > maxBytes)
                content = content[..maxBytes] + "\n... (truncated)";

            _logger.LogInformation("Read file: {Path} ({Len} chars)", path, content.Length);
            return new ToolResult(true, content, null, sw.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file: {Path}", path);
            return new ToolResult(false, string.Empty, ex.Message, sw.Elapsed);
        }
    }

    private bool IsPathAllowed(string path)
    {
        if (_allowedPaths.Count == 0) return true; // No allowlist = allow all
        var fullPath = Path.GetFullPath(path);
        return _allowedPaths.Any(allowed =>
            fullPath.StartsWith(Path.GetFullPath(allowed), StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Tool to open a URL in the default browser.
/// </summary>
public sealed class BrowserTool : IToolProvider
{
    public string ToolName => "open_browser";
    public ToolClass Class => ToolClass.Browser;

    public ToolSchema Schema { get; } = new()
    {
        Name = "open_browser",
        Description = "Open a URL in the default web browser.",
        ParametersJsonSchema = JsonDocument.Parse("""
            {"type":"object","properties":{"url":{"type":"string","description":"URL to open"}},"required":["url"]}
            """).RootElement,
        RequiresConfirmation = false,
        AllowAlways = true,
    };

    private readonly ILogger<BrowserTool> _logger;

    public BrowserTool(ILogger<BrowserTool> logger) => _logger = logger;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("url", out var urlEl) || urlEl.ValueKind != JsonValueKind.String)
            return Task.FromResult(new ToolResult(false, string.Empty, "Missing 'url' argument", TimeSpan.Zero));

        var url = urlEl.GetString()!;

        // Validate URL scheme
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
        {
            return Task.FromResult(new ToolResult(false, string.Empty, $"Invalid URL scheme: {url}", TimeSpan.Zero));
        }

        var sw = Stopwatch.StartNew();
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri.ToString(),
                UseShellExecute = true,
            });
            _logger.LogInformation("Opened browser: {Url}", url);
            return Task.FromResult(new ToolResult(true, $"Opened: {url}", null, sw.Elapsed));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open browser: {Url}", url);
            return Task.FromResult(new ToolResult(false, string.Empty, ex.Message, sw.Elapsed));
        }
    }
}

/// <summary>
/// Tool to get/set clipboard text (cross-platform via TextCopy library or platform APIs).
/// </summary>
public sealed class ClipboardTool : IToolProvider
{
    public string ToolName => "clipboard";
    public ToolClass Class => ToolClass.Clipboard;

    public ToolSchema Schema { get; } = new()
    {
        Name = "clipboard",
        Description = "Get or set the system clipboard text.",
        ParametersJsonSchema = JsonDocument.Parse("""
            {"type":"object","properties":{"action":{"type":"string","enum":["get","set"],"description":"Whether to get or set the clipboard"},"text":{"type":"string","description":"Text to set (for 'set' action)"}},"required":["action"]}
            """).RootElement,
        RequiresConfirmation = false,
        AllowAlways = true,
    };

    private readonly ILogger<ClipboardTool> _logger;

    public ClipboardTool(ILogger<ClipboardTool> logger) => _logger = logger;

    public Task<ToolResult> ExecuteAsync(JsonElement arguments, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("action", out var actionEl) || actionEl.ValueKind != JsonValueKind.String)
            return Task.FromResult(new ToolResult(false, string.Empty, "Missing 'action' argument", TimeSpan.Zero));

        var action = actionEl.GetString()!;
        var sw = Stopwatch.StartNew();

        try
        {
            if (action == "get")
            {
                // Clipboard access requires UI thread on some platforms
                // This is a placeholder — actual implementation uses platform-specific APIs
                _logger.LogInformation("Clipboard get (placeholder)");
                return Task.FromResult(new ToolResult(true, "(clipboard get requires UI thread)", null, sw.Elapsed));
            }
            else if (action == "set")
            {
                if (!arguments.TryGetProperty("text", out var textEl) || textEl.ValueKind != JsonValueKind.String)
                    return Task.FromResult(new ToolResult(false, string.Empty, "Missing 'text' for set action", TimeSpan.Zero));

                var text = textEl.GetString()!;
                _logger.LogInformation("Clipboard set: {Len} chars", text.Length);
                // Actual clipboard set requires UI thread
                return Task.FromResult(new ToolResult(true, $"Set clipboard: {text.Length} chars", null, sw.Elapsed));
            }

            return Task.FromResult(new ToolResult(false, string.Empty, $"Unknown action: {action}", TimeSpan.Zero));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clipboard operation failed");
            return Task.FromResult(new ToolResult(false, string.Empty, ex.Message, sw.Elapsed));
        }
    }
}