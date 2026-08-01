namespace VPet.Cognition.Domain.Errors;

/// <summary>
/// Errors raised by AI providers (LLM, STT, TTS, embedding).
/// </summary>
public sealed record ProviderError(string Code, string Message) : Error(Code, Message)
{
    public static ProviderError Timeout(string provider) =>
        new("LLM_TIMEOUT", $"Provider '{provider}' did not respond within the timeout.");

    public static ProviderError ServerError(string provider, int status) =>
        new("LLM_5XX", $"Provider '{provider}' returned server error {status}.");

    public static ProviderError Deserialize(string provider, string detail) =>
        new("LLM_DESERIALIZE", $"Provider '{provider}' returned a malformed response: {detail}");

    public static ProviderError ContentFilter(string provider) =>
        new("LLM_CONTENT_FILTER", $"Provider '{provider}' blocked the response via content filter.");

    public static ProviderError Unavailable(string provider) =>
        new("PROVIDER_UNAVAILABLE", $"Provider '{provider}' is not available.");

    public static ProviderError Network(string provider, string detail) =>
        new("PROVIDER_NETWORK", $"Network error contacting '{provider}': {detail}");
}