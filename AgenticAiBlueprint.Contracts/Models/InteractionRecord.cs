namespace AgenticAiBlueprint.Contracts.Models;

/// <summary>Final response returned to the caller after a goal has finished executing.</summary>
public sealed class AgentExecutionResponse
{
    public required string GoalId { get; init; }

    public bool Success { get; init; }

    public string FinalAnswer { get; init; } = string.Empty;

    public ReflectionResult? FinalReflection { get; init; }

    public EscalationDecision? Escalation { get; init; }

    public List<ExecutionStep> ExecutionSteps { get; init; } = new();

    public List<ReplanningEvent> ReplanningEvents { get; init; } = new();

    public List<AgentContribution> Contributions { get; init; } = new();

    public int IterationsUsed { get; init; }

    public Dictionary<string, object?> WorkingMemorySnapshot { get; init; } = new();
}

/// <summary>Durable record of a single goal execution, persisted for organizational learning.</summary>
public sealed class InteractionRecord
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string GoalDescription { get; init; }

    public string? Domain { get; init; }

    public string ExecutionSummary { get; init; } = string.Empty;

    public string Outcome { get; init; } = string.Empty;

    public double Confidence { get; init; }

    public string Recommendations { get; init; } = string.Empty;

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>A piece of retrievable organizational/domain knowledge.</summary>
public sealed class KnowledgeDocument
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string Title { get; init; }

    public required string Content { get; init; }

    public string? Domain { get; init; }

    public string[] Tags { get; init; } = Array.Empty<string>();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>An executive-level recommendation surfaced by the Control Tower.</summary>
public sealed class ExecutiveRecommendation
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string Problem { get; init; }

    public required string Recommendation { get; init; }

    public string ExpectedImpact { get; init; } = string.Empty;

    public RecommendationPriority Priority { get; init; } = RecommendationPriority.Medium;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum RecommendationPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>Aggregated view for the Control Tower dashboard.</summary>
public sealed class ControlTowerDashboard
{
    public List<AgentContribution> RecentFindings { get; init; } = new();

    public List<ExecutiveRecommendation> Recommendations { get; init; } = new();

    public List<InteractionRecord> RecentInteractions { get; init; } = new();

    public Dictionary<string, int> HistoricalTrends { get; init; } = new();
}

/// <summary>A single message in a Control Tower chat conversation.</summary>
public sealed class ControlTowerChatMessage
{
    public required string Role { get; init; }

    public required string Content { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class ControlTowerChatRequest
{
    public required string Message { get; init; }

    public List<ControlTowerChatMessage> History { get; init; } = new();
}

public sealed class ControlTowerChatResponse
{
    public required string Reply { get; init; }
}
