# HANDOFF — TaskFlow Reference App

## Session Summary

Phases 1–5e complete (including Uno UI + Quality Gates). 28-project clean-architecture solution with rich domain model, full CRUD services/endpoints, Aspire orchestration, DbContext pooling, FusionCache, middleware pipeline, YARP Gateway, TickerQ Scheduler, Azure Functions (isolated worker), Uno Platform WASM UI with MVUX + Kiota client → Gateway. 186 tests green (174 Unit + 12 Architecture).

## Current State

- **currentPhase:** 5
- **currentSubPhase:** f
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

## Phase 5d Outputs

- **YARP Gateway**: YARP reverse proxy with service-discovery destinations (`https+http://taskflowapi`), `PathRemovePrefix` transform, TokenService (stub) for downstream bearer tokens, `X-Orig-Request` claim relay header (Base64 JSON), CORS policy for Uno UI origins, auth stub (Phase 5f replaces)
- **TickerQ Scheduler (v10.2.5)**: 3 `[TickerFunction]` jobs (OverdueTaskCheck, RecurringTaskGeneration, StaleTaskCleanup), BaseTickerQJob with scoped handler dispatch, 3 handler implementations using `ITaskItemService`, EF Core operational store (SQL Server), cron seeding via `ICronTickerManager<CronTickerEntity>`, optional dashboard
- **Azure Functions (isolated worker v4)**: 4 trigger types — HTTP (health + TaskApiProxy), Timer (StaleTaskCleanup), Blob (ProcessAttachment), ServiceBus topic (ProcessTaskEvent), Bootstrapper DI reuse, `host.json` + `local.settings.json` configured
- **Packages added**: Yarp.ReverseProxy 2.3.0, Microsoft.Extensions.ServiceDiscovery.Yarp 10.1.0, TickerQ 10.2.5 + EFCore + Dashboard, Azure Functions Worker packages
- **Gate: `dotnet build` — 0 errors, `dotnet test --filter "TestCategory=Unit"` — 162 passed, 0 failed**

## Phase 5d Outputs (Uno UI)

- **TaskFlow.Uno** (WASM): Uno.Sdk/6.5.31, single project targeting `net10.0-browserwasm`, builds clean (0 errors, warnings only)
- **TaskFlow.Uno.Core**: Platform-agnostic class library with Business models, services, and API client
- **App.xaml**: `Application` base class with `MaterialToolkitTheme` (Uno.Toolkit.UI.Material)
- **App.xaml.cs**: `Program.Main` entry point (manual, Uno SDK 6.5.x on .NET 10), `CreateBuilder` + `NavigateAsync<Shell>`
- **App.xaml.host.cs**: UseToolkitNavigation, UseHttp with AddKiotaClient → Gateway, CustomAuth scaffold, navigation route registration, service DI
- **Shell**: ExtendedSplashScreen loading container (Chefs pattern)
- **MVUX Models (9)**: ShellModel, MainModel, DashboardModel, TaskListModel, TaskDetailModel, TaskFormModel, CategoryTreeModel, TagManagementModel, SettingsModel
- **Business Services (7 interfaces + 7 implementations)**: ITaskItemApiService, ICategoryApiService, ITagApiService, ICommentApiService, IChecklistItemApiService, IAttachmentApiService, IDashboardService — all wrapping Kiota client → Gateway
- **UI Models (7 records)**: TaskItemModel, CategoryModel, TagModel, CommentModel, ChecklistItemModel, AttachmentModel, DashboardSummary
- **Views/Pages (7 + Shell)**: Shell (splash), MainPage (NavigationView sidebar with region-based nav), DashboardPage (summary cards, overdue counts, recent activity), TaskListPage (filter/sort/search, inline status toggle), TaskDetailPage (comments CRUD, checklist CRUD, attachments, sub-tasks), TaskFormPage (create/edit with validation), CategoryTreePage (hierarchy CRUD), TagManagementPage (CRUD + color), SettingsPage
- **Kiota Client Stub**: TaskFlowApiClient with typed request builders mirroring all API endpoints (`/api/task-items`, `/api/categories`, `/api/tags`, `/api/comments`, `/api/checklist-items`, `/api/attachments`)
- **Mock/Live switch**: `Features:UseMocks` config, `USE_MOCKS` compile constant, `MockHttpMessageHandler` with full canned data
- **Aspire integration**: AppHost registers `taskflowuno` with `WithReference(gateway).WaitFor(gateway)`
- **Solution**: 26 projects in `TaskFlow.slnx` (added `/UI/` folder with TaskFlow.Uno + TaskFlow.Uno.Core)
- **Smoke tests (12)**: MockHttpMessageHandler tests (search/delete/404), TaskItemApiService mapping tests, CategoryApiService tests, DashboardService aggregation test
- **Packages added**: CommunityToolkit.Mvvm 8.4.0 to Directory.Packages.props
- **Build notes**: Uno.Sdk 6.5.31 required (6.0.67 bundles Uno.Wasm.Bootstrap 8.0.23 incompatible with .NET 9+); `<TargetFramework />` clears Directory.Build.props singular TFM; `Program.cs` entry point added manually (SDK auto-generation not triggered on .NET 10); global using for `System.Collections.Immutable` in Uno csproj
- **Gate: `dotnet build TaskFlow.Uno/TaskFlow.Uno.csproj` — 0 errors, `dotnet test --filter "TestCategory=Unit"` — 174 passed, 0 failed**

