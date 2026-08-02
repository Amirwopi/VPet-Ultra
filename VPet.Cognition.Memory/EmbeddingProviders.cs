using System.Text.Json;
using Microsoft.Extensions.Logging;
using VPet.Cognition.Domain.Abstractions;
using VPet.Cognition.Domain.ValueObjects;

namespace VPet.Cognition.MemoryStore;

/// <summary>
/// Local embedding provider that uses a simple TF-IDF + hashing approach.
/// This is a lightweight, offline-first embedding that doesn't require ONNX runtime
/// or external model files. It produces a fixed-size vector (default 384 dims)
/// suitable for cosine similarity search in SQLite.
/// </summary>
/// <remarks>
/// For production-quality embeddings, use the ONNX-based provider with bge-small.
/// This provider is a fallback when no model file is available.
/// </remarks>
public sealed class LocalEmbeddingProvider : IEmbeddingProvider
{
    private readonly ILogger<LocalEmbeddingProvider> _logger;
    private readonly int _dimensions;
    private readonly Dictionary<string, int> _vocab = new();
    private readonly object _vocabLock = new();

    public int Dimensions => _dimensions;

    public LocalEmbeddingProvider(int dimensions = 384, ILogger<LocalEmbeddingProvider>? logger = null)
    {
        _dimensions = dimensions;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<LocalEmbeddingProvider>.Instance;
    }

    public Task<Embedding> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var vector = ComputeHashingEmbedding(text);
        return Task.FromResult(new Embedding(vector));
    }

    public Task<IReadOnlyList<Embedding>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var results = texts.Select(t => new Embedding(ComputeHashingEmbedding(t))).ToList();
        return Task.FromResult<IReadOnlyList<Embedding>>(results);
    }

    /// <summary>
    /// Compute a hashing-based embedding: tokenize, hash each token to a dimension,
    /// and accumulate weighted counts. Normalized to unit length.
    /// </summary>
    private float[] ComputeHashingEmbedding(string text)
    {
        var vector = new float[_dimensions];
        var tokens = Tokenize(text.ToLowerInvariant());

        foreach (var token in tokens)
        {
            // Hash token to a dimension
            var hash = token.GetHashCode();
            var idx = (uint)hash % (uint)_dimensions;
            var sign = ((hash >> 31) & 1) == 1 ? 1f : -1f;

            // Weight by inverse document frequency approximation
            // (shorter tokens are more common → lower weight)
            var weight = 1f / Math.Max(1, token.Length);

            vector[idx] += sign * weight;
        }

        // L2 normalize
        var norm = Math.Sqrt(vector.Sum(v => v * v));
        if (norm > 0)
        {
            for (int i = 0; i < _dimensions; i++)
                vector[i] = (float)(vector[i] / norm);
        }

        return vector;
    }

    /// <summary>
    /// Simple tokenizer: split on non-alphanumeric, filter short tokens.
    /// </summary>
    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var span = text.AsSpan();
        int start = 0;

        for (int i = 0; i <= span.Length; i++)
        {
            if (i == span.Length || !char.IsLetterOrDigit(span[i]))
            {
                if (i > start)
                {
                    var token = span[start..i].ToString();
                    if (token.Length >= 2)
                        tokens.Add(token);
                }
                start = i + 1;
            }
        }

        return tokens;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>
/// Online embedding provider that calls an OpenAI-compatible embeddings endpoint
/// (e.g. Ollama /api/embeddings, OpenAI /v1/embeddings).
/// </summary>
public sealed class OnlineEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string _model;
    private readonly string _apiKey;
    private readonly ILogger<OnlineEmbeddingProvider> _logger;

    public int Dimensions { get; }

    public OnlineEmbeddingProvider(
        HttpClient httpClient,
        string endpoint,
        string model,
        string apiKey,
        int dimensions,
        ILogger<OnlineEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _endpoint = endpoint.TrimEnd('/');
        _model = model;
        _apiKey = apiKey;
        Dimensions = dimensions;
        _logger = logger;
    }

    public async Task<Embedding> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var batch = await EmbedBatchAsync(new[] { text }, cancellationToken);
        return batch[0];
    }

    public async Task<IReadOnlyList<Embedding>> EmbedBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default)
    {
        var textList = texts.ToList();
        if (textList.Count == 0) return Array.Empty<Embedding>();

        var requestBody = JsonSerializer.Serialize(new
        {
            model = _model,
            input = textList,
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"{_endpoint}/embeddings")
        {
            Content = new StringContent(requestBody, System.Text.Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrEmpty(_apiKey))
            request.Headers.Add("Authorization", $"Bearer {_apiKey}");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        var embeddings = new List<Embedding>();
        foreach (var item in doc.RootElement.GetProperty("data").EnumerateArray())
        {
            var vec = item.GetProperty("embedding").Deserialize<float[]>() ?? Array.Empty<float>();
            embeddings.Add(new Embedding(vec));
        }

        return embeddings;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}