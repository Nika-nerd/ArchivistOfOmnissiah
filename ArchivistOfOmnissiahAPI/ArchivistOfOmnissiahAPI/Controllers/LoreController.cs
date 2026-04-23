using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Archivist.Data;
using Archivist.Models;
using Archivist.Services;
using Pgvector.EntityFrameworkCore;


namespace Archivist.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoreController : ControllerBase
{
    private readonly ArchivistDbContext _context;

    public LoreController(ArchivistDbContext context)
    {
        _context = context;
    }

    [HttpGet("ask")]
public async Task<IActionResult> Ask(
    [FromServices] EmbeddingService embedding, 
    [FromServices] ChatService chat, 
    [FromQuery] string question,
    [FromQuery] string sessionId = "default-session")
{
    // 1. ВАЛИДАЦИЯ: Проверяем входящий запрос
    if (string.IsNullOrWhiteSpace(question) || question.Length < 3)
    {
        return BadRequest("The query is too brief or empty. Input more data to initiate the machine spirit.");
    }

    if (question.Length > 2000)
    {
        return BadRequest("The data-stream is too dense. Condense your query to under 2000 units.");
    }

    try 
    {
        // 2. ОЧИСТКА СТАРОЙ ИСТОРИИ 
        var sessionMessagesCount = await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .CountAsync();

        if (sessionMessagesCount > 50)
        {
            var oldestMessages = _context.ChatMessages
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .Take(10);

            _context.ChatMessages.RemoveRange(oldestMessages);
            await _context.SaveChangesAsync();
        }

        // 3. RAG: Поиск релевантного лора в базе данных
        var vector = await embedding.GetVectorAsync(question);

        // Ищем 3 самых похожих фрагмента по косинусному расстоянию
        var loreChunks = await _context.LoreEntries
            .OrderBy(e => e.Embedding!.CosineDistance(vector))
            .Take(3)
            .Select(e => e.Content)
            .ToListAsync();

        // Объединяем найденный лор в одну строку
        var context = string.Join("\n\n", loreChunks);

        // 4. ГЕНЕРАЦИЯ: Отправляем контекст и вопрос в ChatService
        var answer = await chat.AskQuestionAsync(context, question, sessionId);

        return Ok(new { sessionId, question, answer });
    }
    catch (Exception ex)
    {
        // 5. ОБРАБОТКА ОШИБОК
        return StatusCode(500, new 
        { 
            error = "Machine Spirit Disturbance", 
            message = "A critical failure occurred. Log entry: " + ex.Message 
        });
    }
}

    [HttpPost("ingest-wiki")]
    public async Task<IActionResult> IngestWiki(
        [FromQuery] string topic,
        [FromServices] IBackgroundTaskQueue queue,
        [FromServices] IServiceScopeFactory scopeFactory)
    {
        
        if (string.IsNullOrWhiteSpace(topic)) return BadRequest("Topic is empty.");

        
        await queue.QueueBackgroundWorkItemAsync(async token =>
        {
            using var scope = scopeFactory.CreateScope();
            var wiki = scope.ServiceProvider.GetRequiredService<WikiService>();
            var embedding = scope.ServiceProvider.GetRequiredService<EmbeddingService>();
            var db = scope.ServiceProvider.GetRequiredService<ArchivistDbContext>();

            var chunks = await wiki.GetArticleContentAsync(topic);
        
            foreach (var chunk in chunks)
            {
                var hash = wiki.ComputeHash(chunk);
                if (!await db.LoreEntries.AnyAsync(e => e.ContentHash == hash, token))
                {
                    var vector = await embedding.GetVectorAsync(chunk);
                    db.LoreEntries.Add(new LoreEntry
                    {
                        Content = chunk,
                        ContentHash = hash,
                        Source = $"Wikipedia: {topic}",
                        Embedding = vector
                    });
                }
            }
            await db.SaveChangesAsync(token);
        });

        return Accepted(new { message = $"Task initiated. The archives are being updated with data on: {topic}" });
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllLore()
    {
        _context.LoreEntries.RemoveRange(_context.LoreEntries);
        await _context.SaveChangesAsync();
        return Ok(new { message = "The Archives have been purged. The Great Library is empty." });
    }
}