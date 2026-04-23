using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Pgvector;

namespace Archivist.Services;

public class EmbeddingService
{
    private readonly HttpClient _httpClient;

    public EmbeddingService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Ollama");
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var request = new { model = "mxbai-embed-large", prompt = text };
        
        var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        return result?.Embedding ?? Array.Empty<float>();
    }

    private class OllamaResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = null!;
    }

    public async Task<Pgvector.Vector> GetPgvectorAsync(string text)
    {
        var array = await GetEmbeddingAsync(text);
        return new Pgvector.Vector(array);
    }
    
    
    public async Task<Pgvector.Vector> GetVectorAsync(string text)
    {
        var floats = await GetEmbeddingAsync(text); // Твой старый метод возвращающий float[]
        return new Pgvector.Vector(floats);
    }
}