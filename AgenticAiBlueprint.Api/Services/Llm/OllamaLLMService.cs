using System.Net.Http.Json;
using System.Text.Json.Serialization;
using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;
using Microsoft.Extensions.Options;

namespace AgenticAiBlueprint.Api.Services.Llm;

/// <summary>
/// ILLMService implementation backed by a locally hosted Ollama instance.
/// Falls back to a deterministic heuristic response if Ollama is unreachable,
/// so the platform remains usable/demoable without a live model server.
/// </summary>
public sealed class OllamaLLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly ModelRoutingOptions _modelRouting;
    private readonly ILogger<OllamaLLMService> _logger;

    public OllamaLLMService(HttpClient httpClient, IOptions<ModelRoutingOptions> modelRouting, ILogger<OllamaLLMService> logger)
    {
        _httpClient = httpClient;
        _modelRouting = modelRouting.Value;
        _logger = logger;
    }

    public async Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        var model = _modelRouting.Resolve(request.ModelRole);

        try
        {
            var payload = new OllamaGenerateRequest(
                model,
                CombinePrompt(request),
                false,
                new OllamaOptionsPayload(request.Temperature));

            using var response = await _httpClient.PostAsJsonAsync("/api/generate", payload, cancellationToken);
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: cancellationToken);
            return new LLMResponse
            {
                Content = body?.Response ?? string.Empty,
                ModelUsed = model
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama unreachable at {BaseAddress}; falling back to heuristic response.", _httpClient.BaseAddress);
            return new LLMResponse
            {
                Content = HeuristicFallback.Generate(request),
                ModelUsed = $"{model} (fallback-heuristic)"
            };
        }
    }

    private static string CombinePrompt(LLMRequest request) =>
        string.IsNullOrWhiteSpace(request.SystemPrompt)
            ? request.Prompt
            : $"{request.SystemPrompt}\n\n{request.Prompt}";

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("options")] OllamaOptionsPayload Options);

    private sealed record OllamaOptionsPayload([property: JsonPropertyName("temperature")] double Temperature);

    private sealed record OllamaGenerateResponse([property: JsonPropertyName("response")] string? Response);
}

/// <summary>
/// Simple deterministic fallback used when no LLM provider is reachable, keeping the
/// orchestration loop functional (e.g. in local dev/build/test environments).
/// </summary>
internal static class HeuristicFallback
{
    public static string Generate(LLMRequest request) => request.ModelRole switch
    {
        LLMModelRole.Planner => "PLAN: SingleAgentTask | Reason: heuristic-default | Order: 1",
        LLMModelRole.Coach or LLMModelRole.Insight =>
            "GoalAchieved: true | Confidence: 0.75 | Missing: none | Replan: false | Escalate: false",
        _ => "Acknowledged."
    };
}
