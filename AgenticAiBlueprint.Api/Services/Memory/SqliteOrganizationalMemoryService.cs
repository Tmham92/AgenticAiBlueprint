using AgenticAiBlueprint.Api.Persistence;
using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace AgenticAiBlueprint.Api.Services.Memory;

/// <summary>
/// SQLite-backed implementation of organizational memory, persisting every interaction so
/// agents can retrieve historical outcomes and support organizational learning.
/// </summary>
public sealed class SqliteOrganizationalMemoryService : IOrganizationalMemoryService
{
    private readonly IDbContextFactory<AgenticDbContext> _dbContextFactory;

    public SqliteOrganizationalMemoryService(IDbContextFactory<AgenticDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task RecordInteractionAsync(InteractionRecord record, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.Interactions.Add(record);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InteractionRecord>> GetRecentInteractionsAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Interactions
            .OrderByDescending(i => i.Timestamp)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InteractionRecord>> SearchInteractionsAsync(string query, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Interactions
            .Where(i => i.GoalDescription.Contains(query) || i.Outcome.Contains(query))
            .OrderByDescending(i => i.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
