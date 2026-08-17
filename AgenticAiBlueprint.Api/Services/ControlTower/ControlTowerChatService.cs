using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Api.Services.ControlTower;

/// <summary>
/// Aggregates agent findings, risks, recommendations, and historical trends for executive
/// visibility, and answers ad-hoc Control Tower chat questions grounded in that aggregated data.
/// </summary>
public sealed class ControlTowerChatService : IControlTowerChatService
{
    private readonly IOrganizationalMemoryService _memoryService;
    private readonly IExecutiveRecommendationAgent _recommendationAgent;
    private readonly ILLMService _llmService;

    public ControlTowerChatService(
        IOrganizationalMemoryService memoryService,
        IExecutiveRecommendationAgent recommendationAgent,
        ILLMService llmService)
    {
        _memoryService = memoryService;
        _recommendationAgent = recommendationAgent;
        _llmService = llmService;
    }

    public async Task<ControlTowerDashboard> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var interactions = await _memoryService.GetRecentInteractionsAsync(50, cancellationToken);
        var recommendations = await _recommendationAgent.GenerateRecommendationsAsync(cancellationToken);

        var trends = interactions
            .GroupBy(i => i.Domain ?? "General")
            .ToDictionary(g => g.Key, g => g.Count());

        return new ControlTowerDashboard
        {
            RecentInteractions = interactions.Take(10).ToList(),
            Recommendations = recommendations.ToList(),
            HistoricalTrends = trends
        };
    }

    public async Task<ControlTowerChatResponse> ChatAsync(ControlTowerChatRequest request, CancellationToken cancellationToken = default)
    {
        var dashboard = await GetDashboardAsync(cancellationToken);

        var context =
            $"Recent interactions: {dashboard.RecentInteractions.Count}\n" +
            $"Trends: {string.Join(", ", dashboard.HistoricalTrends.Select(t => $"{t.Key}={t.Value}"))}\n" +
            $"Recommendations: {string.Join("; ", dashboard.Recommendations.Select(r => r.Recommendation))}";

        var response = await _llmService.CompleteAsync(new LLMRequest
        {
            Prompt = $"Control Tower context:\n{context}\n\nExecutive question: {request.Message}",
            SystemPrompt = "You are the Control Tower assistant, answering executive questions using aggregated organizational data.",
            ModelRole = LLMModelRole.Insight
        }, cancellationToken);

        return new ControlTowerChatResponse { Reply = response.Content };
    }
}