## Phase 5e Outputs

- **Architecture Tests (12)**: DomainDependencyTests (3 — no deps on Application/Infrastructure/Hosts), ApplicationDependencyTests (4 — Contracts no deps on Infra/Hosts, Services no deps on Infra/Hosts), InfrastructureDependencyTests (2 — Repos no deps on Services/Hosts), ConventionTests (3 — all entities implement ITenantEntity<Guid>, all services implement interface counterpart, all entity properties have private setters)
- **Dockerfiles (4)**: `TaskFlow.Api/Dockerfile`, `TaskFlow.Gateway/Dockerfile`, `TaskFlow.Scheduler/Dockerfile`, `TaskFlow.Functions/Dockerfile` — all multi-stage (restore → publish → runtime), `mcr.microsoft.com/dotnet/aspnet:10.0` runtime, `NUGET_TOKEN` build arg for private feed, `.dockerignore` at repo root
- **CI/CD Pipeline**: `.github/workflows/ci.yml` (PR validation: restore → build → Unit + Architecture + Endpoint tests, optional Integration via workflow_dispatch), `.github/workflows/cd.yml` (main push: build → test → docker build/push via matrix + OIDC Azure auth, deploy placeholder per environment)
- **Load Tests**: `Test/Test.Load/` project with NBomber 6.3.0 + NBomber.Http 6.2.0, TaskItem search throughput + CRUD scenarios, `[TestCategory("Load")]`, `[Ignore]` (manual run only)
- **Benchmarks**: `Test/Test.Benchmarks/` project with BenchmarkDotNet 0.14.0, entity mapping benchmarks (ToDto, round-trip), console runner
- **Packages added**: NBomber 6.3.0, NBomber.Http 6.2.0, BenchmarkDotNet 0.14.0 to Directory.Packages.props
- **Solution**: 28 projects in `TaskFlow.slnx` (added Test.Load + Test.Benchmarks)
- **Gate: `dotnet test --filter "TestCategory=Unit|TestCategory=Architecture"` — 186 passed (174 Unit + 12 Architecture), 0 failed**

## Next Phase (Phase 5f — Authentication)

### Exact Session Start Instructions

1. Read this HANDOFF.md first. Resume from Phase 5f.
2. Read `implementation-plan.md` — Phase 5f section has the task list.
3. Load instruction skills (from the AI-Instructions-NewProject repo):
   - `skills/identity-management.md` — Entra ID, JWT Bearer, role-based policies
   - `skills/security.md` — auth patterns, claim extraction, tenant isolation
   - `skills/api.md` — endpoint auth policy wiring
