using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Produces an <see cref="AgentPlan"/> for a goal, determining which agents run, in what order, and why.
/// </summary>
public interface IAgentPlanner
{
    Task<AgentPlan> CreatePlanAsync(AgentGoal goal, AgentExecutionContext context, CancellationToken cancellationToken = default);
}
