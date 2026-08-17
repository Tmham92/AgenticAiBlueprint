namespace AgenticAiBlueprint.Contracts.Models;

/// <summary>
/// A single task within an agent execution plan, identifying which agent should run and why.
/// </summary>
public sealed class PlannedTask
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Name of the agent that should execute this task (matches <see cref="IAgent.Name"/>).</summary>
    public required string AgentName { get; init; }

    /// <summary>Reason the planner selected this agent/task, useful for auditability.</summary>
    public string Reason { get; init; } = string.Empty;

    /// <summary>Order of execution relative to other tasks (lower runs first).</summary>
    public int Order { get; init; }

    /// <summary>Optional condition expression evaluated against working memory before execution.</summary>
    public string? Condition { get; init; }

    public bool IsComplete { get; set; }
}

/// <summary>
/// An ordered set of tasks produced by an <see cref="IAgentPlanner"/> in response to a goal.
/// </summary>
public sealed class AgentPlan
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string GoalId { get; init; }

    public List<PlannedTask> Tasks { get; init; } = new();

    /// <summary>Overall rationale for the plan as produced by the planner/LLM.</summary>
    public string Rationale { get; init; } = string.Empty;

    public int Iteration { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
