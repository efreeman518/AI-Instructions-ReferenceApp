# HANDOFF — TaskFlow Reference App

## Session Summary

Phase 1 through Phase 4 (Contract Scaffolding) complete. Full solution structure generated, all 21 projects compile successfully.

## Current State

- **currentPhase:** 5
- **currentSubPhase:** b
- **instructionVersion:** "1.1"
- **contractsScaffolded:** true
- **foundationComplete:** true

## Phase 1 Outputs

- `domain-specification.yaml` — in project root, validated against `schemas/domain-specification.schema.json`

## Phase 2 Outputs

- `resource-implementation.yaml` — in project root, validated against `schemas/resource-implementation.schema.json`

## Phase 3 Outputs

- `implementation-plan.md` — in project root, vertical slice order defined, all questions resolved
- `dotnet-tools.json` — dotnet-ef 10.0.5 installed
- Pre-flight: .NET 10.0.104 SDK verified, dotnet-ef installed

## Phase 4 Outputs

- `src/TaskFlow.slnx` — 21 projects in clean-architecture layout
- Entity shells (7 entities) with NotImplementedException stubs
- Interfaces: 7 service interfaces, 14 repository interfaces (query + trxn)
- DTOs + SearchFilters for all entities  
- Enums: TaskItemStatus, Priority, TaskFeatures (flags), AttachmentOwnerType
- Value objects: DateRange, RecurrencePattern
- Domain events: 7 event records implementing IDomainEvent
- DbContext shells: TaskFlowDbContextTrxn + TaskFlowDbContextQuery
- No-op service stubs for all services
- Bootstrapper RegisterServices.cs with no-op wiring
- Test infrastructure: TestConstants, entity + DTO builders
- Aspire AppHost + ServiceDefaults
- All hosts: Api, Scheduler, Gateway, Functions (shells)
- **Gate: `dotnet build` succeeds — 0 errors**

## Phase 5a Outputs

- All 7 entities implemented with rich domain model (Create, Update, Valid, child management)
- TaskItem status state machine: Open→InProgress→Completed, Open→Cancelled, InProgress→Blocked, Blocked→InProgress, reopen from Completed/Cancelled
- Domain rules infrastructure: IRule<T>, RuleBase<T>, RuleExtensions, TaskItemStatusTransitionRule
- 7 EF configurations (Fluent API): CategoryConfiguration, TagConfiguration, TaskItemConfiguration, CommentConfiguration, ChecklistItemConfiguration, AttachmentConfiguration, TaskItemTagConfiguration
- DbContextTrxn: DbContextBase<string, Guid?>, schema "taskflow", ApplyConfigurationsFromAssembly
- DbContextQuery: inherits Trxn (shared model)
- 14 repositories (7 Query + 7 Trxn) with entity-specific Get/Search methods
- Repository interfaces extended with entity-specific methods
- All 7 entity builders activated (With* methods + Build() calling real Create())
- InMemoryDbBuilder for test infrastructure
- 75 unit tests: entity tests (create, validate, update, children, status transitions) + rule tests
- **Gate: `dotnet build` + `dotnet test --filter "TestCategory=Unit"` — 75 passed, 0 failed**

### Entity Ordering Used
Category → Tag → TaskItem → Comment → ChecklistItem → Attachment → TaskItemTag

## Next Phase (Phase 4 — Contract Scaffolding)

Load `ai/contract-scaffolding.md`, `skills/solution-structure.md`, `skills/package-dependencies.md`, `ai/placeholder-tokens.md`, `support/ef-packages-reference.md`.
Generate full solution structure with interfaces, DTOs, entity shells, test infra, no-op DI stubs.
**Gate:** `dotnet build` succeeds on full solution including test projects.

## Domain Model Summary

### Entities (7)

| Entity | Key Patterns |
|---|---|
| **Category** | Self-referencing hierarchy (ParentCategoryId), 1:M → TaskItems, max 5 levels |
| **Tag** | M:M ↔ TaskItems via TaskItemTag join entity, unique name per tenant |
| **TaskItem** | Root aggregate, self-referencing sub-tasks (ParentTaskItemId, max 3 levels), 1:M → Comments, 1:M → ChecklistItems, M:M ↔ Tags, M:1 → Category, owns DateRange + RecurrencePattern value objects, state machine (Status), flags enum (Features) |
| **Comment** | 1:M child of TaskItem, cascade delete, polymorphic Attachments |
| **ChecklistItem** | 1:M child of TaskItem, cascade delete, orderable, completable |
| **Attachment** | Polymorphic join (OwnerType: TaskItem or Comment), blob storage URI |
| **TaskItemTag** | Explicit M:M bridge entity (TaskItemId + TagId) |

