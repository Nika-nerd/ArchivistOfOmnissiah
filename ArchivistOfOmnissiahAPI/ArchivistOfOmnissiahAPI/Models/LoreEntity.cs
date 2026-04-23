using Pgvector;

namespace Archivist.Models;

public class LoreEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public Vector? Embedding { get; set; }
    public string Source { get; set; } = string.Empty;
    public string Era { get; set; } = "M40"; // По умолчанию
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ContentHash { get; set; } = string.Empty;
}