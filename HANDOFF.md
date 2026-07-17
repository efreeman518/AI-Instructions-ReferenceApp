# HANDOFF - TaskFlow Reference App

> **Status: complete.** Phases 1-5e + post-phase hardening sessions are all done. This file is retained as the scaffold-flow resume contract; it is no longer the source of truth for build/test/vulnerability state.
>
> - Current verified counts, warnings, and vulnerability table -> [REFERENCE-STATUS.md](.scaffold/REFERENCE-STATUS.md).
> - Per-phase narrative and outputs -> `git log` (e.g. `git log --oneline -- HANDOFF.md`); the pre-collapse version is preserved in commit history.

## Production Deployment TODOs

- Keep the reference proof on `AuthMode: Scaffold`: it uses an automatic principal and must not require a login. Before exposing a production deployment to real users, replace the scaffold-only client experience with the selected live identity provider and follow `infra/README.md` section Optional Live Interactive Identity.
- For every enabled live interactive client, provision exact public redirect/logout URIs, app roles and assignments, `openid`/`profile` permissions, admin consent, the CIAM authority and a local CIAM acceptance user. Verify one real role-bearing sign-in from each published `Release` head. Scaffold-token tests are not live-provider evidence.
- For self-hosted or multi-proxy deployment, configure the trusted forwarded-header chain and prove public HTTPS host/path-base values. Do not accept an internal container URL in redirects or generated links.
- **Temporary dependency pin 2026-07-17:** Uno.Sdk `6.6.0-dev.166` contains the upstream browser-WASM `RootElement` startup-race fix and passed the published `Release` first-visit plus normal Uno Playwright projects from empty browser state without retry. Replace it with the first stable Uno.Sdk 6.6+ release when available.
- Browser-WASM `Release` currently publishes with `PublishTrimmed=false` because the latest stable Uno Navigation/Toolkit/WinUI packages emit upstream `IL2104` analysis failures under repository-wide `TreatWarningsAsErrors`. Re-enable trimming when those packages are trim-clean, then rerun the published cold-start gate.

## Session Summary

Phases 1-5e complete (5a Foundation, 5b App Core + Runtime, 5c Optional Hosts, 5d Quality + Delivery, 5e Integration Auth + AI) + post-phase hardening (test hardening + EF migrations, infrastructure validation, IaC) + FlowEngine workflow orchestration integration. Clean-architecture solution with rich domain model, full CRUD services/endpoints, Aspire orchestration (SQL, Redis, Azure Storage, Service Bus, Cosmos DB emulators, Foundry Local), DbContext pooling, FusionCache, middleware pipeline, YARP Gateway, TickerQ Scheduler, Azure Functions (isolated worker), Uno Platform WASM UI with MVUX + Kiota client -> Gateway, Blazor Server UI hosted in Aspire, automatic scaffold authentication with no login requirement (live Entra is deployment-only), AI integration (Azure AI Search + Microsoft Agent Framework, Foundry Local/Azure model path with no-op stubs), blob storage with multipart upload endpoint, domain event publishing, Cosmos DB read-model projections, WebApplicationFactory endpoint tests, TestContainers integration tests, EF migration baseline, CI/CD pipelines activated, IaC (Bicep) modules, plus **EF.FlowEngine** (separate `flowengine` schema in same SQL DB, three shipped workflows under `TaskFlow.Api/Workflows/`, Blazor-hosted Dashboard + Designer, admin API at `/api/flowengine/*`, agent nodes wired to the Aspire `IChatClient`, `IWorkflowTrigger` invoked from the Functions Service Bus trigger on task creation).

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
  includeReactUI: true           # Vite SPA via AddViteApp; not a .slnx member - see DESIGN-DECISIONS D-018
  includeNotifications: false
  includeAiServices: true        # scaffold mode (deployment-only, no-op stubs)
  includeFlowEngine: true        # agent nodes use the Aspire IChatClient
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
| `TaskFlow.slnx` | 4+ | Clean-architecture solution |
| `infra/` | 5d | Bicep IaC modules |
| `.azure/deployment-plan.md` | 5d | Deployment plan |
| `HANDOFF.md` | - | This file. Stays at project root as the session resume contract. |
| `src/Host/TaskFlow.Api/Workflows/*.json` | 5e+ | Three FlowEngine workflow definitions (`ai-task-triage`, `ai-task-decomposer`, `compliance-check`) - seeded at startup, validated on every build via Test.Integration.FlowEngine. |
| `src/Infrastructure/TaskFlow.Infrastructure.Data/Migrations/FlowEngine/` | 5e+ | FlowEngine schema migration (`flowengine` schema, separate migration history). |

## Outstanding Follow-Ups

Tracked in [REFERENCE-STATUS.md Section  Outstanding Follow-Ups](.scaffold/REFERENCE-STATUS.md). Current items are environment-gated or scenario-gated, not package vulnerability blockers.

AI Foundry option surfaces beyond the default inference path are documented as commented opt-ins, not wired: existing-account `RunAsExisting` and a Foundry project + prompt agent in `AppHost.cs`, and the pre-existing-agent client path (`AIProjectClient.AsAIAgent`) in `TaskFlow.Api/Program.cs`. See README "AI Demos" -> "Projects and agents". Enabling any requires Azure (prompt agents deploy to Azure even under `aspire run`).

## Resume Protocol

To start a new scaffold session against this repo, the next AI session loads `START-AI.md` (from the sister scaffold install) + this file. Because all phases are complete, a new session should treat this as a maintenance run: update `.scaffold/REFERENCE-STATUS.md` after any change that moves build/test/vulnerability state, and commit in the same commit.
