# HANDOFF — TaskFlow Reference App

## Session Summary

Phases 1–5c complete. 21-project clean-architecture solution with rich domain model, full CRUD services/endpoints, Aspire orchestration, DbContext pooling, FusionCache, middleware pipeline. 162 unit tests green.

## Current State

- **currentPhase:** 5
- **currentSubPhase:** c
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

## Phase 5b Outputs

- 7 service implementations with IRequestContext, ITenantBoundaryValidator, IEntityCacheProvider
- 7 service interfaces with DefaultRequest<T>/DefaultResponse<T> wrappers, PagedResponse<T> (unwrapped)
- 7 mapper classes (entity ↔ DTO)
- 7 endpoint files with full CRUD + search
- App-level types: DefaultRequest<T>, DefaultResponse<T>, AppConstants, ITenantBoundaryValidator, IEntityCacheProvider
- TenantBoundaryValidator, NoOpEntityCacheProvider implementations
- TenantId added to all 6 search filter classes
- 162 unit tests (entity + rule + mapper + service tests) — all green
- Old service stubs removed (replaced by real implementations)
- **Gate: `dotnet build` + `dotnet test --filter "TestCategory=Unit"` — 162 passed, 0 failed**

## Phase 5c Outputs

- **Aspire AppHost**: persistent SQL (password param, data volume, port 38433), persistent Redis, named connection strings (TaskFlowDbContextTrxn, TaskFlowDbContextQuery, Redis1)
- **ServiceDefaults**: `/healthz` (liveness), `/readyz` (readiness, "ready" tag)
- **Database context pooling**: `AddPooledDbContextFactory` + `DbContextScopedFactory` for both Trxn and Query contexts, AuditInterceptor on Trxn, ConnectionNoLockInterceptor, ReadOnly intent on Query
- **Tenant query filters**: automatic `HasQueryFilter` for all `ITenantEntity<Guid>` entities via `BuildTenantFilter`
- **Default data types**: decimal(10,4), datetime2 global conventions
- **FusionCache**: named cache instances from `CacheSettings[]` config, SystemTextJson serializer, Redis L2 + backplane (conditional)
- **Health check**: SqlHealthCheck (readiness) using `IDbContextFactory`
- **Middleware**: SecurityHeadersMiddleware (X-Content-Type-Options, X-Frame-Options, Referrer-Policy), CorrelationIdMiddleware (X-Correlation-Id header), GlobalExceptionHandler (409 concurrency, 422 validation, 404 not-found, 403 forbidden, 500 default)
- **Rate limiting**: per-tenant fixed window (100/min) via `AddRateLimiter`
- **Configuration**: appsettings.json with ConnectionStrings, CacheSettings, FeatureFlags sections
- **Packages**: FusionCache 2.6.0, StackExchange.Redis backplane, Microsoft.Extensions.Caching.StackExchangeRedis
- **Gate: `dotnet build` + `dotnet test --filter "TestCategory=Unit"` — 162 passed, 0 failed**

## Next Phase (Phase 5d — Optional Hosts)

Load `skills/background-services.md`, `skills/function-app.md`, `skills/gateway.md`, `skills/uno-ui.md`, `skills/notifications.md`.
Implement YARP Gateway, Scheduler jobs (TickerQ), Function App triggers, Uno UI (WASM).
**Gate:** Gateway forwards requests, scheduler runs, functions trigger.

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
