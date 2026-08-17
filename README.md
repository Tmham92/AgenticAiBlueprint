# Agentic AI Blueprint

A reusable, **domain-agnostic** foundation for building Agentic AI applications on .NET 10. This is not a finished product for one business domain — it is a **platform/starter kit** you clone and extend for Procurement, HR, Finance, Customer Service, IT Operations, Governance, Compliance, Supply Chain, or any other domain that needs goal-driven, multi-agent automation.

See [`ARCHITECTURE.md`](./ARCHITECTURE.md) for the detailed technical architecture.

## What's in this repository

| Project | Purpose |
|---|---|
| `AgenticAiBlueprint.Contracts` | Pure abstractions: interfaces (`IAgent`, `IAgentPlanner`, `IAgentOrchestrator`, `IReflectionAgent`, `IEscalationAgent`, `IReplanningService`, `ILLMService`, `IKnowledgeService`, `IOrganizationalMemoryService`, `IControlTowerChatService`, ...) and models (`AgentGoal`, `AgentPlan`, `AgentExecutionContext`/`WorkingMemory`, `ExecutionHistory`, `ReplanningHistory`, `AgentContribution`, `ReflectionResult`, `EscalationLevel`, `InteractionRecord`, `KnowledgeDocument`, `ExecutiveRecommendation`, Control Tower chat/dashboard types). No implementation, no dependencies on frameworks or providers. |
| `AgenticAiBlueprint.Api` | ASP.NET Core minimal API + the orchestration engine: `DefaultAgentOrchestrator` (Goal → Plan → Execute → Reflect → Replan → Escalate loop), `LLMAgentPlanner`, `ReflectionAgent`, `EscalationAgent`, `ReplanningService`, `AgentCollaborationService`, LLM providers (`OllamaLLMService`, placeholder `AzureOpenAIService`), SQLite-backed `IOrganizationalMemoryService`/`IKnowledgeService`, Control Tower services, and two **sample** domain agents (`ProcurementAgent`, `HRAgent`) that exist purely to demonstrate the plugin model. |
| `AgenticAiBlueprint.Web` | Blazor Server UI: Goal Input, Execution Trace, Reflection Results, Escalation view, Replanning History, Agent Contribution Viewer, Working Memory Viewer, Control Tower Dashboard, Control Tower Chat — all calling the Api via a typed `HttpClient` (`AgenticApiClient`). |

### Core architectural principles already implemented

1. Goal-Based Execution — `AgentGoal` in, `AgentExecutionResponse` out.
2. Agent Planning — `IAgentPlanner` / `LLMAgentPlanner`.
3. Agent Orchestration — `IAgentOrchestrator` / `DefaultAgentOrchestrator`.
4. Shared Memory — `WorkingMemory` + `AgentExecutionContext`; agents never call each other directly.
5. Reflection — `IReflectionAgent` / `ReflectionAgent`.
6. Replanning — `IReplanningService`, bounded by `OrchestrationOptions.MaxIterations`.
7. Human-In-The-Loop — `IEscalationAgent`, `EscalationLevel` (Automatic → ReviewRequired → ApprovalRequired → HumanInterventionRequired).
8. Agent Collaboration — `AgentContribution`, `IAgentCollaborationService` (consensus/conflict aggregation).
9. Organizational Learning — `IOrganizationalMemoryService` (SQLite via EF Core), `InteractionRecord`.
10. Executive Insights — `IExecutiveRecommendationAgent`, `ExecutiveRecommendation`.
11. Control Tower Chat — `IControlTowerChatService`, dashboard + chat grounded in aggregated data.

Business/domain logic is intended to live **only** in pluggable `IAgent` implementations registered via `AddDomainAgent<TAgent>()`. Nothing in `Contracts` or the orchestration engine should need to change when you add a new domain.

## Getting started

```powershell
# restore & build
dotnet build .\AgenticAiBlueprint.slnx

# run the API (SQLite db is created automatically on first run)
dotnet run --project .\AgenticAiBlueprint.Api

# run the Blazor UI (in a second terminal)
dotnet run --project .\AgenticAiBlueprint.Web
```

The Web app calls the Api at the URL configured in `AgenticAiBlueprint.Web/appsettings.json` (`AgenticApi:BaseUrl`, defaults to `https://localhost:7007`) — check `AgenticAiBlueprint.Api/Properties/launchSettings.json` for the actual port and adjust if needed.

