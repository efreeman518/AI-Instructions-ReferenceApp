# REFERENCE-STATUS - TaskFlow

Current verified status of the TaskFlow reference application. Used by the proof map ([../AI-Instructions-Scaffold/support/taskflow-proof-map.md](../AI-Instructions-Scaffold/support/taskflow-proof-map.md)) and consumers who need an authoritative snapshot of build/test/vulnerability state.

> **Update protocol:** when you commit reference-app changes that move build/test counts or vulnerability state, refresh this file in the same commit. HANDOFF.md narrates session history; this file is the current truth.

## Build Status

| Field | Value |
|---|---|
| Last verified | 2026-06-18 |
| Solution | `src/TaskFlow.slnx` |
| Target framework | .NET 10 |
| Projects | 39 |
| Errors | 0 |
| Warnings | 0 |

> Note: `src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj` builds separately because Uno.Sdk requires explicit invocation: `dotnet build src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj`.

## Test Status

| Project | Category filter | Verified count | Notes |
|---|---|---:|---|
| Test.Unit | `TestCategory=Unit` | 200 | mocked unit tests; re-verified 2026-07-08 (added 3 TaskItem secure-property tests for D-019; count reconciled - prior 243 was stale from earlier unrelated test churn) |
| Test.Architecture | `TestCategory=Architecture` | 22 | NetArchTest layering rules; verified 2026-06-18 |
| Test.Endpoints | `TestCategory=Endpoint` | 34 | WebApplicationFactory in-memory contract tests; verified 2026-06-18 |
| Test.E2E | `TestCategory=E2E` | 7 | WebApplicationFactory + Testcontainers SQL workflow chains; verified 2026-06-18 through Podman-backed Docker context |
| Test.Integration | `TestCategory=Integration` | 15 | service-level vs real SQL via Testcontainers; verified 2026-06-18 through Podman-backed Docker context |
| Test.Integration.FlowEngine | `TestCategory=Integration` | 16 | workflow JSON validity (deserialize, validator, in-memory registry round-trip, builder, file-presence guard); no Aspire/Docker; verified 2026-06-18 |
| Test.Aspire | multiple (`Aspire`, `Foundry`, `Integration`, `LiveAI`) | 13 | Single shared RID-free Aspire mesh graph for API, Gateway, Blazor, React, Uno, Functions, audit, and Azure Foundry capability probes; Foundry Local live coverage belongs to `Test.FoundryLocal`; verified 2026-07-08 through Podman-backed Docker context |
| Test.PlaywrightUI | n/a (Node.js) | - | hosted-stack required (see below) |
| Test.Load | `TestCategory=Load` | - | NBomber; `[Ignore]` by default; manual run |
| Test.Benchmarks | n/a | - | BenchmarkDotNet console runner; `dotnet run -c Release` |

**Current automated verification:** Test.Unit re-verified 2026-07-08 (200 passing, incl. D-019 secure-property tests) plus the `has-pending-model-changes` drift check for `TaskFlowDbContextTrxn` (clean). Architecture/Endpoint/E2E/Integration/Integration.FlowEngine/Aspire counts carry forward from 2026-06-18 and were NOT re-run this session (need a container runtime); re-run the full suite to reconfirm the aggregate. Docker-compatible runtime verified through Podman context `podman-machine-default`; Aspire runtime verified with `ASPIRE_CONTAINER_RUNTIME=podman`.

### Playwright (`src/Test/Test.PlaywrightUI/`)

Node.js Playwright suite. Run `npm install` inside the folder before first use. Tests run against a real running stack (Aspire AppHost or docker-compose). Set `PLAYWRIGHT_BASE_URL` to the host URL printed by `dotnet run --project src/Host/Aspire/AppHost`.

## Vulnerability Status

Run `dotnet list package --vulnerable --include-transitive` and capture findings here. Severity policy from [../AI-Instructions-Scaffold/support/execution-gates.md](../AI-Instructions-Scaffold/support/execution-gates.md) Section  Vulnerability Audit:

- **High/Critical:** must be fixed or recorded with owner + target resolution date
- **Moderate:** logged here, tracked but not blocking
- **Low:** team discretion

