using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Host.Options;

namespace VPet.Cognition.Host.Config;

/// <summary>
/// Loads and binds CognitionOptions from an appsettings.json file.
/// Falls back to defaults if the file or section is missing.
/// </summary>
public sealed class CognitionConfigLoader
{
    private readonly ILogger<CognitionConfigLoader> _logger;

    public CognitionConfigLoader(ILogger<CognitionConfigLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Load options from a JSON file. Returns defaults if file not found.
    /// </summary>
    public CognitionOptions Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogInformation("Config file {Path} not found; using defaults", filePath);
            return new CognitionOptions();
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("Cognition", out var section))
            {
                var options = JsonSerializer.Deserialize<CognitionOptions>(section.GetRawText());
                if (options is not null)
                {
                    _logger.LogInformation("Loaded cognition config from {Path}", filePath);
                    return options;
                }
            }

            _logger.LogWarning("No 'Cognition' section in {Path}; using defaults", filePath);
            return new CognitionOptions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load config from {Path}; using defaults", filePath);
            return new CognitionOptions();
        }
    }

    /// <summary>
    /// Save options to a JSON file.
    /// </summary>
    public void Save(CognitionOptions options, string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var wrapper = new { Cognition = options };
        var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });

        // Redact secrets before writing
        json = VPet.Cognition.Host.Security.SecretRedactor.Redact(json);

        File.WriteAllText(filePath, json);
        _logger.LogInformation("Saved cognition config to {Path}", filePath);
    }

    /// <summary>
    /// Create a default appsettings.json template.
    /// </summary>
    public static string CreateDefaultTemplate()
    {
        var options = new CognitionOptions();
        var wrapper = new { Cognition = options };
        return JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
    }
}