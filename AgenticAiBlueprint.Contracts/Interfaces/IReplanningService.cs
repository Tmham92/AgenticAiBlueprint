using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Produces a revised <see cref="AgentPlan"/> when reflection determines the current plan is insufficient.
/// </summary>
public interface IReplanningService
{
    Task<AgentPlan> ReplanAsync(AgentGoal goal, AgentPlan previousPlan, ReflectionResult reflection, AgentExecutionContext context, CancellationToken cancellationToken = default);
}
