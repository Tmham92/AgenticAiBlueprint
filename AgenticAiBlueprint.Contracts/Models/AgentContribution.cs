namespace AgenticAiBlueprint.Contracts.Models;

/// <summary>Result produced by a single agent execution.</summary>
public sealed class AgentResult
{
    public required string AgentName { get; init; }

    public bool Success { get; init; } = true;

    public string Summary { get; init; } = string.Empty;

    public Dictionary<string, object?> Output { get; init; } = new();

    public string? Error { get; init; }
}

/// <summary>A finding, recommendation, or risk contributed by an agent during collaboration.</summary>
public sealed class AgentContribution
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string AgentName { get; init; }

    public ContributionType Type { get; init; } = ContributionType.Finding;

    public required string Content { get; init; }

    /// <summary>Optional reference to another contribution this one challenges or supports.</summary>
    public string? RelatedContributionId { get; init; }

    public double Confidence { get; init; } = 1.0;

    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public enum ContributionType
{
    Finding,
    Recommendation,
    Risk,
    Challenge
}

/// <summary>Aggregated result of multiple agents collaborating on a goal.</summary>
public sealed class AgentCollaborationResult
{
    public List<AgentContribution> Contributions { get; init; } = new();

    public List<string> Consensus { get; init; } = new();

    public List<string> Conflicts { get; init; } = new();

    public string Summary { get; init; } = string.Empty;
}
