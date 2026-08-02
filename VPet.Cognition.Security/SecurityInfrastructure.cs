using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace VPet.Cognition.Security;

/// <summary>
/// HTTP delegating handler that filters outbound requests to only allowlisted domains.
/// Prevents SSRF and data exfiltration to unauthorized endpoints.
/// </summary>
public sealed class EgressFilterHandler : DelegatingHandler
{
    private readonly HashSet<string> _allowlist;
    private readonly ILogger<EgressFilterHandler> _logger;

    // Default allowlist for known AI providers
    private static readonly string[] DefaultAllowlist =
    {
        "localhost", "127.0.0.1", "0.0.0.0",
        "api.openai.com", "api.deepseek.com",
        "dashscope.aliyuncs.com", "open.bigmodel.cn",
        "huggingface.co", "cdn-lfs.huggingface.co",
        "cdn-lfs-us-1.huggingface.co", "cdn-lfs-eu-1.huggingface.co",
    };

    public EgressFilterHandler(ILogger<EgressFilterHandler> logger, IEnumerable<string>? additionalAllowlist = null)
    {
        _logger = logger;
        _allowlist = new HashSet<string>(DefaultAllowlist, StringComparer.OrdinalIgnoreCase);
        if (additionalAllowlist is not null)
        {
            foreach (var domain in additionalAllowlist)
                _allowlist.Add(domain);
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null)
        {
            var host = request.RequestUri.Host;

            if (!_allowlist.Contains(host))
            {
                _logger.LogWarning("Egress blocked: {Host} not in allowlist. URL: {Url}", host, request.RequestUri);
                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.Forbidden)
                {
                    Content = new StringContent($"Egress blocked: {host} not in allowlist"),
                });
            }

            _logger.LogDebug("Egress allowed: {Host}", host);
        }

        return base.SendAsync(request, cancellationToken);
    }
}

/// <summary>
/// Resolves secrets from environment variables, with optional Windows DPAPI encryption.
/// On Windows, secrets can be stored encrypted via ProtectedData (DPAPI).
/// On Linux, secrets are read from environment variables only.
/// </summary>
public sealed class SecretResolver
{
    private readonly ILogger<SecretResolver> _logger;

    public SecretResolver(ILogger<SecretResolver> logger) => _logger = logger;

    /// <summary>
    /// Resolve a secret value. Supports ${ENV_VAR} syntax for environment variable expansion.
    /// </summary>
    public string Resolve(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        // Expand ${ENV_VAR} syntax
        if (value.StartsWith("${") && value.EndsWith("}"))
        {
            var envVar = value[2..^1];
            var resolved = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrEmpty(resolved))
            {
                _logger.LogWarning("Environment variable '{Var}' not set; using empty string.", envVar);
                return string.Empty;
            }
            _logger.LogDebug("Resolved secret from env var '{Var}'", envVar);
            return resolved;
        }

        // Check if value looks like a base64-encoded DPAPI blob (Windows only)
        if (OperatingSystem.IsWindows() && value.StartsWith("dpapi:"))
        {
            try
            {
                var base64 = value[5..];
                var encrypted = Convert.FromBase64String(base64);
                var decrypted = System.Security.Cryptography.ProtectedData.Unprotect(
                    encrypted, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                return System.Text.Encoding.UTF8.GetString(decrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt DPAPI secret.");
                return value;
            }
        }

        return value;
    }

    /// <summary>
    /// Encrypt a secret using Windows DPAPI (CurrentUser scope).
    /// Returns "dpapi:base64data" string.
    /// </summary>
    public string Encrypt(string plainValue)
    {
        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("DPAPI encryption not available on non-Windows; returning plain value.");
            return plainValue;
        }

        var encrypted = System.Security.Cryptography.ProtectedData.Protect(
            System.Text.Encoding.UTF8.GetBytes(plainValue),
            null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
        return "dpapi:" + Convert.ToBase64String(encrypted);
    }
}