Last audited: 2026-06-18 with `dotnet list src\TaskFlow.slnx package --vulnerable --include-transitive`; no vulnerable packages reported for any project. `MessagePack` was pinned to `3.1.7` for `Test.Load` to clear the previous NBomber transitive `NU1903`. NOTE: 2026-07-08 added `Aspire.Hosting.Azure.KeyVault` 13.4.6 and `Microsoft.Data.SqlClient.AlwaysEncrypted.AzureKeyVaultProvider` 7.0.2 (D-019); re-run the vulnerability audit to reconfirm.

| Package | Version | Severity | Direct/Transitive | Advisory | Notes |
|---|---|---|---|---|---|
| _None_ | - | - | - | - | Full solution audit reported no vulnerable packages. |

The solution build currently emits no package vulnerability warnings.

## Phase Completion

Per the consolidated 5-sub-phase taxonomy:

| Phase | Status |
|---|---|
| 1 - Domain Discovery | complete |
| 2 - Resource Definition | complete |
| 3 - Implementation Plan | complete |
| 4 - Contract Scaffolding | complete |
| 5a - Foundation (TDD) | complete |
| 5b - App Core + Runtime/Edge | complete |
| 5c - Optional Hosts | complete (Gateway, Scheduler, Functions, Uno UI, Blazor) |
| 5d - Quality + Delivery | complete (architecture/load/benchmark tests, Dockerfiles, CI/CD, IaC Bicep) |
| 5e - Integration (Auth + AI) | complete (scaffold mode; live Entra/Foundry deployment-only) |
| 5e+ - Workflow Orchestration | complete (EF.FlowEngine, three shipped workflows, Blazor dashboard, admin API at `/api/flowengine/*`; agent nodes use the Aspire `IChatClient`, with no-op fallback when AI is disabled) |

## AI Runtime Status

Foundry Local verified on 2026-06-13 with:

- Foundry Local `0.8.119`
- Aspire CLI `13.4.3`
- .NET SDK `10.0.300`
- local model `qwen2.5-0.5b` / `FoundryModel.Local.Qwen2505b` (`chat, tools`)

Verified through the Aspire Gateway: D1 basic chat, D2 streaming chat, D3 code-hosted agent, D7 read-only advisor. The Aspire graph now also starts `TaskFlow.Blazor`; `/ai-chat` rendered against the live Gateway URL. D4/D5/D6/D9 side effects still require an authenticated tenant context before they can persist changes.

## Infrastructure as Code (IaC)

`infra/` contains the Bicep deployment baseline:

- `main.bicep` - top-level entry
- `modules/` - SQL, Cosmos DB, Service Bus, Storage, Key Vault (incl. the D-019 Always Encrypted CMK RSA key `taskflow-cmk`, gated by `enableAlwaysEncrypted`), App Configuration, Functions, Container Apps + environment, Static Web App, Log Analytics, deploy identity, role assignment, Cosmos RBAC

Deployment plan: [.azure/deployment-plan.md](.azure/deployment-plan.md).

Validate locally: `az bicep build --file infra/main.bicep`.

## Test Harness Architecture

`Test.Endpoints` and `Test.E2E` derive from a shared `WebApplicationFactoryBase<TProgram, TTrxnContext, TQueryContext>` in `Test.Support` (see `src/Test/Test.Support/WebApplicationFactoryBase.cs`). The base handles the standard EF.Packages plumbing swap (interceptor removal, pooled-factory removal, scoped-factory removal, reflection-based `DbContext` creation). Derived classes only specify the test-mode store:

- `Test.Endpoints/CustomApiFactory.cs` - InMemoryDatabase per factory instance
- `Test.E2E/SqlApiFactory.cs` - Testcontainers SQL Server, container managed at the class level

## Outstanding Follow-Ups

Tracked here so the next instruction-set or reference-app PR knows what's pending:

1. **Authenticated AI side effects.** D4/D5/D6/D9 persistence/enqueue side effects still require normal tenant/auth context before they can be verified end-to-end.
