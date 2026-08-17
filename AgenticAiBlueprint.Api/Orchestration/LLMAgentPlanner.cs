using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;
using Microsoft.Extensions.Logging;

namespace AgenticAiBlueprint.Api.Orchestration;

/// <summary>
/// Default LLM-driven planner. Asks the LLM which registered agents should run, in what order,
/// and why. Falls back to a simple heuristic (run every registered agent once, in registration
/// order) if the LLM response cannot be parsed, keeping the platform functional offline.
/// </summary>
public sealed class LLMAgentPlanner : IAgentPlanner
{
    private readonly ILLMService _llmService;
    private readonly IEnumerable<IAgent> _agents;
    private readonly ILogger<LLMAgentPlanner> _logger;

    public LLMAgentPlanner(ILLMService llmService, IEnumerable<IAgent> agents, ILogger<LLMAgentPlanner> logger)
    {
        _llmService = llmService;
        _agents = agents;
        _logger = logger;
    }

    public async Task<AgentPlan> CreatePlanAsync(AgentGoal goal, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        var agentCatalog = string.Join("\n", _agents.Select(a => $"- {a.Name}: {a.Description}"));

        var prompt =
            $"Goal: {goal.Description}\n" +
            $"Domain: {goal.Domain ?? "unspecified"}\n\n" +
            $"Available agents:\n{agentCatalog}\n\n" +
            "Respond with one agent name per line to invoke, in execution order, prefixed by its order number, e.g. '1. AgentName: reason'.";

        var response = await _llmService.CompleteAsync(new LLMRequest
        {
            Prompt = prompt,
            SystemPrompt = "You are a planning agent for a domain-agnostic agentic AI platform. Select the minimal set of agents required.",
            ModelRole = LLMModelRole.Planner
        }, cancellationToken);

        var tasks = ParsePlanResponse(response.Content);

        if (tasks.Count == 0)
        {
            _logger.LogInformation("Planner could not parse LLM output; falling back to running all registered agents once.");
            tasks = _agents
                .Select((a, i) => new PlannedTask { AgentName = a.Name, Reason = "Fallback: heuristic default plan", Order = i })
                .ToList();
        }

        return new AgentPlan
        {
            GoalId = goal.Id,
            Tasks = tasks,
            Rationale = response.Content,
            Iteration = context.CurrentIteration
        };
    }

    private List<PlannedTask> ParsePlanResponse(string content)
    {
        var tasks = new List<PlannedTask>();
        var knownNames = _agents.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var matchedName = knownNames.FirstOrDefault(name => line.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (matchedName is null)
            {
                continue;
            }

            var reason = line.Contains(':') ? line[(line.IndexOf(':') + 1)..].Trim() : string.Empty;

            tasks.Add(new PlannedTask
            {
                AgentName = matchedName,
                Reason = reason,
                Order = tasks.Count
            });
        }

        return tasks;
    }
}
