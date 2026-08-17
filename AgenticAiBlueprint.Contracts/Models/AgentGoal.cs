namespace AgenticAiBlueprint.Contracts.Models;

/// <summary>
/// Represents a high-level, domain-agnostic goal submitted for agentic execution.
/// </summary>
public sealed class AgentGoal
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Natural-language description of what the user/system wants achieved.</summary>
    public required string Description { get; init; }

    /// <summary>Optional domain hint (e.g. "Procurement", "HR") used to scope agent selection.</summary>
    public string? Domain { get; init; }

    /// <summary>Arbitrary structured parameters supplied alongside the goal.</summary>
    public Dictionary<string, object?> Parameters { get; init; } = new();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