4. `cd C:\Users\EbenFreeman\source\repos\AI-Instructions-ReferenceApp\src` — all commands run from here.
5. Verify baseline: `dotnet build TaskFlow.slnx` and `dotnet test --filter "TestCategory=Unit|TestCategory=Architecture"` — expect 186 passed.

### Phase 5f Task Breakdown

**5f-1: Conditional Auth Registration**
- `appsettings.json` section presence → JWT Bearer; absent → no-op passthrough
- App still boots in scaffold mode without Entra config

**5f-2: Auth Stub → Real Entra ID JWT Bearer**
- Config-driven JWT Bearer authentication
- Claim extraction precedence: oid > NameIdentifier > sub

**5f-3: Role-Based Policies**
- GlobalAdmin bypass + tenant-matched policies (TenantMember, TenantAdmin)
- StatusTransitionPolicy (role-based transition control)

**5f-4: Gateway Auth**
- User-facing auth (Entra External)
- Claim relay to API via X-Orig-Request header
- Service-to-service tokens: Gateway → API via client credentials (TokenService)

**5f-5: Update Appsettings**
- Entra configuration sections (commented out for scaffold mode)
- ValidateOnStart for auth config

### Gate
`dotnet test --filter "TestCategory=Unit|TestCategory=Architecture"` — all pass, 0 failures. App boots without Entra config.

### Project Structure Reference (28 projects)
```
src/
├── Domain/TaskFlow.Domain.Model/
├── Domain/TaskFlow.Domain.Shared/
├── Application/TaskFlow.Application.Contracts/
├── Application/TaskFlow.Application.Mappers/
├── Application/TaskFlow.Application.Models/
├── Application/TaskFlow.Application.Services/
├── Application/TaskFlow.Application.MessageHandlers/
├── Infrastructure/TaskFlow.Infrastructure.Data/
├── Infrastructure/TaskFlow.Infrastructure.Repositories/
├── TaskFlow.Bootstrapper/
├── TaskFlow.Api/
├── TaskFlow.Scheduler/
├── TaskFlow.Gateway/
├── TaskFlow.Functions/
├── Aspire/AppHost/
├── Aspire/ServiceDefaults/
├── UI/TaskFlow.Uno/          (Uno.Sdk/6.5.31, net10.0-browserwasm)
├── UI/TaskFlow.Uno.Core/     (net10.0, testable business logic)
├── Test/Test.Unit/           (174 tests, TestCategory=Unit)
├── Test/Test.Architecture/   (12 tests, TestCategory=Architecture)
├── Test/Test.Endpoints/      (empty shell)
├── Test/Test.Integration/    (empty shell)
├── Test/Test.Load/           (NBomber, TestCategory=Load, manual run)
├── Test/Test.Benchmarks/     (BenchmarkDotNet, console runner)
├── Test/Test.Support/        (builders, InMemoryDbBuilder, TestConstants)
```

### Build Commands
```powershell
cd C:\Users\EbenFreeman\source\repos\AI-Instructions-ReferenceApp\src
dotnet build TaskFlow.slnx                              # full solution (excludes Uno WASM)
dotnet build TaskFlow.Uno/TaskFlow.Uno.csproj            # Uno WASM (separate, needs Uno.Sdk)
dotnet test --filter "TestCategory=Unit"                 # 174 unit tests
dotnet test --filter "TestCategory=Architecture"         # 12 architecture tests
dotnet test --filter "TestCategory=Unit|TestCategory=Architecture"  # combined gate (186)
```

### Known Constraints
- Uno WASM (`TaskFlow.Uno.csproj`) builds separately — do NOT include in `dotnet build TaskFlow.slnx` unless the machine has Uno.Sdk resolved
- Docker builds require Docker Desktop running — verify availability before attempting
- Load tests (`[Ignore]`) require API host running — manual invocation only
- Benchmarks are a console app — run via `dotnet run -c Release` not `dotnet test`

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