### Relationship Patterns Covered

- Self-referencing: Category (hierarchy), TaskItem (sub-tasks)
- One-to-many: TaskItem → Comments, TaskItem → ChecklistItems, Category → TaskItems
- Many-to-many: TaskItem ↔ Tag via TaskItemTag
- Polymorphic join: Attachment → TaskItem or Comment
- Reference navigation: TaskItem → Category (optional, no ownership)
- Value objects: DateRange, RecurrencePattern

### State Machine

TaskItem.Status: None → Open → InProgress ↔ Blocked → Completed/Cancelled → Open (reopen)
Guard: AllChecklistItemsComplete (on InProgress → Completed)

### Domain Events

TaskItemCreated, TaskItemStatusChanged, TaskItemCompleted, TaskItemRescheduled, TaskItemOverdueSuspected (scheduled), CommentAdded, AttachmentUploaded

### Workflows

OverdueTaskEscalation (orchestrator, with compensation), RecurringTaskGeneration (orchestrator)

### AI Capabilities

Search: TaskItem (hybrid — keyword + semantic + vector)
Agent: TaskAssistant (CRUD + search + summarize via function tools + RAG)

### Cross-Cutting

- Multi-tenant: all entities implement ITenantEntity<Guid>, row-level isolation
- Auth: EntraID (enterprise), scaffold mode for local dev
- Policy matrix: StatusTransitionPolicy (role-based transition control)

## Next Phase (Phase 5b — App Core TDD)

Load `ai/SKILL.md`, `ai/placeholder-tokens.md`, `ai/tdd-protocol.md`, `patterns/api-host-wiring.md`.
Load templates: service-template, endpoint-template, structure-validator-template, data-mapping-template, exception-handler-template.
Load test templates: test-templates-service, test-templates-endpoint.

TDD cycle per entity (Category → Tag → TaskItem → Comment → ChecklistItem → Attachment → TaskItemTag):
1. Write service unit tests (CRUD, search) — RED
2. Implement services, mappers, validators — GREEN
3. Write endpoint integration tests — RED
4. Implement minimal API endpoints — GREEN
5. Replace no-op DI stubs with real implementations

**Gate:** `dotnet build` + `dotnet test --filter "TestCategory=Unit|TestCategory=Endpoint"` passes

## Resource Implementation Summary

### Scaffold Config
- scaffoldMode: full
- testingProfile: comprehensive
- functionProfile: full
- unoProfile: full

### Hosts
- API (primary REST surface)
- Gateway (YARP reverse proxy, user auth, claim relay)
- Scheduler (TickerQ background jobs)
- Function App (Service Bus, Timer, Blob, HTTP triggers)
- Uno UI (WASM, full CRUD + dashboard)

### Infrastructure (all emulator mode)
- SQL Server — all entities, split read/write DbContexts
- Redis — FusionCache L2, backplane for cache sync
- Service Bus — domain events (topic), task commands (queue)
- Azure Storage — blob (attachments), emulator mode
- Cosmos DB — denormalized TaskView projection store, emulator mode

### External Dependency Modes
| Dependency | Mode |
|---|---|
| SQL | emulator |
| Redis | emulator |
| Service Bus | emulator |
| Key Vault | lazy-optional |
| Blob Storage | emulator |
| Cosmos DB | emulator |
| AI Services | deployment-only |

### AI Services (deployment-only, no-op stubs for local)
- Foundry: gpt-4o (agent reasoning), text-embedding-3-small (embeddings)
- Search: AzureAISearch, taskitems-index (hybrid: keyword + semantic + vector)
- Agent: TaskAssistant (ChatClientAgent, function tools, RAG)

### Messaging
- Service Bus topic: DomainEvents (at-least-once, outbox enabled)
- Service Bus queue: TaskCommands (at-least-once)

### Scheduled Jobs
- OverdueTaskCheck (every 6 hours)
- RecurringTaskGeneration (daily)
- StaleTaskCleanup (weekly)

### Functions
- ProcessTaskEvent (Service Bus topic trigger)
- StaleTaskCleanup (timer trigger)
- ProcessAttachment (blob trigger)
- TaskApiProxy (HTTP trigger)

## Target Framework

.NET 10 — all packages at latest versions

## Residual Environment Notes

- No MCP servers required for Phase 1
- Python (pyyaml + jsonschema) used for schema validation
