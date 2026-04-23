namespace Archivist.Models;

public class ChatMessage
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = "default-session";
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}