using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;

namespace AccessManager.UI.Services;

public class OpenAiEmbeddingService : IEmbeddingService
{
    private const string DefaultModel = "text-embedding-3-small";
    private const int MaxInputTokens = 8191; // model limit; metin uzunsa keseriz

    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiEmbeddingService> _logger;

    public OpenAiEmbeddingService(IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<OpenAiEmbeddingService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<float[]?> GetEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var results = await GetEmbeddingsAsync(new[] { text.Trim() }, cancellationToken);
        return results.Count > 0 ? results[0] : null;
    }

    public async Task<IReadOnlyList<float[]>> GetEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts == null || texts.Count == 0) return Array.Empty<float[]>();

        var apiKey = _config["OpenAI:ApiKey"]?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("OpenAI:ApiKey tanımlı değil; embedding atlanıyor.");
            return Array.Empty<float[]>();
        }

        var model = _config["OpenAI:EmbeddingModel"] ?? DefaultModel;
        var trimmed = texts.Select(t => TruncateForEmbedding(t)).ToList();

        var payload = new JsonObject
        {
            ["model"] = model,
            ["input"] = trimmed.Count == 1 ? (JsonNode?)trimmed[0] : new JsonArray(trimmed.Select(t => (JsonNode?)t).ToArray())
        };

        var client = _httpClientFactory.CreateClient("OpenAI");
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OpenAI Embeddings API hatası. Status: {StatusCode}, Body: {Body}", response.StatusCode, body.Length > 200 ? body[..200] + "..." : body);
            return Array.Empty<float[]>();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseJson);
        var data = doc.RootElement.GetProperty("data");
        var list = new List<float[]>();
        foreach (var item in data.EnumerateArray())
        {
            var emb = item.GetProperty("embedding");
            var arr = new float[emb.GetArrayLength()];
            for (var i = 0; i < arr.Length; i++)
                arr[i] = (float)emb[i].GetDouble();
            list.Add(arr);
        }
        return list;
    }

    private static string TruncateForEmbedding(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        // Kabaca 4 char ~ 1 token; 8191 token ~ 32K karakter
        const int maxChars = 30000;
        return text.Length <= maxChars ? text : text[..maxChars] + "\n... (kesildi)";
    }
}
