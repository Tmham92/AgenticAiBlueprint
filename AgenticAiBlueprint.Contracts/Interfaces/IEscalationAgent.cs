using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Decides how much human involvement is required to safely complete a goal (Human-In-The-Loop).
/// </summary>
public interface IEscalationAgent
{
    Task<EscalationDecision> EvaluateAsync(AgentGoal goal, ReflectionResult reflection, AgentExecutionContext context, CancellationToken cancellationToken = default);
}
