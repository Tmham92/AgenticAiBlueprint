using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Api.Orchestration;

/// <summary>
/// Produces a revised plan when reflection determines the current plan was insufficient,
/// re-invoking the planner with reflection context (missing information) appended to the goal.
/// </summary>
public sealed class ReplanningService : IReplanningService
{
    private readonly IAgentPlanner _planner;

    public ReplanningService(IAgentPlanner planner)
    {
        _planner = planner;
    }

    public async Task<AgentPlan> ReplanAsync(AgentGoal goal, AgentPlan previousPlan, ReflectionResult reflection, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        var augmentedGoal = new AgentGoal
        {
            Id = goal.Id,
            Description = $"{goal.Description}\n\nPrevious attempt was insufficient. Missing information: " +
                          $"{(reflection.MissingInformation.Count > 0 ? string.Join(", ", reflection.MissingInformation) : "unspecified")}. " +
                          $"Reflection notes: {reflection.Rationale}",
            Domain = goal.Domain,
            Parameters = goal.Parameters
        };

        var newPlan = await _planner.CreatePlanAsync(augmentedGoal, context, cancellationToken);

        context.ReplanningHistory.Add(new ReplanningEvent
        {
            Iteration = context.CurrentIteration,
            Reason = reflection.Rationale,
            PreviousPlan = previousPlan,
            NewPlan = newPlan
        });

        return newPlan;
    }
}
