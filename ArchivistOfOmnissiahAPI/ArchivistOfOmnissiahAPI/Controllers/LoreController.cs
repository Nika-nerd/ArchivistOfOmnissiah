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
        
        if (string.IsNullOrWhiteSpace(question) || question.Length < 3)
        {
            return BadRequest("The query is too brief or empty.");
        }

        
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

        var vector = await embedding.GetVectorAsync(question);

        var loreChunks = await _context.LoreEntries
            .OrderBy(e => e.Embedding!.CosineDistance(vector))
            .Take(3)
            .Select(e => e.Content)
            .ToListAsync();

        var context = string.Join("\n\n", loreChunks);
        var answer = await chat.AskQuestionAsync(context, question, sessionId);

        return Ok(new { sessionId, question, answer });
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