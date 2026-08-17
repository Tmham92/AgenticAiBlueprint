using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Coordinates the full goal-based execution loop: plan, execute agents, reflect, replan, escalate.
/// </summary>
public interface IAgentOrchestrator
{
    Task<AgentExecutionResponse> ExecuteGoalAsync(AgentGoal goal, CancellationToken cancellationToken = default);
}
