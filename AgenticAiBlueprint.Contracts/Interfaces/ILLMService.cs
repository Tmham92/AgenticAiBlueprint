using AgenticAiBlueprint.Contracts.Models;

namespace AgenticAiBlueprint.Contracts.Interfaces;

/// <summary>
/// Domain-agnostic abstraction over a Large Language Model provider.
/// Business logic must depend only on this interface, never on a specific provider.
/// </summary>
public interface ILLMService
{
    Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default);
}
