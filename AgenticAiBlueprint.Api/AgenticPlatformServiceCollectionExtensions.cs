using AgenticAiBlueprint.Api.DomainAgents;
using AgenticAiBlueprint.Api.Orchestration;
using AgenticAiBlueprint.Api.Persistence;
using AgenticAiBlueprint.Api.Services.ControlTower;
using AgenticAiBlueprint.Api.Services.Knowledge;
using AgenticAiBlueprint.Api.Services.Llm;
using AgenticAiBlueprint.Api.Services.Memory;
using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;
using Microsoft.EntityFrameworkCore;

namespace AgenticAiBlueprint.Api;

/// <summary>
/// Registers the core Agentic AI Blueprint platform: orchestration engine, LLM abstraction,
/// persistence, and Control Tower services. Domain solutions call this once, then register their
/// own <see cref="IAgent"/> implementations (via <see cref="AddDomainAgent{TAgent}"/> or plain DI)
/// without modifying anything in this method.
/// </summary>
public static class AgenticPlatformServiceCollectionExtensions
{
    public static IServiceCollection AddAgenticCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<ModelRoutingOptions>(configuration.GetSection(ModelRoutingOptions.SectionName));
        services.Configure<OllamaOptions>(configuration.GetSection(OllamaOptions.SectionName));
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<OrchestrationOptions>(configuration.GetSection(OrchestrationOptions.SectionName));

        var sqliteConnectionString = configuration.GetConnectionString("AgenticDb") ?? "Data Source=agentic-blueprint.db";
        services.AddPooledDbContextFactory<AgenticDbContext>(options => options.UseSqlite(sqliteConnectionString));

        services.AddHttpClient<ILLMService, OllamaLLMService>((sp, client) =>
        {
            var ollamaOptions = configuration.GetSection(OllamaOptions.SectionName).Get<OllamaOptions>() ?? new OllamaOptions();
            client.BaseAddress = new Uri(ollamaOptions.BaseUrl);
        });

        services.AddSingleton<IOrganizationalMemoryService, SqliteOrganizationalMemoryService>();
        services.AddSingleton<IKnowledgeService, SqliteKnowledgeService>();

        services.AddScoped<IAgentPlanner, LLMAgentPlanner>();
        services.AddScoped<IReflectionAgent, ReflectionAgent>();
        services.AddScoped<IEscalationAgent, EscalationAgent>();
        services.AddScoped<IReplanningService, ReplanningService>();
        services.AddSingleton<IAgentCollaborationService, AgentCollaborationService>();
        services.AddScoped<IAgentOrchestrator, DefaultAgentOrchestrator>();

        services.AddScoped<IExecutiveRecommendationAgent, ExecutiveRecommendationAgent>();
        services.AddScoped<IControlTowerChatService, ControlTowerChatService>();

        // Sample domain agents demonstrating the plugin model. Real domain solutions register
        // their own agents the same way, without modifying core platform code.
        services.AddDomainAgent<ProcurementAgent>();
        services.AddDomainAgent<HRAgent>();

        return services;
    }

    /// <summary>Registers a domain-specific <see cref="IAgent"/> implementation for use by the orchestrator.</summary>
    public static IServiceCollection AddDomainAgent<TAgent>(this IServiceCollection services) where TAgent : class, IAgent
    {
        services.AddScoped<IAgent, TAgent>();
        return services;
    }
}
