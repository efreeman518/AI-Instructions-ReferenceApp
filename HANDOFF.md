# HANDOFF - TaskFlow Reference App

> **Status: complete.** Phases 1-5e + post-phase hardening sessions are all done. This file is retained as the scaffold-flow resume contract; it is no longer the source of truth for build/test/vulnerability state.
>
> - Current verified counts, warnings, and vulnerability table -> [REFERENCE-STATUS.md](.scaffold/REFERENCE-STATUS.md).
> - Per-phase narrative and outputs -> `git log` (e.g. `git log --oneline -- HANDOFF.md`); the pre-collapse version is preserved in commit history.

## Session Summary

Phases 1-5e complete (5a Foundation, 5b App Core + Runtime, 5c Optional Hosts, 5d Quality + Delivery, 5e Integration Auth + AI) + post-phase hardening (test hardening + EF migrations, infrastructure validation, IaC) + FlowEngine workflow orchestration integration. Clean-architecture solution with rich domain model, full CRUD services/endpoints, Aspire orchestration (SQL, Redis, Azure Storage, Service Bus, Cosmos DB emulators, Foundry Local), DbContext pooling, FusionCache, middleware pipeline, YARP Gateway, TickerQ Scheduler, Azure Functions (isolated worker), Uno Platform WASM UI with MVUX + Kiota client -> Gateway, Blazor Server UI hosted in Aspire, config-driven authentication (Scaffold/EntraID), AI integration (Azure AI Search + Microsoft Agent Framework, Foundry Local/Azure model path with no-op stubs), blob storage with multipart upload endpoint, domain event publishing, Cosmos DB read-model projections, WebApplicationFactory endpoint tests, TestContainers integration tests, EF migration baseline, CI/CD pipelines activated, IaC (Bicep) modules, plus **EF.FlowEngine 1.0.132** (separate `flowengine` schema in same SQL DB, three shipped workflows under `TaskFlow.Api/Workflows/`, Blazor-hosted Dashboard + Designer, admin API at `/api/flowengine/*`, agent nodes wired to the Aspire `IChatClient`, `IWorkflowTrigger` invoked from the Functions Service Bus trigger on task creation).

## Current State

```yaml
instructionVersion: "1.1"
currentPhase: 5
currentSubPhase: complete
scaffoldMode: full
contractsScaffolded: true
foundationComplete: true
enabledFeatures:
  includeGateway: true
  includeScheduler: true
  includeFunctionApp: true
  includeUnoUI: true
  includeBlazorUI: true
  includeNotifications: false
  includeAiServices: true        # scaffold mode (deployment-only, no-op stubs)
  includeFlowEngine: true        # 1.0.132; agent nodes use the Aspire IChatClient
testStatus:
  unitTests: green
  endpointTests: green
  infrastructureTests: green     # Integration + E2E via Testcontainers
  flowEngineTests: green         # Test.Integration.FlowEngine - 16 workflow-validity cases
hostGates:
  scheduler: validated
  functionApp: validated
  unoUI: validated
  blazorUI: validated            # also hosts FlowEngine Dashboard + Designer
  notifications: not-applicable
```

## Outputs

| File | Phase | Purpose |
|---|---|---|
| `.scaffold/domain-specification.yaml` | 1 | Domain spec (validated against schema) |
| `.scaffold/UBIQUITOUS-LANGUAGE.md` | 1 | Shared vocabulary |
| `.scaffold/DESIGN-DECISIONS.md` | 1 | Decision dependency graph |
| `.scaffold/resource-implementation.yaml` | 2 | Resource definition (validated) |
| `.scaffold/implementation-plan.md` | 3 | Vertical slice order |
| `.scaffold/INSTRUCTION-GAPS.md` | - | Recorded gaps for the maintenance repo |
| `.scaffold/REFERENCE-STATUS.md` | - | Current verified build/test/vuln snapshot |
| `dotnet-tools.json` | 3 | Local tool manifest (`ilspycmd`, `dotnet-stryker`) |
| `src/TaskFlow.slnx` | 4+ | 32-project clean-architecture solution |
| `infra/` | 5d | Bicep IaC modules |
| `.azure/deployment-plan.md` | 5d | Deployment plan |
| `HANDOFF.md` | - | This file. Stays at project root as the session resume contract. |
| `src/Host/TaskFlow.Api/Workflows/*.json` | 5e+ | Three FlowEngine workflow definitions (`ai-task-triage`, `ai-task-decomposer`, `compliance-check`) - seeded at startup, validated on every build via Test.Integration.FlowEngine. |
| `src/Infrastructure/TaskFlow.Infrastructure.Data/Migrations/FlowEngine/` | 5e+ | FlowEngine schema migration (`flowengine` schema, separate migration history). |

## Outstanding Follow-Ups

Tracked in [REFERENCE-STATUS.md Section  Outstanding Follow-Ups](.scaffold/REFERENCE-STATUS.md). Primary: vulnerability resolution (`System.Security.Cryptography.Xml`, `OpenTelemetry.Api`) pending upstream patches.

AI Foundry option surfaces beyond the default inference path are documented as commented opt-ins, not wired: existing-account `RunAsExisting` and a Foundry project + prompt agent in `AppHost.cs`, and the pre-existing-agent client path (`AIProjectClient.AsAIAgent`) in `TaskFlow.Api/Program.cs`. See README "AI Demos" -> "Projects and agents". Enabling any requires Azure (prompt agents deploy to Azure even under `aspire run`).

## Resume Protocol

To start a new scaffold session against this repo, the next AI session loads `START-AI.md` (from the sister scaffold install) + this file. Because all phases are complete, a new session should treat this as a maintenance run: update `.scaffold/REFERENCE-STATUS.md` after any change that moves build/test/vulnerability state, and commit in the same commit.
