using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AgenticAiBlueprint.Api.Orchestration;

/// <summary>
/// Default, domain-agnostic implementation of the agent orchestration loop:
/// Goal -> Plan -> Execute Agents -> Reflect -> Replan -> Escalate -> Final Response.
/// Supports multiple iterations, dynamic replanning, conditional execution, and agent collaboration.
/// </summary>
public sealed class DefaultAgentOrchestrator : IAgentOrchestrator
{
    private readonly IAgentPlanner _planner;
    private readonly IEnumerable<IAgent> _agents;
    private readonly IReflectionAgent _reflectionAgent;
    private readonly IReplanningService _replanningService;
    private readonly IEscalationAgent _escalationAgent;
    private readonly IAgentCollaborationService _collaborationService;
    private readonly IOrganizationalMemoryService _memoryService;
    private readonly OrchestrationOptions _options;
    private readonly ILogger<DefaultAgentOrchestrator> _logger;

    public DefaultAgentOrchestrator(
        IAgentPlanner planner,
        IEnumerable<IAgent> agents,
        IReflectionAgent reflectionAgent,
        IReplanningService replanningService,
        IEscalationAgent escalationAgent,
        IAgentCollaborationService collaborationService,
        IOrganizationalMemoryService memoryService,
        IOptions<OrchestrationOptions> options,
        ILogger<DefaultAgentOrchestrator> logger)
    {
        _planner = planner;
        _agents = agents;
        _reflectionAgent = reflectionAgent;
        _replanningService = replanningService;
        _escalationAgent = escalationAgent;
        _collaborationService = collaborationService;
        _memoryService = memoryService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AgentExecutionResponse> ExecuteGoalAsync(AgentGoal goal, CancellationToken cancellationToken = default)
    {
        var context = new AgentExecutionContext
        {
            Goal = goal,
            MaxIterations = _options.MaxIterations
        };
        context.SetMemory("goal", goal.Description);

        var plan = await _planner.CreatePlanAsync(goal, context, cancellationToken);
        ReflectionResult? lastReflection = null;
        EscalationDecision? escalation = null;

        for (context.CurrentIteration = 1; context.CurrentIteration <= context.MaxIterations; context.CurrentIteration++)
        {
            await ExecutePlanAsync(plan, context, cancellationToken);

            lastReflection = await _reflectionAgent.ReflectAsync(goal, plan, context, cancellationToken);

            escalation = await _escalationAgent.EvaluateAsync(goal, lastReflection, context, cancellationToken);

            if (lastReflection.GoalAchieved || escalation.RequiresHumanAction || !lastReflection.ReplanningNeeded)
            {
                break;
            }

            _logger.LogInformation("Replanning triggered at iteration {Iteration}: {Rationale}", context.CurrentIteration, lastReflection.Rationale);
            plan = await _replanningService.ReplanAsync(goal, plan, lastReflection, context, cancellationToken);
        }

        var collaboration = _collaborationService.Aggregate(context.Contributions);

        var response = new AgentExecutionResponse
        {
            GoalId = goal.Id,
            Success = lastReflection?.GoalAchieved ?? false,
            FinalAnswer = collaboration.Summary,
            FinalReflection = lastReflection,
            Escalation = escalation,
            ExecutionSteps = context.History.Steps.ToList(),
            ReplanningEvents = context.ReplanningHistory.Events.ToList(),
            Contributions = context.Contributions.ToList(),
            IterationsUsed = context.CurrentIteration - 1,
            WorkingMemorySnapshot = context.Memory.Snapshot().ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        await _memoryService.RecordInteractionAsync(new InteractionRecord
        {
            GoalDescription = goal.Description,
            Domain = goal.Domain,
            ExecutionSummary = string.Join("; ", response.ExecutionSteps.Select(s => s.Action)),
            Outcome = response.Success ? "Achieved" : "Not achieved",
            Confidence = lastReflection?.ConfidenceLevel ?? 0,
            Recommendations = collaboration.Summary
        }, cancellationToken);

        return response;
    }

    private async Task ExecutePlanAsync(AgentPlan plan, AgentExecutionContext context, CancellationToken cancellationToken)
    {
        foreach (var task in plan.Tasks.OrderBy(t => t.Order))
        {
            var agent = _agents.FirstOrDefault(a => string.Equals(a.Name, task.AgentName, StringComparison.OrdinalIgnoreCase));

            if (agent is null)
            {
                context.History.Add(new ExecutionStep
                {
                    AgentName = task.AgentName,
                    Action = "Skipped",
                    Details = "No registered agent matched this task name.",
                    Success = false,
                    Iteration = context.CurrentIteration
                });
                continue;
            }

            if (!string.IsNullOrWhiteSpace(task.Condition) && !EvaluateCondition(task.Condition, context))
            {
                context.History.Add(new ExecutionStep
                {
                    AgentName = agent.Name,
                    Action = "ConditionNotMet",
                    Details = task.Condition,
                    Iteration = context.CurrentIteration
                });
                continue;
            }

            if (!await agent.CanExecuteAsync(task, context, cancellationToken))
            {
                context.History.Add(new ExecutionStep
                {
                    AgentName = agent.Name,
                    Action = "CannotExecute",
                    Iteration = context.CurrentIteration
                });
                continue;
            }

            AgentResult result;
            try
            {
                result = await agent.ExecuteAsync(task, context, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent {AgentName} failed during execution.", agent.Name);
                result = new AgentResult { AgentName = agent.Name, Success = false, Error = ex.Message };
            }

            task.IsComplete = true;

            context.History.Add(new ExecutionStep
            {
                AgentName = agent.Name,
                Action = "Execute",
                Details = result.Success ? result.Summary : result.Error,
                Success = result.Success,
                Iteration = context.CurrentIteration
            });

            if (!string.IsNullOrWhiteSpace(result.Summary))
            {
                context.Contributions.Add(new AgentContribution
                {
                    AgentName = agent.Name,
                    Type = ContributionType.Finding,
                    Content = result.Summary
                });
            }
        }
    }

    /// <summary>
    /// Minimal conditional execution support: checks whether a given memory key is present/truthy.
    /// Format: "memory:KeyName" evaluates truthiness of Context.GetMemory[bool/string](KeyName).
    /// </summary>
    private static bool EvaluateCondition(string condition, AgentExecutionContext context)
    {
        if (!condition.StartsWith("memory:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var key = condition["memory:".Length..].Trim();
        if (!context.Memory.TryGet(key, out var value) || value is null)
        {
            return false;
        }

        return value switch
        {
            bool b => b,
            string s => !string.IsNullOrWhiteSpace(s),
            _ => true
        };
    }
}
