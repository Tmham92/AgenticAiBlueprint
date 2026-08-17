using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Evaluates progress toward a goal after execution: whether it's achieved, missing information,
/// confidence level, and whether replanning or escalation is required.
/// </summary>
public interface IReflectionAgent
{
    Task<ReflectionResult> ReflectAsync(AgentGoal goal, AgentPlan plan, AgentExecutionContext context, CancellationToken cancellationToken = default);
}
