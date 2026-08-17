using System.Net.Http.Json;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Web.Services;

/// <summary>
/// Typed HttpClient wrapper for calling the AgenticAiBlueprint.Api endpoints from the Blazor UI.
/// </summary>
public sealed class AgenticApiClient
{
    private readonly HttpClient _httpClient;

    public AgenticApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AgentExecutionResponse?> ExecuteGoalAsync(AgentGoal goal, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/agent/execute", goal, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentExecutionResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ControlTowerChatResponse?> ChatAsync(ControlTowerChatRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("/api/controltower/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ControlTowerChatResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ControlTowerDashboard?> GetDashboardAsync(CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<ControlTowerDashboard>("/api/controltower", cancellationToken);

    public async Task<List<InteractionRecord>?> GetInteractionsAsync(CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<List<InteractionRecord>>("/api/interactions", cancellationToken);

    public async Task<List<ExecutiveRecommendation>?> GetRecommendationsAsync(CancellationToken cancellationToken = default) =>
        await _httpClient.GetFromJsonAsync<List<ExecutiveRecommendation>>("/api/recommendations", cancellationToken);
}
