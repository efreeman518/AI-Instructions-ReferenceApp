# REFERENCE-STATUS — TaskFlow

Current verified status of the TaskFlow reference application. Used by the proof map ([../AI-Instructions-Scaffold/support/taskflow-proof-map.md](../AI-Instructions-Scaffold/support/taskflow-proof-map.md)) and consumers who need an authoritative snapshot of build/test/vulnerability state.

> **Update protocol:** when you commit reference-app changes that move build/test counts or vulnerability state, refresh this file in the same commit. HANDOFF.md narrates session history; this file is the current truth.

## Build Status

| Field | Value |
|---|---|
| Last verified | 2026-04-28 |
| Solution | `src/TaskFlow.slnx` |
| Target framework | .NET 10 |
| Projects | 32 |
| Errors | 0 |
| Warnings | 28 (all NU1902/NU1903 — vulnerable transitive packages; tracked under Vulnerability Status) |

> Note: `src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj` builds separately because Uno.Sdk requires explicit invocation: `dotnet build src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj`.

## Test Status

| Project | Category filter | Verified count | Notes |
|---|---|---:|---|
| Test.Unit | `TestCategory=Unit` | 245 | mocked unit tests |
| Test.Architecture | `TestCategory=Architecture` | 12 | NetArchTest layering rules |
| Test.Endpoints | `TestCategory=Endpoint` | 36 | WebApplicationFactory in-memory contract tests |
| Test.E2E | `TestCategory=E2E` | 7 | WebApplicationFactory + Testcontainers SQL workflow chains (~40s) |
| Test.Integration | `TestCategory=Integration` | 14 | service-level vs real SQL via Testcontainers (~170s) |
| Test.PlaywrightUI | n/a (Node.js) | — | hosted-stack required (see below) |
| Test.Load | `TestCategory=Load` | — | NBomber; `[Ignore]` by default; manual run |
| Test.Benchmarks | n/a | — | BenchmarkDotNet console runner; `dotnet run -c Release` |

**Total automated:** 314 tests passing across Unit/Architecture/Endpoint/E2E/Integration.

### Playwright (`src/Test/Test.PlaywrightUI/`)

Node.js Playwright suite. Run `npm install` inside the folder before first use. Tests run against a real running stack (Aspire AppHost or docker-compose). Set `PLAYWRIGHT_BASE_URL` to the host URL printed by `dotnet run --project src/Host/Aspire/AppHost`.

## Vulnerability Status

Run `dotnet list package --vulnerable --include-transitive` and capture findings here. Severity policy from [../AI-Instructions-Scaffold/support/execution-gates.md](../AI-Instructions-Scaffold/support/execution-gates.md) § Vulnerability Audit:

- **High/Critical:** must be fixed or recorded with owner + target resolution date
- **Moderate:** logged here, tracked but not blocking
- **Low:** team discretion

Last audited: 2026-04-28 (build-time NU1902/NU1903 warnings).

| Package | Version | Severity | Direct/Transitive | Advisory | Notes |
|---|---|---|---|---|---|
| `System.Security.Cryptography.Xml` | 8.0.2 | **High** | Transitive | [GHSA-37gx-xxp4-5rgx](https://github.com/advisories/GHSA-37gx-xxp4-5rgx), [GHSA-w3x6-4m5h-cxqf](https://github.com/advisories/GHSA-w3x6-4m5h-cxqf) | Recorded as deployment-blocker; pinned awaiting upstream patch in identity-related dependency chain. Owner: ef. Target: next .NET 10 servicing release. |
| `OpenTelemetry.Api` | 1.12.0, 1.15.1 | Moderate | Transitive | [GHSA-g94r-2vxg-569j](https://github.com/advisories/GHSA-g94r-2vxg-569j) | Logged. Tracking upstream OpenTelemetry release; not blocking per pragmatic warning policy. |

The build emits a warning per vulnerable package per consuming project (28 total warnings). When the upstream packages publish patched versions, bumping `Directory.Packages.props` should clear all instances simultaneously.

## Phase Completion

Per the consolidated 5-sub-phase taxonomy:

| Phase | Status |
|---|---|
| 1 — Domain Discovery | complete |
| 2 — Resource Definition | complete |
| 3 — Implementation Plan | complete |
| 4 — Contract Scaffolding | complete |
| 5a — Foundation (TDD) | complete |
| 5b — App Core + Runtime/Edge | complete |
| 5c — Optional Hosts | complete (Gateway, Scheduler, Functions, Uno UI; Blazor pending) |
| 5d — Quality + Delivery | complete (architecture/load/benchmark tests, Dockerfiles, CI/CD, IaC Bicep) |
| 5e — Integration (Auth + AI) | complete (scaffold mode; live Entra/Foundry deployment-only) |

## Infrastructure as Code (IaC)

`infra/` contains the Bicep deployment baseline:

- `main.bicep` — top-level entry
- `modules/` — SQL, Cosmos DB, Service Bus, Storage, Key Vault, App Configuration, Functions, Container Apps + environment, Static Web App, Log Analytics, deploy identity, role assignment, Cosmos RBAC

Deployment plan: [.azure/deployment-plan.md](.azure/deployment-plan.md).

Validate locally: `az bicep build --file infra/main.bicep`.

## Test Harness Architecture

`Test.Endpoints` and `Test.E2E` derive from a shared `WebApplicationFactoryBase<TProgram, TTrxnContext, TQueryContext>` in `Test.Support` (see `src/Test/Test.Support/WebApplicationFactoryBase.cs`). The base handles the standard EF.Packages plumbing swap (interceptor removal, pooled-factory removal, scoped-factory removal, reflection-based `DbContext` creation). Derived classes only specify the test-mode store:

- `Test.Endpoints/CustomApiFactory.cs` — InMemoryDatabase per factory instance
- `Test.E2E/SqlApiFactory.cs` — Testcontainers SQL Server, container managed at the class level

## Outstanding Follow-Ups

Tracked here so the next instruction-set or reference-app PR knows what's pending:

1. **Vulnerability resolution.** Watch upstream releases of `System.Security.Cryptography.Xml` (high) and `OpenTelemetry.Api` (moderate); bump pinned versions in `Directory.Packages.props` once patches ship.
2. **Blazor host.** Phase 5c lists Blazor as pending; the project exists in the solution but is not yet wired into the proof map.
