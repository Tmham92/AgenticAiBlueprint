using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Api.Orchestration;

/// <summary>
/// LLM-driven reflection agent that evaluates whether a goal has been achieved after execution.
/// Uses a lightweight structured text response from the LLM with heuristic parsing/fallback.
/// </summary>
public sealed class ReflectionAgent : IReflectionAgent
{
    private readonly ILLMService _llmService;

    public ReflectionAgent(ILLMService llmService)
    {
        _llmService = llmService;
    }

    public async Task<ReflectionResult> ReflectAsync(AgentGoal goal, AgentPlan plan, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        var executionSummary = string.Join("\n", context.History.Steps
            .Where(s => s.Iteration == context.CurrentIteration)
            .Select(s => $"- [{s.AgentName}] {s.Action}: {(s.Success ? "succeeded" : "failed")} {s.Details}"));

        var prompt =
            $"Goal: {goal.Description}\n\n" +
            $"Execution results this iteration:\n{executionSummary}\n\n" +
            "Evaluate: Was the goal achieved? What information is missing? What is your confidence (0-1)? " +
            "Is replanning needed? Is human escalation needed? " +
            "Respond in the form: GoalAchieved: true|false | Confidence: 0.0-1.0 | Missing: comma,separated,list|none | Replan: true|false | Escalate: true|false";

        var response = await _llmService.CompleteAsync(new LLMRequest
        {
            Prompt = prompt,
            SystemPrompt = "You are a reflection agent evaluating whether an agentic execution satisfied its goal.",
            ModelRole = LLMModelRole.Coach
        }, cancellationToken);

        return Parse(response.Content);
    }

    private static ReflectionResult Parse(string content)
    {
        bool goalAchieved = ExtractBool(content, "GoalAchieved") ?? DefaultAchieved(content);
        double confidence = ExtractDouble(content, "Confidence") ?? 0.6;
        bool replan = ExtractBool(content, "Replan") ?? !goalAchieved;
        bool escalate = ExtractBool(content, "Escalate") ?? confidence < 0.4;
        var missing = ExtractList(content, "Missing");

        return new ReflectionResult
        {
            GoalAchieved = goalAchieved,
            ConfidenceLevel = confidence,
            ReplanningNeeded = replan && !goalAchieved,
            EscalationNeeded = escalate,
            MissingInformation = missing,
            Rationale = content
        };
    }

    private static bool DefaultAchieved(string content) =>
        content.Contains("achieved", StringComparison.OrdinalIgnoreCase) && !content.Contains("not achieved", StringComparison.OrdinalIgnoreCase);

    private static bool? ExtractBool(string content, string key)
    {
        var value = ExtractValue(content, key);
        if (value is null)
        {
            return null;
        }

        return value.Trim().StartsWith("true", StringComparison.OrdinalIgnoreCase);
    }

    private static double? ExtractDouble(string content, string key)
    {
        var value = ExtractValue(content, key);
        return double.TryParse(value, out var result) ? result : null;
    }

    private static List<string> ExtractList(string content, string key)
    {
        var value = ExtractValue(content, key);
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string>();
        }

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
    }

    private static string? ExtractValue(string content, string key)
    {
        var idx = content.IndexOf(key + ":", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return null;
        }

        var start = idx + key.Length + 1;
        var end = content.IndexOf('|', start);
        var value = end > start ? content[start..end] : content[start..];
        return value.Trim();
    }
}
