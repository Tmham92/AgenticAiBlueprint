# Architecture

This document describes the technical architecture of the Agentic AI Blueprint platform: a reusable, domain-agnostic foundation for goal-driven, multi-agent AI applications on .NET 10.

## Goals

- **Domain-agnostic core.** Nothing in `Contracts` or the orchestration engine (`AgenticAiBlueprint.Api/Orchestration`) references any specific business domain.
- **Pluggable domain logic.** New domains are added purely by implementing `IAgent` and registering it via DI — no core code changes.
- **Provider independence.** LLM providers, persistence, and knowledge retrieval are abstracted behind interfaces so implementations can be swapped without touching business logic.
- **Full auditability.** Every plan, execution step, reflection, replanning event, and escalation decision is logged and retrievable.

## Solution structure

```
AgenticAiBlueprint.Contracts   Interfaces + models only. No implementation, no external dependencies beyond BCL.
AgenticAiBlueprint.Api         ASP.NET Core minimal API host + orchestration engine + service implementations.
AgenticAiBlueprint.Web         Blazor Server UI (Control Tower).
```

Dependency direction: `Api` → `Contracts`, `Web` → `Contracts`. `Web` talks to `Api` only over HTTP (via `AgenticApiClient`), never via a shared in-process reference to orchestration internals — this keeps the UI deployable independently of the API.

## Core execution flow

```
AgentGoal
   │
   ▼
IAgentPlanner.CreatePlanAsync            (LLMAgentPlanner)
   │  → AgentPlan { PlannedTask[] }
   ▼
DefaultAgentOrchestrator loop (per iteration, up to MaxIterations):
   │
   ├─▶ Execute each PlannedTask via matching IAgent
   │      - IAgent.CanExecuteAsync (guard)
   │      - Conditional execution ("memory:<key>" conditions)
   │      - IAgent.ExecuteAsync → AgentResult
   │      - Result recorded as ExecutionStep + AgentContribution
   │
   ├─▶ IReflectionAgent.ReflectAsync → ReflectionResult
   │      (GoalAchieved, Confidence, MissingInformation, ReplanningNeeded, EscalationNeeded)
   │
   ├─▶ IEscalationAgent.EvaluateAsync → EscalationDecision (EscalationLevel)
   │
   └─▶ if not achieved and not escalated and replanning needed:
		  IReplanningService.ReplanAsync → new AgentPlan (loop continues)
	   else: break
   │
   ▼
IAgentCollaborationService.Aggregate(contributions) → AgentCollaborationResult
   │
   ▼
IOrganizationalMemoryService.RecordInteractionAsync(InteractionRecord)
   │
   ▼
AgentExecutionResponse  (returned to caller / Blazor UI)
```

### Key types (all in `AgenticAiBlueprint.Contracts.Models`)

- **`AgentGoal`** — the natural-language goal + optional domain hint + parameters.
- **`AgentPlan` / `PlannedTask`** — ordered list of agents to run, with reasons and optional conditions.
- **`AgentExecutionContext`** — carries `WorkingMemory`, `ExecutionHistory`, `ReplanningHistory`, and `Contributions` through the whole run. Agents receive only this context — never references to other agents.
- **`WorkingMemory`** — thread-safe key/value store (`Set`/`Get`). This is the *only* channel through which agents communicate, satisfying the "no direct agent dependencies" principle.
- **`ExecutionStep` / `ExecutionHistory`** — append-only audit log of every agent action.
- **`ReplanningEvent` / `ReplanningHistory`** — audit log of why/when the plan changed.
- **`AgentContribution` / `AgentCollaborationResult`** — findings/recommendations/risks/challenges agents contribute, aggregated into consensus vs. conflicts.
- **`ReflectionResult`** — structured verdict from the reflection step.
- **`EscalationLevel` / `EscalationDecision`** — Automatic → ReviewRequired → ApprovalRequired → HumanInterventionRequired.
- **`InteractionRecord`** — persisted summary of a completed goal execution (organizational learning).
- **`KnowledgeDocument`** — retrievable knowledge unit (designed for future RAG/vector search).
- **`ExecutiveRecommendation` / `ControlTowerDashboard`** — aggregated executive-facing insights.

## Interfaces (contracts) and their default implementations

| Interface | Default implementation | Location |
|---|---|---|
| `IAgent` | `ProcurementAgent`, `HRAgent` (samples) | `Api/DomainAgents` |
| `IAgentPlanner` | `LLMAgentPlanner` | `Api/Orchestration` |
| `IAgentOrchestrator` | `DefaultAgentOrchestrator` | `Api/Orchestration` |
| `IReflectionAgent` | `ReflectionAgent` | `Api/Orchestration` |
| `IEscalationAgent` | `EscalationAgent` | `Api/Orchestration` |
| `IReplanningService` | `ReplanningService` | `Api/Orchestration` |
| `IAgentCollaborationService` | `AgentCollaborationService` | `Api/Orchestration` |
| `ILLMService` | `OllamaLLMService` (active), `AzureOpenAIService` (placeholder) | `Api/Services/Llm` |
| `IKnowledgeService` | `SqliteKnowledgeService` | `Api/Services/Knowledge` |
| `IOrganizationalMemoryService` | `SqliteOrganizationalMemoryService` | `Api/Services/Memory` |
| `IControlTowerChatService` | `ControlTowerChatService` | `Api/Services/ControlTower` |
| `IExecutiveRecommendationAgent` | `ExecutiveRecommendationAgent` | `Api/Services/ControlTower` |

