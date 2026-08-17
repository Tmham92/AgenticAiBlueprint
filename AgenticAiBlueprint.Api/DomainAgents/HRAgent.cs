using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Api.DomainAgents;

/// <summary>
/// Sample domain agent demonstrating the plugin model for HR scenarios.
/// </summary>
public sealed class HRAgent : IAgent
{
    public string Name => "HRAgent";

    public string Description => "Handles HR-related goals such as policy questions, onboarding, and workforce analysis.";

    public Task<bool> CanExecuteAsync(PlannedTask task, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<AgentResult> ExecuteAsync(PlannedTask task, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        context.SetMemory("hr.reviewed", true);

        return Task.FromResult(new AgentResult
        {
            AgentName = Name,
            Success = true,
            Summary = $"Reviewed HR aspects of goal: {context.Goal.Description}",
            Output = { ["reviewed"] = true }
        });
    }
}
