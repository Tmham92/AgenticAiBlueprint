using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Persists and retrieves historical interactions, enabling organizational learning across goal executions.
/// </summary>
public interface IOrganizationalMemoryService
{
    Task RecordInteractionAsync(InteractionRecord record, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InteractionRecord>> GetRecentInteractionsAsync(int count = 20, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InteractionRecord>> SearchInteractionsAsync(string query, CancellationToken cancellationToken = default);
}
