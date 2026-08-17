using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Provides retrieval over organizational/domain knowledge documents. Designed for eventual RAG.
/// </summary>
public interface IKnowledgeService
{
    Task<KnowledgeDocument> AddDocumentAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<KnowledgeDocument>> SearchAsync(string query, string? domain = null, int maxResults = 5, CancellationToken cancellationToken = default);
}
