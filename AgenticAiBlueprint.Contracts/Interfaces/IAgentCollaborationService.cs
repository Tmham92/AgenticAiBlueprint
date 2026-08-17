using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Aggregates contributions from multiple agents into a collaboration result
/// (consensus, conflicts, and a synthesized summary).
/// </summary>
public interface IAgentCollaborationService
{
    AgentCollaborationResult Aggregate(IReadOnlyCollection<AgentContribution> contributions);
}

/// <summary>
/// Generates executive-level recommendations (problem, recommendation, expected impact, priority)
/// from aggregated agent findings and historical trends.
/// </summary>
public interface IExecutiveRecommendationAgent
{
    Task<IReadOnlyList<ExecutiveRecommendation>> GenerateRecommendationsAsync(CancellationToken cancellationToken = default);
}
