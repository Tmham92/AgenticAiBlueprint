namespace AgenticAiBlueprint.Contracts.Models;

/// <summary>A single logged action taken during goal execution, for full auditability.</summary>
public sealed class ExecutionStep
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string AgentName { get; init; }

    public required string Action { get; init; }

    public string? Details { get; init; }

    public bool Success { get; init; } = true;

    public int Iteration { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Ordered log of every <see cref="ExecutionStep"/> taken while executing a goal.</summary>
public sealed class ExecutionHistory
{
    private readonly List<ExecutionStep> _steps = new();

    public IReadOnlyList<ExecutionStep> Steps => _steps;

    public void Add(ExecutionStep step) => _steps.Add(step);
}

/// <summary>Record of a single replanning event, capturing why the plan changed.</summary>
public sealed class ReplanningEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public int Iteration { get; init; }

    public required string Reason { get; init; }

    public required AgentPlan PreviousPlan { get; init; }

    public required AgentPlan NewPlan { get; init; }

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>Ordered log of every replanning event that occurred during goal execution.</summary>
public sealed class ReplanningHistory
{
    private readonly List<ReplanningEvent> _events = new();

    public IReadOnlyList<ReplanningEvent> Events => _events;

    public void Add(ReplanningEvent evt) => _events.Add(evt);
}
