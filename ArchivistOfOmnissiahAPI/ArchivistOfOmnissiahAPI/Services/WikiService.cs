using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace Archivist.Services;

public class WikiService
{
    private readonly HttpClient _http;

    public WikiService(IHttpClientFactory httpClientFactory)
    {
        _http = httpClientFactory.CreateClient();
    }

    public string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }

    public async Task<List<string>> GetArticleContentAsync(string topic)
    {
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _http.DefaultRequestHeaders.Add("User-Agent", "ArchivistBot/1.0");
        }

        // 1. Поиск статьи
        var searchUrl = $"https://en.wikipedia.org/w/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(topic)}&format=json";
        var searchResponse = await _http.GetFromJsonAsync<JsonElement>(searchUrl);
        
        var queryElement = searchResponse.GetProperty("query");
        var searchResults = queryElement.GetProperty("search");
        
        if (searchResults.GetArrayLength() == 0) return new List<string>();

        var title = searchResults[0].GetProperty("title").GetString();
        if (string.IsNullOrEmpty(title)) return new List<string>();

        // 2. Получение чистого текста (plain text)
        var contentUrl = $"https://en.wikipedia.org/w/api.php?action=query&prop=extracts&explaintext=1&titles={Uri.EscapeDataString(title)}&format=json";
        var contentResponse = await _http.GetFromJsonAsync<JsonElement>(contentUrl);
        
        var pages = contentResponse.GetProperty("query").GetProperty("pages");
        var pageProperty = pages.EnumerateObject().FirstOrDefault();
        
        if (pageProperty.Value.ValueKind == JsonValueKind.Undefined) return new List<string>();

        var text = pageProperty.Value.GetProperty("extract").GetString();

        if (string.IsNullOrEmpty(text)) return new List<string>();

        // Очистка текста от лишних переносов строк и специфических символов Википедии
        text = CleanWikiText(text);

        // Нарезка: оптимальный размер для эмбеддингов — около 800-1000 символов
        return ChunkText(text, 1000, 150);
    }

    private string CleanWikiText(string text)
    {
        // Убираем множественные переносы строк и лишние пробелы
        var cleaned = Regex.Replace(text, @"\n{2,}", "\n");
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ");
        return cleaned.Trim();
    }

    private List<string> ChunkText(string text, int chunkSize, int overlap)
    {
        var chunks = new List<string>();
        int start = 0;

        while (start < text.Length)
        {
            int end = Math.Min(start + chunkSize, text.Length);
            
            // Если мы не дошли до конца текста, ищем ближайшую точку, 
            // чтобы закончить кусок на конце предложения.
            if (end < text.Length)
            {
                // Ищем точку в последних 200 символах текущего чанка
                int lastDot = text.LastIndexOf('.', end, Math.Min(200, end - start));
                if (lastDot > start)
                {
                    end = lastDot + 1; // Захватываем точку
                }
            }

            string chunk = text.Substring(start, end - start).Trim();
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                chunks.Add(chunk);
            }

            // Сдвигаемся вперед с учетом перекрытия (overlap)
            start = end - overlap;
            
            // Страховка от бесконечного цикла, если overlap больше чанка
            if (start >= end) start = end; 
            if (end == text.Length) break;
        }

        return chunks;
    }
}