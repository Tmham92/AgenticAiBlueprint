using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;
using Microsoft.Extensions.Options;

namespace AgenticAiBlueprint.Api.Services.Llm;

/// <summary>
/// Placeholder ILLMService implementation for Azure OpenAI. Not yet wired to the real
/// Azure OpenAI SDK; exists so provider switching can happen purely through Dependency Injection
/// once credentials/deployment are available.
/// </summary>
public sealed class AzureOpenAIService : ILLMService
{
    private readonly AzureOpenAIOptions _options;

    public AzureOpenAIService(IOptions<AzureOpenAIOptions> options)
    {
        _options = options.Value;
    }

    public Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint) || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "AzureOpenAIService is a placeholder and is not configured. Provide Endpoint/ApiKey/DeploymentName " +
                "in configuration and implement the Azure OpenAI SDK call, or register OllamaLLMService instead.");
        }

        throw new NotImplementedException("Azure OpenAI integration is not yet implemented.");
    }
}
