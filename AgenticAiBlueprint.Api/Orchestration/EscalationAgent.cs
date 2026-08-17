using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Api.Orchestration;

/// <summary>
/// Configurable escalation agent implementing Human-In-The-Loop policy. Escalation thresholds
/// are simple and deterministic so behavior is predictable and auditable, but can be replaced
/// via DI with a more sophisticated policy engine.
/// </summary>
public sealed class EscalationAgent : IEscalationAgent
{
    public Task<EscalationDecision> EvaluateAsync(AgentGoal goal, ReflectionResult reflection, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        EscalationLevel level;
        string reason;

        if (reflection.EscalationNeeded && reflection.ConfidenceLevel < 0.3)
        {
            level = EscalationLevel.HumanInterventionRequired;
            reason = "Very low confidence and explicit escalation signal from reflection.";
        }
        else if (reflection.EscalationNeeded)
        {
            level = EscalationLevel.ApprovalRequired;
            reason = "Reflection flagged escalation as needed.";
        }
        else if (!reflection.GoalAchieved && context.CurrentIteration >= context.MaxIterations)
        {
            level = EscalationLevel.ReviewRequired;
            reason = "Maximum iterations reached without achieving the goal.";
        }
        else if (reflection.ConfidenceLevel < 0.5)
        {
            level = EscalationLevel.ReviewRequired;
            reason = "Confidence below acceptable threshold.";
        }
        else
        {
            level = EscalationLevel.Automatic;
            reason = "Goal achieved with sufficient confidence; no human involvement required.";
        }

        return Task.FromResult(new EscalationDecision
        {
            Level = level,
            Reason = reason
        });
    }
}
