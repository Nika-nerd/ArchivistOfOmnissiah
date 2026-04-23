using Microsoft.EntityFrameworkCore;
using Pgvector;
using Archivist.Models; // Проверь, чтобы папка Models и этот Namespace совпадали

namespace Archivist.Data;

public class ArchivistDbContext : DbContext
{
    public ArchivistDbContext(DbContextOptions<ArchivistDbContext> options) 
        : base(options)
    {
    }

    // Твоя основная таблица с лором
    public DbSet<LoreEntry> LoreEntries => Set<LoreEntry>();
    
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Включаем поддержку расширения pgvector
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<LoreEntry>(entity =>
        {
            entity.ToTable("lore_entries");

            entity.HasKey(e => e.Id);

            // Текст фрагмента лора
            entity.Property(e => e.Content)
                .IsRequired();

            // Векторная колонка. 
            // 1024 — размерность для модели mxbai-embed-large (Ollama).
            // Если будешь использовать другую модель, поменяем число.
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(1024)"); 

            // Индекс для быстрого поиска по векторам (HNSW — самый быстрый для RAG)
            entity.HasIndex(e => e.Embedding)
                .HasMethod("hnsw")
                .HasOperators("vector_cosine_ops");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("chat_messages");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.SessionId);
            entity.Property(e => e.Role)
                .IsRequired();
            entity.Property(e => e.Content)
                .IsRequired();
        });

    }
}