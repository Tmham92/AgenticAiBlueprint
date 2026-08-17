namespace AgenticAiBlueprint.Contracts.Models;

/// <summary>A request for text completion/chat sent to an ILLMService implementation.</summary>
public sealed class LLMRequest
{
    public required string Prompt { get; init; }

    public string? SystemPrompt { get; init; }

    /// <summary>Logical model role (e.g. "Planner", "Coach", "Insight", "Default") used for routing.</summary>
    public LLMModelRole ModelRole { get; init; } = LLMModelRole.Default;

    public double Temperature { get; init; } = 0.2;
}

public sealed class LLMResponse
{
    public required string Content { get; init; }

    public string? ModelUsed { get; init; }
}

public enum LLMModelRole
{
    Default,
    Planner,
    Coach,
    Insight
}

/// <summary>Configurable mapping of logical model roles to concrete model names.</summary>
public sealed class ModelRoutingOptions
{
    public const string SectionName = "ModelRouting";

    public string DefaultModel { get; set; } = "llama3";

    public string PlannerModel { get; set; } = "llama3";

    public string CoachModel { get; set; } = "llama3";

    public string InsightModel { get; set; } = "llama3";

    public string Resolve(LLMModelRole role) => role switch
    {
        LLMModelRole.Planner => PlannerModel,
        LLMModelRole.Coach => CoachModel,
        LLMModelRole.Insight => InsightModel,
        _ => DefaultModel
    };
}

/// <summary>Configuration for connecting to a locally hosted Ollama instance.</summary>
public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";
}

/// <summary>Placeholder configuration for a future Azure OpenAI integration.</summary>
public sealed class AzureOpenAIOptions
{
    public const string SectionName = "AzureOpenAI";

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string DeploymentName { get; set; } = string.Empty;
}

/// <summary>Configuration governing the agent orchestration loop.</summary>
public sealed class OrchestrationOptions
{
    public const string SectionName = "Orchestration";

    public int MaxIterations { get; set; } = 5;

    public double MinConfidenceToComplete { get; set; } = 0.7;
}
