using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Api.Services.ControlTower;

/// <summary>
/// Synthesizes executive-level recommendations (problem, recommendation, expected impact, priority)
/// from recent organizational interactions.
/// </summary>
public sealed class ExecutiveRecommendationAgent : IExecutiveRecommendationAgent
{
    private readonly IOrganizationalMemoryService _memoryService;
    private readonly ILLMService _llmService;

    public ExecutiveRecommendationAgent(IOrganizationalMemoryService memoryService, ILLMService llmService)
    {
        _memoryService = memoryService;
        _llmService = llmService;
    }

    public async Task<IReadOnlyList<ExecutiveRecommendation>> GenerateRecommendationsAsync(CancellationToken cancellationToken = default)
    {
        var interactions = await _memoryService.GetRecentInteractionsAsync(20, cancellationToken);

        if (interactions.Count == 0)
        {
            return Array.Empty<ExecutiveRecommendation>();
        }

        var lowConfidence = interactions.Where(i => i.Confidence < 0.6).ToList();
        var recommendations = new List<ExecutiveRecommendation>();

        if (lowConfidence.Count > 0)
        {
            var response = await _llmService.CompleteAsync(new LLMRequest
            {
                Prompt = $"Recent low-confidence interactions:\n" +
                         string.Join("\n", lowConfidence.Select(i => $"- {i.GoalDescription} (confidence {i.Confidence:F2})")) +
                         "\n\nSummarize the top systemic problem and recommend one concrete executive action.",
                SystemPrompt = "You are an executive insights agent generating concise, actionable recommendations.",
                ModelRole = LLMModelRole.Insight
            }, cancellationToken);

            recommendations.Add(new ExecutiveRecommendation
            {
                Problem = $"{lowConfidence.Count} recent interaction(s) completed with low confidence.",
                Recommendation = response.Content,
                ExpectedImpact = "Improved reliability and reduced rework across future goal executions.",
                Priority = lowConfidence.Count > 5 ? RecommendationPriority.High : RecommendationPriority.Medium
            });
        }

        return recommendations;
    }
}
