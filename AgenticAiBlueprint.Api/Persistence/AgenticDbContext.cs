using AgenticAiBlueprint.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace AgenticAiBlueprint.Api.Persistence;

/// <summary>
/// EF Core DbContext backing the platform's SQLite-based organizational memory and knowledge stores.
/// </summary>
public sealed class AgenticDbContext : DbContext
{
    public AgenticDbContext(DbContextOptions<AgenticDbContext> options) : base(options)
    {
    }

    public DbSet<InteractionRecord> Interactions => Set<InteractionRecord>();

    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InteractionRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GoalDescription).IsRequired();
        });

        modelBuilder.Entity<KnowledgeDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Tags)
                .HasConversion(
                    tags => string.Join('|', tags),
                    value => value.Length == 0 ? Array.Empty<string>() : value.Split('|', StringSplitOptions.RemoveEmptyEntries));
        });
    }
}
