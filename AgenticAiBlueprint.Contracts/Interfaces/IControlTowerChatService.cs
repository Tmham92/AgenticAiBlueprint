using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Powers the Control Tower conversational interface, allowing executives to query
/// aggregated findings, risks, recommendations, and historical trends.
/// </summary>
public interface IControlTowerChatService
{
    Task<ControlTowerChatResponse> ChatAsync(ControlTowerChatRequest request, CancellationToken cancellationToken = default);

    Task<ControlTowerDashboard> GetDashboardAsync(CancellationToken cancellationToken = default);
}
