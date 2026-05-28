# AI-Instructions-ReferenceApp

Reference implementation for [AI-Instructions-Scaffold](https://github.com/efreeman518/AI-Instructions-Scaffold).

## Description

**TaskFlow** - a production-grade reference app demonstrating AI-assisted development patterns for a multi-tenant task management system. Built with Clean Architecture and Domain-Driven Design, running local with Aspire, deployable to Azure.

## Architecture

| Layer | Path | Responsibility |
|-------|------|----------------|
| Domain | `src/Domain/` | Aggregates, entities, domain events |
| Application | `src/Application/` | Use cases, service contracts, message handlers |
| Infrastructure | `src/Infrastructure/` | EF Core, Azure AI, Storage, Repos |
| Host | `src/Host/` | API, Functions, Scheduler, Gateway, Aspire |

**Azure services:** SQL Server, Cosmos DB, Service Bus, Blob Storage, Azure AI Search, Microsoft Foundry.

**Workflow orchestration:** EF.FlowEngine 1.0.104 - three AI-driven workflows (`ai-task-triage`, `ai-task-decomposer`, `compliance-check`) with human-in-the-loop, saga compensation, and atomic outbox; Blazor-hosted Dashboard + Designer; admin REST at `/api/flowengine/*`. See [tech-design.md Section 14](docs/tech-design.md#14-workflow-orchestration-flowengine).

Multi-tenant (row-level tenancy). Event-driven async via Service Bus. IaC via Bicep (`infra/`).

**Detailed docs:** [tech-design.md](docs/tech-design.md) - [DESIGN-DECISIONS.md](.scaffold/DESIGN-DECISIONS.md) - [UBIQUITOUS-LANGUAGE.md](.scaffold/UBIQUITOUS-LANGUAGE.md)

## Local Mobile UI Tests

TaskFlow mobile smoke tests use MSTest + Appium for the Uno Android/iOS heads. Android runs locally on Windows; iOS requires macOS or a Mac host with Xcode.

Before building the Android package, restore the Uno app with all mobile targets included:

```powershell
rtk dotnet restore src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj -p:BuildAllUnoTargets=true
rtk dotnet build src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj -p:TargetFrameworkOverride=net10.0-android -p:UseMocks=true --no-restore -m:1
```

The explicit `BuildAllUnoTargets=true` restore is required because the Uno project defaults to a fast Wasm-only restore for local web work. Android/Appium runs need the platform Skia runtime packages in the NuGet asset graph.

Full Appium setup and run commands live in [src/Test/Test.Mobile/README.md](src/Test/Test.Mobile/README.md).

## Mutation Testing

TaskFlow has a focused Stryker.NET sample project at [src/Test/Test.Mutation](src/Test/Test.Mutation/README.md). It mutates selected domain files and runs MSTest samples that show boundary, failure-message, status-transition, and idempotency checks.

```powershell
rtk dotnet tool restore
rtk dotnet test src/Test/Test.Mutation/Test.Mutation.csproj
```

Run Stryker from `src/Test/Test.Mutation`:

```powershell
rtk dotnet tool run dotnet-stryker
```

## Phase 1 Alignment Artifacts

- `.scaffold/domain-specification.yaml`
- `.scaffold/UBIQUITOUS-LANGUAGE.md`
- `.scaffold/DESIGN-DECISIONS.md`
