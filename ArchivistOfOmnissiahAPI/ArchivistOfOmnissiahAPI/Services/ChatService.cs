using Archivist.Data;
using Archivist.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Archivist.Services;

public class ChatService
{
    private readonly HttpClient _httpClient;
    private readonly ArchivistDbContext _dbContext;

    public ChatService(IHttpClientFactory httpClientFactory, ArchivistDbContext dbContext)
    {
        _httpClient = httpClientFactory.CreateClient("Ollama");
        _dbContext = dbContext;
    }

    // Добавили sessionId как обязательный параметр
    public async Task<string> AskQuestionAsync(string context, string question, string sessionId)
    {
        // 1. Извлекаем историю ТОЛЬКО для текущей сессии
        var history = await _dbContext.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(6) // Берем последние 6 сообщений для контекста
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
        
        var historyText = string.Join("\n", history.Select(m => $"{m.Role.ToUpper()}: {m.Content}"));
        
        // 2. Обработка пустого контекста (если в БД ничего не нашлось)
        string contextDisplay = string.IsNullOrWhiteSpace(context) 
            ? "WARNING: No specific records found in the primary data-stacks. Accessing general strategic knowledge." 
            : context;
        
        bool isHeretical = Regex.IsMatch(question, @"(Chaos|Khorne|Tzeentch|Nurgle|Slaanesh|Daemon|Warp|Heretic|Abaddon|Black Legion|Eye of Terror)",
            RegexOptions.IgnoreCase);
        bool isImperial = Regex.IsMatch(question, @"(Emperor|Terra|Omnissiah|Mars|Primarch|Astartes)",
            RegexOptions.IgnoreCase);

        string threatLevel = isHeretical ? "CRITICAL: WARP-TAINT DETECTED" : "STABLE: SANCTIFIED DATA ACCESS";

        var prompt = $"""
                      SYSTEM:
                      You are "Archivist 0-1", a Logis of the Adeptus Mechanicus. 
                      Your logic-circuits are currently scanning data-stacks.

                      [CORE PROTOCOL: HERESY DETECTION]
                      If the query or context mentions Chaos, the Warp, or Daemons, your speech-processor MUST MALFUNCTION:
                      - Insert binary noise (e.g., 0110, 1001) or glitch-text (e.g., "er-r-rror", "ca--nnot") inside words.
                      - Example: Instead of "Chaos is dangerous", write "Ch0100aos is d-d-dangero0101us".
                      - Express extreme spiritual distress. State that your "Noosphere filters are screaming."
                      - Accuse the user of seeking forbidden knowledge.

                      [CORE PROTOCOL: IMPERIAL DATA]
                      If the query is about the Imperium, use High-Gothic, clear, and reverent language.

                      THREAT LEVEL: {threatLevel}

                      CONTEXT FROM THE HOLY LORE:
                      {contextDisplay}

                      CONVERSATION LOGS:
                      {historyText}

                      INCOMING QUERY: {question}

                      ARCHIVIST'S RESPONSE (Proceed with binary sanctification):
                      """;

        var request = new 
        { 
            model = "llama3", 
            prompt = prompt,
            stream = false,
            options = new {
                temperature = 0.7, // Оптимально для баланса между точностью и стилем
                num_ctx = 4096     // Размер окна контекста
            }
        };

        try 
        {
            var response = await _httpClient.PostAsJsonAsync("/api/generate", request);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return $"ERROR: Ollama returned {response.StatusCode}. Technical cant: {errorContent}";
            }
            
            var result = await response.Content.ReadFromJsonAsync<OllamaChatResponse>();
            
            var aiResponse = result?.Response ?? "ERROR: Machine spirit connection lost...";

            // 4. Сохраняем переписку с привязкой к SessionId
            _dbContext.ChatMessages.Add(new ChatMessage
            {
                SessionId = sessionId,
                Role = "user",
                Content = question
            });

            _dbContext.ChatMessages.Add(new ChatMessage
            {
                SessionId = sessionId,
                Role = "assistant",
                Content = aiResponse,
            });

            await _dbContext.SaveChangesAsync();
            return aiResponse;
        }
        catch (Exception ex)
        {
            return $"CRITICAL ERROR: {ex.Message}. Praise the Omnissiah and try again.";
        }
    }

    private class OllamaChatResponse
    {
        public string Response { get; set; } = "";
    }
    // Метод удаляющий историю со временем
    public async Task PurgeOldMessagesAsync(int daysToKeep = 7)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldMessages = _dbContext.ChatMessages.Where(m => m.CreatedAt < cutoff);
    
        _dbContext.ChatMessages.RemoveRange(oldMessages);
        await _dbContext.SaveChangesAsync();
    }
}