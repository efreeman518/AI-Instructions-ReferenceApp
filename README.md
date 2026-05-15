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

**Workflow orchestration:** EF.FlowEngine 1.0.104 — three AI-driven workflows (`ai-task-triage`, `ai-task-decomposer`, `compliance-check`) with human-in-the-loop, saga compensation, and atomic outbox; Blazor-hosted Dashboard + Designer; admin REST at `/api/flowengine/*`. See [tech-design.md §14](docs/tech-design.md#14-workflow-orchestration-flowengine).

Multi-tenant (row-level tenancy). Event-driven async via Service Bus. IaC via Bicep (`infra/`).

**Detailed docs:** [tech-design.md](docs/tech-design.md) · [DESIGN-DECISIONS.md](.scaffold/DESIGN-DECISIONS.md) · [UBIQUITOUS-LANGUAGE.md](.scaffold/UBIQUITOUS-LANGUAGE.md)

## Phase 1 Alignment Artifacts

- `.scaffold/domain-specification.yaml`
- `.scaffold/UBIQUITOUS-LANGUAGE.md`
- `.scaffold/DESIGN-DECISIONS.md`
