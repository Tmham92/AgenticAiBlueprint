namespace AgenticAiBlueprint.Contracts.Models;

/// <summary>Outcome of a <see cref="IReflectionAgent"/> evaluation of progress toward a goal.</summary>
public sealed class ReflectionResult
{
    public bool GoalAchieved { get; init; }

    public List<string> MissingInformation { get; init; } = new();

    public double ConfidenceLevel { get; init; }

    public bool ReplanningNeeded { get; init; }

    public bool EscalationNeeded { get; init; }

    public string Rationale { get; init; } = string.Empty;
}

/// <summary>Increasing levels of human involvement required to complete a goal.</summary>
public enum EscalationLevel
{
    Automatic = 0,
    ReviewRequired = 1,
    ApprovalRequired = 2,
    HumanInterventionRequired = 3
}

/// <summary>Decision produced by an <see cref="IEscalationAgent"/> about required human involvement.</summary>
public sealed class EscalationDecision
{
    public EscalationLevel Level { get; init; } = EscalationLevel.Automatic;

    public string Reason { get; init; } = string.Empty;

    public bool RequiresHumanAction => Level >= EscalationLevel.ReviewRequired;
}