By default the LLM provider is `OllamaLLMService`, which expects a local [Ollama](https://ollama.com) instance at `http://localhost:11434` (configurable under `Ollama:BaseUrl`). If Ollama isn't running, the service falls back to a deterministic heuristic response so the app still runs end-to-end for demos/dev — **replace this before relying on real planning/reflection quality.**

## Using this as a starting point for a new application

Recommended workflow:

1. **Clone/fork** this repository as the base for your new domain solution (e.g. `FinanceAgenticApp`).
2. **Define your domain goals** — what natural-language goals will users submit? (e.g. "Review this invoice for policy compliance and recommend approval or rejection.")
3. **Create domain agents** in a new folder/project (e.g. `DomainAgents/InvoiceComplianceAgent.cs`) implementing `IAgent`. Keep agents focused and stateless; communicate only through `AgentExecutionContext`/`WorkingMemory`.
4. **Register your agents** with `services.AddDomainAgent<YourAgent>()` next to the existing sample registrations in `AgenticPlatformServiceCollectionExtensions.AddAgenticCore` (or better, move domain registration into your own extension method/project so the core stays untouched).
5. **Define domain rules/services** (validation, business calculations, external system integrations) as ordinary DI services your agents depend on — keep them out of `Contracts`/orchestration.
6. **Seed domain knowledge** via `IKnowledgeService.AddDocumentAsync` for grounding/RAG-style lookups.
7. **Adjust the Blazor UI** — rename pages/branding, add domain-specific views (e.g. an invoice detail viewer) alongside the existing Control Tower/Execution Trace pages.
8. Remove the sample `ProcurementAgent`/`HRAgent` once you have real domain agents (or leave them as a reference).

## What needs to be updated before production use

These are known gaps/placeholders intentionally left for you to fill in per-project:

- **LLM providers**: `AzureOpenAIService` is a stub that throws `NotImplementedException`. Implement it with the Azure OpenAI SDK, or add another provider (OpenAI, Anthropic, local model server) — all consumers depend only on `ILLMService`, so this is a drop-in DI swap.
- **Planner/Reflection parsing**: `LLMAgentPlanner` and `ReflectionAgent` parse simple structured text from the LLM. For production, prefer JSON-mode/function-calling/structured output from your model provider for more reliable parsing.
- **Knowledge retrieval**: `SqliteKnowledgeService` uses basic substring search. Swap in embeddings + vector search (e.g. Azure AI Search, Qdrant, pgvector, SQLite vector extensions) behind the same `IKnowledgeService` interface for real RAG.
- **Escalation/Human-in-the-loop UX**: `EscalationAgent` thresholds are simple heuristics. Wire `EscalationLevel.ReviewRequired`/`ApprovalRequired`/`HumanInterventionRequired` to real notification/approval workflows (email, Teams, ticketing) and a UI for humans to act on them.
- **Authentication/Authorization**: none currently implemented on the API or Blazor app — add ASP.NET Core Identity, Entra ID, or your preferred auth before exposing this beyond localhost.
- **Persistence**: SQLite is used for simplicity/local dev. Consider Azure SQL/PostgreSQL for multi-instance/production deployments; the `AgenticDbContext` and EF Core setup make this a low-effort provider swap.
- **Observability**: add structured logging/tracing (e.g. OpenTelemetry) around orchestration iterations, replanning events, and escalation decisions for auditability in production.
- **Testing**: no automated tests exist yet. Add unit tests for orchestration logic (mock `IAgent`/`ILLMService`) and integration tests for the API endpoints.
- **Model routing configuration**: `ModelRoutingOptions` defaults all roles (`Planner`, `Coach`, `Insight`, `Default`) to the same model name (`llama3`). Update `appsettings.json` per environment/model availability.

## Suggested next steps (AI-assisted development)

Since the goal is to use AI to accelerate further development, a productive sequence is:

1. Ask AI to **generate new domain agents** from a plain-language description of a business process — point it at `IAgent`, `AgentExecutionContext`, and an existing sample agent (`ProcurementAgent`) as the pattern to follow.
2. Ask AI to **draft the planner prompt/knowledge base** for your domain (what agents exist, when each should run) and feed it into `LLMAgentPlanner`'s system prompt or a domain-specific planner variant.
3. Ask AI to **implement a real `ILLMService`** for your chosen provider (Azure OpenAI, OpenAI, etc.) following the existing `OllamaLLMService` as a template.
4. Ask AI to **generate Blazor pages** for domain-specific data entry/visualization, reusing `AgenticApiClient` as the calling pattern.
5. Ask AI to **write unit/integration tests** for new agents and orchestration paths as you add them, using the interfaces in `Contracts` for mocking.
6. Iterate: goal → agent → test → UI, keeping all new logic inside domain-agent/service code so the core platform (`Contracts`, orchestration engine) remains untouched and reusable across future projects.

## Repository layout

```
AgenticAiBlueprint.slnx
AgenticAiBlueprint.Contracts/    # interfaces + models (no implementation)
AgenticAiBlueprint.Api/          # orchestration engine + minimal API + sample domain agents
AgenticAiBlueprint.Web/          # Blazor Server Control Tower UI
ARCHITECTURE.md                  # detailed architecture reference
```
