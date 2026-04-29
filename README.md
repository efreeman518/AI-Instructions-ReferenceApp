# AI-Instructions-ReferenceApp

Reference implementation for [AI-Instructions-NewProject](https://github.com/efreeman518/AI-Instructions-NewProject).

## Description

**TaskFlow** — a production-grade reference app demonstrating AI-assisted development patterns for a multi-tenant task management system. Built with Clean Architecture and Domain-Driven Design, deployed to Azure.

## Architecture

| Layer | Path | Responsibility |
|-------|------|----------------|
| Domain | `src/Domain/` | Aggregates, entities, domain events |
| Application | `src/Application/` | Use cases, service contracts, message handlers |
| Infrastructure | `src/Infrastructure/` | EF Core, Azure AI, Storage, Repos |
| Host | `src/Host/` | API, Functions, Scheduler, Gateway, Aspire |

**Azure services:** SQL Server, Cosmos DB, Service Bus, Blob Storage, Azure AI Search, Azure OpenAI.

Multi-tenant (row-level tenancy). Event-driven async via Service Bus. IaC via Bicep (`infra/`).

**Detailed docs:** [tech-design.md](docs/tech-design.md) · [DESIGN-DECISIONS.md](DESIGN-DECISIONS.md) · [UBIQUITOUS-LANGUAGE.md](UBIQUITOUS-LANGUAGE.md)

## Phase 1 Alignment Artifacts

- `domain-specification.yaml`
- `UBIQUITOUS-LANGUAGE.md`
- `DESIGN-DECISIONS.md`
