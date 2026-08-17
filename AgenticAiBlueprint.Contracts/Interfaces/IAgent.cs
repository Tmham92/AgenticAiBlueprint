using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Base abstraction for any pluggable agent (core or domain-specific).
/// Agents interact with the outside world only through the supplied <see cref="AgentExecutionContext"/>.
/// </summary>
public interface IAgent
{
    /// <summary>Unique, stable name used by planners to reference this agent.</summary>
    string Name { get; }

    /// <summary>Human-readable description of what this agent does, used by the planner/LLM.</summary>
    string Description { get; }

    /// <summary>Whether this agent can/should run for the given task and context.</summary>
    Task<bool> CanExecuteAsync(PlannedTask task, AgentExecutionContext context, CancellationToken cancellationToken = default);

    /// <summary>Executes the agent's logic for the given task, reading/writing only via the context.</summary>
    Task<AgentResult> ExecuteAsync(PlannedTask task, AgentExecutionContext context, CancellationToken cancellationToken = default);
}