All registrations happen in `AgenticPlatformServiceCollectionExtensions.AddAgenticCore(IServiceCollection, IConfiguration)`, called once from `Program.cs`. Swapping any implementation (e.g. moving from Ollama to Azure OpenAI, or SQLite to PostgreSQL) is a one-line change in this file — no consumer code changes.

## LLM abstraction and model routing

- `ILLMService.CompleteAsync(LLMRequest)` is the single entry point business/orchestration code uses. No code outside `Api/Services/Llm` should know which provider is active.
- `LLMRequest.ModelRole` (`Default`, `Planner`, `Coach`, `Insight`) allows different logical steps to route to different models via `ModelRoutingOptions` (bound from configuration section `ModelRouting`).
- `OllamaLLMService` calls a local Ollama server over HTTP and **falls back to a deterministic heuristic response** if unreachable, so the orchestration loop remains functional without a live model (useful for builds/demos/tests).
- `AzureOpenAIService` is an unimplemented placeholder proving the interface can be satisfied by a different provider purely through DI registration.

## Persistence

- `AgenticDbContext` (EF Core) backs both `IOrganizationalMemoryService` and `IKnowledgeService` using SQLite (`agentic-blueprint.db`, created via `EnsureCreatedAsync` on startup).
- Uses `IDbContextFactory<AgenticDbContext>` (pooled) so scoped/singleton services can safely create short-lived contexts per operation.
- `KnowledgeDocument.Tags` is stored as a delimited string via an EF value converter.
- Designed to be swapped for a server-based provider (Azure SQL, PostgreSQL) by changing the `UseSqlite(...)` call and connection string — no interface changes required.

## Control Tower

- `ControlTowerChatService` aggregates recent `InteractionRecord`s, `ExecutiveRecommendation`s, and simple domain-based trend counts into a `ControlTowerDashboard`.
- `ChatAsync` grounds LLM responses in that aggregated context, effectively a lightweight retrieval-augmented chat over organizational history (not yet true vector-based RAG — see Knowledge Layer below).
- `ExecutiveRecommendationAgent` scans recent low-confidence interactions and asks the LLM (routed to the `Insight` model role) to summarize a systemic problem and recommend an action, tagged with a computed `RecommendationPriority`.

## Knowledge layer (RAG readiness)

- `IKnowledgeService` is intentionally minimal today (`AddDocumentAsync`, `SearchAsync` via substring match over SQLite).
- The interface signature (`query`, `domain`, `maxResults`) is designed so a future implementation can swap in embeddings + vector search (e.g. pgvector, Azure AI Search, Qdrant) without changing any calling agent.

## Human-in-the-loop / escalation model

`EscalationAgent` is deliberately simple and deterministic:

- `EscalationNeeded` + confidence `< 0.3` → `HumanInterventionRequired`
- `EscalationNeeded` (any confidence) → `ApprovalRequired`
- Goal not achieved after `MaxIterations` → `ReviewRequired`
- Confidence `< 0.5` → `ReviewRequired`
- Otherwise → `Automatic`

This policy is a starting point — replace with organization-specific rules (e.g. dollar-value thresholds, regulatory domain, agent-reported risk) as needed. The `EscalationDecision` is surfaced to the caller/UI but has **no built-in workflow** (no notification/approval system) — see README "What needs to be updated."

## Web (Blazor) architecture

- Blazor Server, Interactive Server render mode.
- `AgenticApiClient` (typed `HttpClient`) is the only way the UI talks to the API — no shared orchestration types are invoked in-process, keeping API and UI independently deployable/scalable.
- Pages:
  - `Home.razor` (`/`) — Goal Input, Execution Trace, Reflection Results, Escalation, Replanning History, Agent Contribution Viewer, Working Memory Viewer.
  - `ControlTowerDashboardPage.razor` (`/control-tower`) — dashboard of trends, recommendations, recent interactions.
  - `ControlTowerChat.razor` (`/control-tower/chat`) — conversational Control Tower interface.

> Note: the dashboard page's file/class is named `ControlTowerDashboardPage` (not `ControlTowerDashboard`) to avoid a type name collision with the `ControlTowerDashboard` model in `Contracts`.

## API surface

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/api/health` | Liveness check. |
| `POST` | `/api/agent/execute` | Submit an `AgentGoal`, run the full orchestration loop, return `AgentExecutionResponse`. |
| `POST` | `/api/controltower/chat` | Control Tower conversational query. |
| `GET` | `/api/controltower` | Aggregated `ControlTowerDashboard`. |
| `GET` | `/api/interactions` | Recent `InteractionRecord`s (organizational memory). |
| `GET` | `/api/recommendations` | Current `ExecutiveRecommendation`s. |

CORS is currently open (`AllowAnyOrigin/Header/Method`) for local development — tighten before production.

## Extensibility model (adding a new domain)

1. Implement `IAgent` in a new class (naming convention: `<Domain>Agent`, e.g. `FinanceComplianceAgent`).
2. Read/write only through `AgentExecutionContext`/`WorkingMemory` — never call another agent directly.
3. Register with `services.AddDomainAgent<YourAgent>()`.
4. Optionally seed domain knowledge via `IKnowledgeService.AddDocumentAsync`.
5. Optionally add domain-specific Blazor pages that call new/existing API endpoints via `AgenticApiClient`.
6. No changes required to `Contracts`, `Orchestration`, or the Control Tower services — they operate purely against the registered `IAgent` collection and shared models.

## Non-goals / explicit boundaries

- This platform does not implement authentication/authorization, production-grade observability, or a real approval/notification workflow — these are expected to be added per deployment (see README).
- The planner/reflection LLM response parsing is intentionally simple (structured text, not JSON mode) to keep the reference implementation provider-agnostic; production usage should prefer structured/function-calling output where the provider supports it.
