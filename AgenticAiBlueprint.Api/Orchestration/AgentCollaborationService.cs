using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Api.Orchestration;

/// <summary>
/// Aggregates contributions from collaborating agents into consensus/conflict groupings,
/// enabling agents to challenge each other's conclusions transparently.
/// </summary>
public sealed class AgentCollaborationService : IAgentCollaborationService
{
    public AgentCollaborationResult Aggregate(IReadOnlyCollection<AgentContribution> contributions)
    {
        var challenges = contributions.Where(c => c.Type == ContributionType.Challenge).ToList();
        var challengedIds = challenges
            .Where(c => c.RelatedContributionId is not null)
            .Select(c => c.RelatedContributionId!)
            .ToHashSet();

        var consensus = contributions
            .Where(c => c.Type is ContributionType.Finding or ContributionType.Recommendation && !challengedIds.Contains(c.Id))
            .Select(c => $"[{c.AgentName}] {c.Content}")
            .ToList();

        var conflicts = challenges
            .Select(c => $"[{c.AgentName}] challenges: {c.Content}")
            .ToList();

        var risks = contributions.Where(c => c.Type == ContributionType.Risk).ToList();

        var summary = $"{consensus.Count} consensus item(s), {conflicts.Count} conflict(s), {risks.Count} risk(s) flagged.";

        return new AgentCollaborationResult
        {
            Contributions = contributions.ToList(),
            Consensus = consensus,
            Conflicts = conflicts,
            Summary = summary
        };
    }
}
