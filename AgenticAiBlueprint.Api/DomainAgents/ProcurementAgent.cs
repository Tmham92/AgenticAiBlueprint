using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Api.DomainAgents;

/// <summary>
/// Sample domain agent demonstrating the plugin model for Procurement scenarios.
/// Future domain solutions add agents like this without modifying the core platform.
/// </summary>
public sealed class ProcurementAgent : IAgent
{
    public string Name => "ProcurementAgent";

    public string Description => "Handles procurement-related goals such as purchase order review, vendor evaluation, and spend analysis.";

    public Task<bool> CanExecuteAsync(PlannedTask task, AgentExecutionContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<AgentResult> ExecuteAsync(PlannedTask task, AgentExecutionContext context, CancellationToken cancellationToken = default)
    {
        context.SetMemory("procurement.reviewed", true);

        return Task.FromResult(new AgentResult
        {
            AgentName = Name,
            Success = true,
            Summary = $"Reviewed procurement aspects of goal: {context.Goal.Description}",
            Output = { ["reviewed"] = true }
        });
    }
}
