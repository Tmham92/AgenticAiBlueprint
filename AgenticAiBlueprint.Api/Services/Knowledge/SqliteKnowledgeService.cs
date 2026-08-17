using AgenticAiBlueprint.Api.Persistence;
using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace AgenticAiBlueprint.Api.Services.Knowledge;

/// <summary>
/// SQLite-backed knowledge retrieval service using simple substring matching today.
/// Designed so a future vector-search/embedding implementation can be substituted via DI
/// without changing any consuming agent (eventual RAG support).
/// </summary>
public sealed class SqliteKnowledgeService : IKnowledgeService
{
    private readonly IDbContextFactory<AgenticDbContext> _dbContextFactory;

    public SqliteKnowledgeService(IDbContextFactory<AgenticDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<KnowledgeDocument> AddDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.KnowledgeDocuments.Add(document);
        await db.SaveChangesAsync(cancellationToken);
        return document;
    }

    public async Task<IReadOnlyList<KnowledgeDocument>> SearchAsync(string query, string? domain = null, int maxResults = 5, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var results = db.KnowledgeDocuments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(domain))
        {
            results = results.Where(d => d.Domain == domain);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            results = results.Where(d => d.Title.Contains(query) || d.Content.Contains(query));
        }

        return await results
            .OrderByDescending(d => d.CreatedAt)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}
