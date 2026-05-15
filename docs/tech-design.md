# TaskFlow — Technical Design Document

> **Audience**: Developers onboarding to the project  
> **Last updated**: May 2026

---

## Table of Contents

1. [Overview](#1-overview)
2. [C4 Architecture Diagrams](#2-c4-architecture-diagrams)
3. [Software Architecture Layers](#3-software-architecture-layers)
4. [Service Topology](#4-service-topology)
5. [Domain Model](#5-domain-model)
6. [Data Flow Diagrams](#6-data-flow-diagrams)
7. [API Contract Summary](#7-api-contract-summary)
8. [Security Model](#8-security-model)
9. [Deployment Topology](#9-deployment-topology)
10. [Observability](#10-observability)
11. [Audit Strategy](#11-audit-strategy)
12. [Testing Strategy](#12-testing-strategy)
13. [UI Architecture](#13-ui-architecture)
14. [Workflow Orchestration (FlowEngine)](#14-workflow-orchestration-flowengine)

---

## 1. Overview

TaskFlow is a **multi-tenant task management reference application** built on .NET 10 and .NET Aspire. It demonstrates production-grade patterns for building cloud-native distributed systems with Azure backing services.

### Tech Stack

| Layer | Technology |
|-------|-----------|
| **Orchestration** | .NET Aspire (local dev + cloud deployment) |
| **API** | ASP.NET Core Minimal APIs |
| **Gateway** | YARP Reverse Proxy |
| **Background Jobs** | Azure Functions (isolated worker v4), TickerQ Scheduler |
| **UI** | Uno Platform WASM (MVUX), Blazor WASM/Server (MudBlazor, Refit) |
| **Database** | SQL Server (EF Core, dual DbContext) |
| **Cache** | Redis (FusionCache with L1/L2 + backplane) |
| **Messaging** | Azure Service Bus (topics + queues) |
| **Read Model** | Azure Cosmos DB (denormalized projections) |
| **File Storage** | Azure Blob Storage |
| **AI** | Azure AI Search + Azure OpenAI (stubs) |
| **Workflow Orchestration** | EF.FlowEngine 1.0.104 — SQL state store, outbox, circuit breaker, admin API, Blazor dashboard |
| **Auth** | Microsoft Entra ID (External) / Scaffold mode |
| **Observability** | OpenTelemetry (OTLP), Aspire Dashboard |
| **Testing** | MSTest, Moq, NetArchTest, WebApplicationFactory, Testcontainers.MsSql, Aspire.Hosting.Testing, BenchmarkDotNet, NBomber, Playwright |

### Design Principles

- **Domain-Driven Design** — Aggregates, value objects, domain events, bounded contexts
- **CQRS-like** — Separate read/write DbContexts; denormalized Cosmos read model alongside normalized SQL
- **Multi-Tenant First** — Tenant isolation at query filter, service, and authorization layers
- **Event-Driven** — Integration events flow through Service Bus to Azure Functions for async processing
- **Config-Driven Auth** — Single build, multiple deployment profiles (dev scaffold vs Entra ID prod)
- **Emulator-Ready** — All Azure services run as local emulators via Aspire; no cloud account needed for development

---

## 2. C4 Architecture Diagrams

### 2.1 System Context Diagram

Shows the TaskFlow system boundary, its users, and external dependencies.

```mermaid
C4Context
    title TaskFlow — System Context

    Person(user, "TaskFlow User", "Manages tasks, categories, tags")
    Person(admin, "Tenant Admin", "Manages tenant settings")

    System(taskflow, "TaskFlow System", "Multi-tenant task management platform")

    System_Ext(entra, "Microsoft Entra ID", "Identity & access management")
    System_Ext(aisearch, "Azure AI Search", "Hybrid/vector task search")
    System_Ext(openai, "Azure OpenAI", "Agent chat, AI features")

    Rel(user, taskflow, "Uses", "HTTPS")
    Rel(admin, taskflow, "Administers", "HTTPS")
    Rel(taskflow, entra, "Authenticates via", "OAuth 2.0 / OIDC")
    Rel(taskflow, aisearch, "Searches tasks", "REST")
    Rel(taskflow, openai, "AI agent chat", "REST")

    UpdateLayoutConfig($c4ShapeInRow="3", $c4BoundaryInRow="1")
```

> **Diagram legend:** Solid lines = direct dependency. Labels describe the protocol or relationship.

### 2.2 Container Diagram

All deployable units and infrastructure resources with their relationships.

```mermaid
C4Container
    title TaskFlow — Container Diagram

    Person(user, "User", "Browser / Desktop")

    Container_Boundary(ui, "Frontend") {
        Container(uno, "Uno WASM App", "Uno Platform, .NET 10", "Cross-platform UI (browser + desktop)")
        Container(blazor, "Blazor App", "Blazor Server, .NET 10, MudBlazor", "Interactive Server UI — full CRUD, MudBlazor components, Refit client")
    }

    Container_Boundary(backend, "Backend Services") {
        Container(gateway, "API Gateway", "ASP.NET Core, YARP", "Reverse proxy, auth boundary, claims injection")
        Container(api, "TaskFlow API", "ASP.NET Core Minimal APIs", "Core business logic, CRUD, domain events")
        Container(functions, "Azure Functions", "Isolated Worker v4", "Event processing, blob triggers, cleanup")
        Container(scheduler, "Task Scheduler", "ASP.NET Core, TickerQ", "Cron jobs: overdue checks, recurring tasks, cleanup")
    }

    Container_Boundary(data, "Azure/Aspire Platform Services (Data and Messaging)") {
        ContainerDb(sql, "SQL Server", "EF Core", "Normalized domain model (dual DbContext)")
        ContainerDb(redis, "Redis", "FusionCache", "L2 cache + pub/sub backplane")
        ContainerDb(cosmos, "Cosmos DB", "NoSQL", "Denormalized task view projections")
        ContainerQueue(servicebus, "Service Bus", "Topics + Queues", "Domain event transport")
        ContainerDb(blob, "Blob Storage", "Azure Storage", "File attachments")
    }

    Rel(user, uno, "Uses", "HTTPS")
    Rel(uno, gateway, "API calls", "HTTPS + Bearer")
    Rel(gateway, api, "Proxies to", "HTTP + X-Orig-Request")
    Rel(api, sql, "Reads/Writes", "EF Core")
    Rel(api, redis, "Caches", "FusionCache")
    Rel(api, blob, "Stores files", "Azure SDK")
    Rel(api, servicebus, "Publishes events", "AMQP")
    Rel(api, cosmos, "Reads views", "Azure SDK")
    Rel(servicebus, functions, "Triggers", "Topic subscription")
    Rel(blob, functions, "Triggers", "Blob trigger")
    Rel(functions, sql, "Reads/Writes", "EF Core")
    Rel(functions, cosmos, "Projects views", "Azure SDK")
    Rel(scheduler, sql, "Reads/Writes", "EF Core")
    Rel(scheduler, servicebus, "Publishes events", "AMQP")
    Rel(scheduler, redis, "Caches", "FusionCache")
```

> **Diagram legend:** All relationships shown as solid arrows with protocol labels. Blue containers = compute services, orange containers = data/messaging platform services, purple containers = frontend UI. Solid arrows = read/write. Dashed arrows = secondary r/w. Dotted arrows = async trigger.

### 2.3 Component Diagram — TaskFlow API

Internal structure of the core API service.

```mermaid
C4Component
    title TaskFlow API — Component Diagram

    Container_Boundary(api, "TaskFlow API") {

        Component(endpoints, "Minimal API Endpoints", "ASP.NET Core", "HTTP request handling, routing, OpenAPI")
        Component(middleware, "Middleware Pipeline", "ASP.NET Core", "Security headers, correlation, exception handling, rate limiting, auth")
        Component(services, "Application Services", "C#", "Business logic, validation, mapping, tenant enforcement")
        Component(repos_q, "Query Repositories", "EF Core", "Read-optimized, no-tracking, projections")
        Component(repos_t, "Transactional Repositories", "EF Core", "Write operations, audit, optimistic concurrency")
        Component(cache, "Cache Provider", "FusionCache", "L1 in-memory + L2 Redis with backplane")
        Component(events, "Event Publisher", "Service Bus SDK", "Domain event serialization + publishing")
        Component(storage, "Blob Repository", "Azure SDK", "Upload, download, SAS URL generation")
        Component(views, "Task View Repository", "Cosmos SDK", "Denormalized read model queries")
    }

    ContainerDb(sql, "SQL Server", "")
    ContainerDb(redis, "Redis", "")
    ContainerDb(cosmos, "Cosmos DB", "")
    ContainerQueue(sb, "Service Bus", "")
    ContainerDb(blobs, "Blob Storage", "")

    Rel(endpoints, middleware, "Passes through")
    Rel(endpoints, services, "Calls")
    Rel(services, repos_q, "Queries")
    Rel(services, repos_t, "Mutates")
    Rel(services, cache, "Get/Set/Invalidate")
    Rel(services, events, "Publishes integration events")
    Rel(repos_q, sql, "SELECT")
    Rel(repos_t, sql, "INSERT/UPDATE/DELETE")
    Rel(cache, redis, "L2 cache + backplane")
    Rel(events, sb, "Publishes to topic")
    Rel(storage, blobs, "Reads/Writes blobs")
    Rel(views, cosmos, "Queries views")
```

> **Diagram legend:** Arrows indicate direct dependencies. Components inside the API boundary reference each other; external containers represent infrastructure backing services.

---

## 3. Software Architecture Layers

```mermaid
block-beta
    columns 1
    block:ui["UI Layer"]
        u1["TaskFlow.Uno"] u2["TaskFlow.Blazor"]
    end
    block:host["Host Layer (Hosted Services)"]
        h1["TaskFlow.Api"] h2["TaskFlow.Gateway"] h3["TaskFlow.Functions"] h4["TaskFlow.Scheduler"]
    end
    block:boot["TaskFlow.Bootstrapper (DI composition root — not a layer; referenced by Hosts & Tests)"]
        b1["Wires Application + Infrastructure"]
    end
    block:app["Application Layer"]
        a1["Services"] a2["Contracts"] a3["Models"] a4["Mappers"] a5["MessageHandlers"]
    end
    block:domain["Domain Layer"]
        d1["Domain.Model — Entities, Aggregates, Value Objects"] d2["Domain.Shared — Enums, Interfaces"]
    end
    block:infra["Infrastructure Layer"]
        i1["Repositories (EF Core)"] i2["Storage (Blob)"] i2b["Storage (Cosmos)"] i3["AI (Search, OpenAI)"] i4["Data (DbContext)"]
    end

    ui -- "references Application.Models" --> app
    host -- "references" --> boot
    boot -. "wires" .-> app
    boot -. "wires" .-> infra
    app -- "references" --> domain
    infra -- "implements Application.Contracts" --> app
    infra -- "references" --> domain
```

> **Legend:** Solid blue arrows = project references. Orange `implements` = Infrastructure implementing Application.Contracts interfaces. Dotted arrows = DI wiring (Bootstrapper wires layers at runtime, not a compile-time reference).

### Layer Responsibilities

| Layer | Projects | Responsibility | May Reference |
|-------|----------|---------------|---------------|
| **UI** | TaskFlow.Uno, TaskFlow.Blazor | User interfaces — Uno MVUX + Kiota; Blazor MudBlazor + Refit | Application.Models (shared contract) |
| **Host** | Api, Gateway, Functions, Scheduler | HTTP pipeline, function triggers, config | Bootstrapper |
| **Bootstrapper** | TaskFlow.Bootstrapper | DI composition root — wires all layers (not a layer itself; referenced by Hosts and Tests). Also owns FlowEngine registration (`RegisterServices.FlowEngine.cs`) and FE-migration startup task. | Application, Infrastructure |
| **Application** | Services, Contracts, Models, Mappers, MessageHandlers | Use-case services, validation, DTO mapping, tenant enforcement, integration event definitions. `MessageHandlers` also defines `IWorkflowTrigger` for invoking FlowEngine workflows from domain events. | Domain |
| **Domain** | Domain.Model, Domain.Shared | Entities, aggregates, value objects, enums, marker interfaces | Nothing (no outward deps) |
| **Infrastructure** | Repositories, Data, Storage, AI | EF Core, Azure SDK implementations of Application.Contracts interfaces. `Data` also owns the FlowEngine state DbContext (`TaskFlowFlowEngineDbContext`) and its migrations. | Application.Contracts, Domain |

### Dependency Rules (Architecture-Test Enforced)

- **Domain** has zero references to Application, Infrastructure, or Host layers
- **Application.Services** has zero references to Infrastructure or Host layers
- **Infrastructure** implements `Application.Contracts` interfaces (not Domain contracts)
- All tenant entities implement `ITenantEntity<Guid>`
- All services have corresponding interfaces in Contracts
- Entity properties use private setters (encapsulation)

---

## 4. Service Topology

A clean representation of the Aspire-orchestrated service graph (equivalent to the Aspire dashboard graph view):

```mermaid
graph TB
    subgraph ui["Frontend"]
        UNO["🖥️ Uno WASM App<br/><small>localhost:55551</small>"]
        BLAZOR["🌐 Blazor App<br/><small>MudBlazor, Refit<br/>localhost:5200</small>"]
    end

    subgraph services["Backend Services (Aspire-hosted)"]
        GW["🔀 API Gateway<br/><small>YARP Reverse Proxy<br/>localhost:7120</small>"]
        API["⚡ TaskFlow API<br/><small>Minimal APIs<br/>localhost:7067</small>"]
        FN["⚙️ Azure Functions<br/><small>Isolated Worker v4<br/>Aspire-managed port</small>"]
        SCH["🕐 Task Scheduler<br/><small>TickerQ<br/>localhost:7060</small>"]
    end

    subgraph infra["Azure/Aspire Platform Services (Data and Messaging)"]
        SQL[("🗄️ SQL Server<br/><small>port 38433</small>")]
        REDIS[("💾 Redis<br/><small>FusionCache L2</small>")]
        COSMOS[("🌍 Cosmos DB<br/><small>Task View Projections</small>")]
        SB["📬 Service Bus<br/><small>Topics + Queues</small>"]
        BLOB["📦 Blob Storage<br/><small>Attachments</small>"]
    end

    UNO -->|"HTTPS + Bearer"| GW
    BLAZOR -->|"HTTPS + Bearer"| GW
    GW -->|"HTTP + X-Orig-Request"| API

    API -->|"EF Core (R/W)"| SQL
    API -->|"FusionCache"| REDIS
    API -->|"Read views"| COSMOS
    API -->|"Publish events"| SB
    API -->|"Store/retrieve files"| BLOB

    SB -.->|"Topic subscription"| FN
    BLOB -.->|"Blob trigger"| FN
    FN -->|"EF Core (R/W)"| SQL
    FN -->|"Project views"| COSMOS

    SCH -->|"EF Core (R/W)"| SQL
    SCH -->|"FusionCache"| REDIS
    SCH -->|"Publish events"| SB

    classDef service fill:#4a9eff,stroke:#2d7ad6,color:#fff
    classDef infra fill:#ff9f43,stroke:#e67e22,color:#fff
    classDef ui fill:#a29bfe,stroke:#6c5ce7,color:#fff

    class UNO,BLAZOR ui
    class GW,API,FN,SCH service
    class SQL,REDIS,COSMOS,SB,BLOB infra
```

> **Legend:** Solid arrows = direct synchronous dependency ("read/write"). Dotted arrows = asynchronous/event-driven trigger (the service doesn't call directly; infrastructure triggers it via subscription or blob event). Dashed arrows = secondary read/write.

### Hosted Services

| Service | Project | Purpose | Key Dependencies |
|---------|---------|---------|-----------------|
| **API Gateway** | `TaskFlow.Gateway` | Auth boundary, YARP reverse proxy, claims injection | API |
| **TaskFlow API** | `TaskFlow.Api` | Core business logic, CRUD, integration events, FlowEngine admin REST (`/api/flowengine/*`), workflow JSON seeding | SQL, Redis, Cosmos, Service Bus, Blob, FlowEngine state DB |
| **Azure Functions** | `TaskFlow.Functions` | Async event processing, blob processing, timer cleanup | SQL, Cosmos, Service Bus, Blob |
| **Task Scheduler** | `TaskFlow.Scheduler` | Cron jobs via TickerQ (overdue checks, recurring tasks, cleanup) | SQL, Redis, Service Bus |
| **Uno WASM App** | `TaskFlow.Uno` | Cross-platform UI (browser + desktop + mobile) — Uno Platform MVUX | Gateway |
| **Blazor App** | `TaskFlow.Blazor` | Interactive Server UI — MudBlazor, Refit client, full CRUD; also hosts the FlowEngine Dashboard + Designer pages (routes contributed by `EF.FlowEngine.Dashboard` via `AdditionalAssemblies`) | Gateway → API + FlowEngine admin |

---

## 5. Domain Model

### 5.1 Entity Relationship Diagram

```mermaid
erDiagram
    TaskItem ||--o{ Comment : "has many"
    TaskItem ||--o{ ChecklistItem : "has many"
    TaskItem ||--o{ TaskItemTag : "has many"
    TaskItem ||--o{ Attachment : "owns (OwnerType=TaskItem)"
    TaskItem ||--o{ TaskItem : "subtasks (self-ref)"
    TaskItem }o--o| Category : "belongs to"
    TaskItemTag }o--|| Tag : "references"
    Comment ||--o{ Attachment : "owns (OwnerType=Comment)"
```

**Polymorphic Attachment ownership:** `Attachment` uses a discriminator pattern (`OwnerType` enum + `OwnerId` GUID) instead of separate foreign keys. `OwnerType` is either `TaskItem` or `Comment`, and `OwnerId` points to the owning entity's `Id`. This avoids multiple nullable FKs and allows any entity type to own attachments.

```mermaid
erDiagram
    TaskItem {
        guid Id PK
        guid TenantId
        string Title
        string Description
        Priority Priority "None | Low | Medium | High | Critical"
        TaskItemStatus Status "None | Open | InProgress | Blocked | Completed | Cancelled"
        TaskFeatures Features "[Flags] None | Recurring | Reminder | SharedLink"
        decimal EstimatedEffort
        decimal ActualEffort
        DateTimeOffset CompletedDate
        guid CategoryId FK
        guid ParentTaskItemId FK "self-ref for subtasks"
        DateTimeOffset StartDate "via DateRange value object"
        DateTimeOffset DueDate "via DateRange value object"
    }

    Category {
        guid Id PK
        guid TenantId
        string Name
        string Description
        int SortOrder
        bool IsActive
        guid ParentCategoryId FK "self-ref hierarchy"
    }

    Tag {
        guid Id PK
        guid TenantId
        string Name "unique per tenant"
        string Color "hex e.g. #FF5733"
    }

    Comment {
        guid Id PK
        guid TenantId
        string Body
        guid TaskItemId FK
    }

    ChecklistItem {
        guid Id PK
        guid TenantId
        string Title
        bool IsCompleted
        int SortOrder
        DateTimeOffset CompletedDate
        guid TaskItemId FK
    }

    Attachment {
        guid Id PK
        guid TenantId
        string FileName
        string ContentType
        long FileSizeBytes
        string StorageUri
        AttachmentOwnerType OwnerType "TaskItem | Comment"
        guid OwnerId "polymorphic FK"
    }

    TaskItemTag {
        guid Id PK
        guid TenantId
        guid TaskItemId FK
        guid TagId FK
    }
```

### 5.2 Base Entity

All entities inherit from `EntityBase` (NuGet: `EF.Domain`) which provides:

| Property | Type | Purpose |
|----------|------|---------|
| `Id` | `Guid` | Primary key |
| `RowVersion` | `byte[]` | Optimistic concurrency token |

> **Note:** `EntityBase` does **not** define audit properties (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`). Soft-delete (`IsDeleted`) is defined on individual domain entities that support it, not on `EntityBase`. All audit/timestamp tracking is handled by the `AuditInterceptor` — see [Section 11: Audit Strategy](#11-audit-strategy).

All tenant entities also implement `ITenantEntity<Guid>` — enforcing `TenantId` on every row.

### 5.3 Value Objects

| Value Object | Properties | Used By |
|-------------|-----------|---------|
| **DateRange** | `StartDate`, `DueDate` (both `DateTimeOffset?`) | `TaskItem` |
| **RecurrencePattern** | Recurrence interval, frequency, end conditions | `TaskItem` (EF owned type) |

### 5.4 Integration Events

Events are defined in `Application.Contracts.Events` (not Domain layer). Published via `IIntegrationEventPublisher` to Service Bus.

| Event | Trigger | Downstream Effect |
|-------|---------|-------------------|
| `TaskItemCreatedEvent` | New task created | Service Bus → Functions → Cosmos projection |
| `TaskItemStatusChangedEvent` | Status transition | Service Bus → Functions → Cosmos projection |
| `TaskItemCompletedEvent` | Status → Completed | Notifications (future) |
| `TaskItemRescheduledEvent` | Date range updated | Recalculation (future) |
| `TaskItemOverdueSuspectedEvent` | Scheduler detects overdue | Escalation (future) |
| `CommentAddedEvent` | New comment on task | Notifications (future) |
| `AttachmentUploadedEvent` | File uploaded | Metadata extraction via Functions |

---

## 6. Data Flow Diagrams

### 6.1 Request Flow — CRUD Operation

```mermaid
sequenceDiagram
    participant UI as Uno WASM
    participant GW as Gateway (YARP)
    participant API as TaskFlow API
    participant SVC as Application Service
    participant RQ as Query Repository
    participant RT as Transactional Repository
    participant DB as SQL Server
    participant SB as Service Bus

    UI->>GW: POST /api/task-items (Bearer token)
    GW->>GW: Validate token, extract claims
    GW->>API: POST /api/task-items + X-Orig-Request header
    API->>API: Middleware: correlation ID, rate limit, auth
    API->>SVC: TaskItemService.CreateAsync(request)
    SVC->>SVC: TenantBoundaryValidator.EnsureTenantBoundary()
    SVC->>SVC: StructureValidator.ValidateCreate()
    SVC->>SVC: dto.ToEntity(tenantId)
    SVC->>RT: Create(entity)
    RT->>DB: INSERT INTO TaskItems
    SVC->>RT: SaveChangesAsync(ClientWins)
    RT-->>SVC: Success
    SVC->>SB: PublishAsync(TaskItemCreatedEvent)
    SVC-->>API: Result<DefaultResponse<TaskItemDto>>
    API-->>GW: 201 Created + JSON body
    GW-->>UI: 201 Created
```

### 6.2 Event Processing Flow — Cosmos Projection

```mermaid
sequenceDiagram
    participant API as TaskFlow API
    participant SB as Service Bus
    participant FN as Azure Functions
    participant SQL as SQL Server
    participant COSMOS as Cosmos DB

    API->>SB: Publish TaskItemCreatedEvent (topic: DomainEvents)
    SB->>FN: ProcessTaskEvent trigger (subscription: function-processor)
    FN->>FN: Deserialize event, resolve handler
    FN->>SQL: Query full TaskItem + related data
    SQL-->>FN: TaskItem aggregate
    FN->>FN: Build denormalized TaskViewDocument
    FN->>COSMOS: Upsert TaskViewDocument
    COSMOS-->>FN: Success
    Note over COSMOS: Read model now up-to-date
```

### 6.3 Attachment Upload Flow

```mermaid
sequenceDiagram
    participant UI as Uno WASM
    participant API as TaskFlow API
    participant BLOB as Blob Storage
    participant FN as Azure Functions
    participant SQL as SQL Server

    UI->>API: POST /api/attachments/upload (multipart: file, ownerType, ownerId)
    API->>BLOB: Upload blob to container
    BLOB-->>API: Storage URI
    API->>SQL: INSERT Attachment record (fileName, contentType, size, URI)
    SQL-->>API: Success
    API->>API: Publish AttachmentUploadedEvent
    API-->>UI: 201 Created (AttachmentDto)

    Note over BLOB,FN: Async processing
    BLOB->>FN: ProcessAttachment (blob trigger)
    FN->>FN: Validate file, extract metadata
    FN->>SQL: Update Attachment record with metadata
```

### 6.4 Caching Flow — FusionCache L1/L2

```mermaid
sequenceDiagram
    participant SVC as Application Service
    participant L1 as FusionCache L1 (Memory)
    participant L2 as Redis L2 (Backplane)
    participant DB as SQL Server

    SVC->>L1: Get("task:{id}")
    alt L1 Hit
        L1-->>SVC: Cached TaskItemDto
    else L1 Miss
        SVC->>L2: Get("task:{id}")
        alt L2 Hit
            L2-->>SVC: Cached TaskItemDto
            SVC->>L1: Set (populate L1)
        else L2 Miss
            SVC->>DB: Query TaskItem
            DB-->>SVC: TaskItem entity
            SVC->>SVC: Map to DTO
            SVC->>L1: Set("task:{id}", dto)
            SVC->>L2: Set("task:{id}", dto)
        end
    end

    Note over L2: On write: tag-based invalidation<br/>across all instances via backplane
```

---

## 7. API Contract Summary

### 7.1 Entity Endpoints (Consistent CRUD Pattern)

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/{entity}/search` | Paged search with filters and sorting |
| `GET` | `/api/{entity}/{id}` | Get single entity by ID |
| `POST` | `/api/{entity}` | Create new entity |
| `PUT` | `/api/{entity}/{id}` | Update existing entity |
| `DELETE` | `/api/{entity}/{id}` | Delete entity |

**Entities with full CRUD**: `task-items`, `categories`, `tags`, `comments`, `checklist-items`, `attachments`  
**Entities with partial CRUD**: `task-item-tags` (create, get, delete — no search/update)

### 7.2 Special Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/attachments/upload` | Multipart file upload (file, ownerType, ownerId) |
| `GET` | `/api/search/tasks` | AI-powered hybrid search (`?query=...&mode=hybrid&maxResults=10`) |
| `POST` | `/api/agent/chat` | AI agent chat endpoint |
| `GET` | `/api/task-views` | Cosmos DB denormalized views (`?tenantId=...&pageSize=20`) |
| `GET` | `/api/task-views/{id}` | Single task view (`?tenantId=...`) |
| `*` | `/api/flowengine/*` | FlowEngine admin API — instances, registry, circuit-breakers, human tasks. Mounted via `MapFlowEngineAdmin(prefix: "/api/flowengine")`; see [§14 Workflow Orchestration](#14-workflow-orchestration-flowengine) |
| `GET` | `/health` | Health check |
| `GET` | `/alive` | Liveness probe |

### 7.3 Request/Response Envelopes

```
DefaultRequest<TDto>         → Wraps a DTO for create/update operations
DefaultResponse<TDto>        → Single entity response with metadata
SearchRequest<TFilter>       → Paged search: Page, PageSize, SortBy, SortDirection, Filter
PagedResponse<TDto>          → Items[] + TotalCount + pagination metadata
Result<T>                    → Success | Failure(errors) | None (404)
```

### 7.4 Middleware Pipeline (Order of Execution)

```
1. SecurityHeadersMiddleware       — Adds security response headers
2. CorrelationIdMiddleware         — Generates/propagates X-Correlation-Id
3. ExceptionHandler                — Catches exceptions → ProblemDetails (409, 422, 404, 403, 500)
4. RateLimiter                     — Per-tenant, 100 requests/minute
5. CORS                            — Policy "TaskFlowUi" for allowed origins
6. Authentication                  — Scaffold (dev) or JWT Bearer (Entra ID)
7. Authorization                   — Policy-based (see Security Model)
8. GatewayClaimsMiddleware         — Extracts X-Orig-Request header from Gateway
9. OpenAPI / Scalar UI             — API documentation (if enabled)
10. Health + Alive endpoints       — /health, /alive
11. Entity Endpoints               — All CRUD endpoint groups
```

---

## 8. Security Model

### 8.1 Authentication Modes

| Mode | Environment | Mechanism |
|------|-------------|-----------|
| **Scaffold** | Development | Predictable test identity; all requests succeed; no real token validation |
| **Entra ID** | Production | JWT Bearer validation against Microsoft Entra External ID tenant |

The mode is config-driven (`Auth:Mode` in `appsettings.json`), allowing a single build to serve both environments.

### 8.2 Multi-Tenancy Enforcement

Tenant isolation is enforced at **three levels**:

```mermaid
graph LR
    subgraph "Level 1: Database"
        QF["EF Core Query Filters<br/><small>HasQueryFilter(e => e.TenantId == currentTenant)</small>"]
    end
    subgraph "Level 2: Application"
        TB["TenantBoundaryValidator<br/><small>Service-level tenant check before every operation</small>"]
    end
    subgraph "Level 3: Authorization"
        POL["Authorization Policies<br/><small>TenantMatch policy on all endpoints</small>"]
    end

    POL --> TB --> QF
```

- **Query Filters**: Every `ITenantEntity<Guid>` has an automatic EF Core query filter scoped to the current tenant. Cross-tenant data is invisible at the SQL level.
- **Service Validation**: `TenantBoundaryValidator` checks every service call to prevent tenant leakage, even for operations that bypass query filters.
- **Authorization Policies**: Middleware-level enforcement before requests reach services.

### 8.3 Authorization Policies

| Policy | Purpose |
|--------|---------|
| `GlobalAdmin` | System-wide admin operations |
| `TenantMatch` | Request tenant must match authenticated user's tenant |
| `TenantAdmin` | Admin within a specific tenant |
| `StatusTransition` | Controls who can transition task statuses |

### 8.4 Gateway Claims Flow

```
User → Gateway: Bearer {user-token}
Gateway: Validate token (Entra or scaffold)
Gateway: Acquire service-to-service token
Gateway → API: Authorization: Bearer {service-token}
              + X-Orig-Request: Base64({ oid, tenant_id, name, roles })
API: GatewayClaimsMiddleware extracts X-Orig-Request
API: Sets IRequestContext (tenant, user, roles)
```

### 8.5 Rate Limiting

Per-tenant fixed window: **100 requests per minute**. Returns `429 Too Many Requests` with `Retry-After` header.

---

## 9. Deployment Topology

### 9.1 Local Development (Aspire)

All infrastructure runs as persistent emulators — no Azure subscription required.

```mermaid
graph TB
    subgraph aspire["Aspire AppHost (Orchestrator)"]
        direction TB

        subgraph emulators["Azure/Aspire Platform Services (emulated, persistent volumes)"]
            SQL["SQL Server<br/><small>Container, port 38433<br/>Volume: taskflow-sql-data</small>"]
            REDIS["Redis<br/><small>Emulator<br/>Volume: taskflow-redis-data</small>"]
            SB["Service Bus<br/><small>Emulator</small>"]
            BLOB["Azure Storage<br/><small>Emulator</small>"]
            COSMOS["Cosmos DB<br/><small>Emulator</small>"]
        end

        subgraph apps["Application Services"]
            GW["Gateway :7120"]
            API["API :7067"]
            FN["Functions (dynamic)"]
            SCH["Scheduler"]
        end
    end

    UNO["Uno WASM :55551<br/><small>(run separately)</small>"]
    BLAZOR_DEV["Blazor :5200<br/><small>(run separately)</small>"]
    DASH["Aspire Dashboard :17179"]

    UNO --> GW
    BLAZOR_DEV --> GW
    DASH -.->|"Traces, Metrics, Logs"| apps

    UNO ~~~ aspire
    BLAZOR_DEV ~~~ aspire
    DASH ~~~ aspire

    style aspire fill:#1a1a2e,stroke:#16213e,color:#fff
    style emulators fill:#0f3460,stroke:#16213e,color:#fff
    style apps fill:#533483,stroke:#16213e,color:#fff
```

**Start locally**:
```bash
dotnet run --project src/Aspire/AppHost
# Uno WASM and Blazor (with FlowEngine Dashboard) run separately
```

**Service dependencies**: API waits for SQL + Redis. Gateway waits for API. Functions wait for SQL + Storage.

**Startup tasks (Development / Aspire only)**: on first boot the API runs two `IStartupTask` implementations from `TaskFlow.Bootstrapper`:

1. `ApplyEFMigrationsStartup` — applies app-schema migrations against `TaskFlowDbContextTrxn`.
2. `ApplyFlowEngineMigrationsStartup` — applies the FlowEngine `flowengine` schema migrations against `TaskFlowFlowEngineDbContext` (separate migration history table `__EFMigrationsHistory_FlowEngine`).

Both are gated on `ASPNETCORE_ENVIRONMENT=Development` or an Aspire signal and log-and-continue on failure so a missing local DB does not block boot. In production migrations run via the deployment pipeline, not at startup.

After migrations apply, the FlowEngine workflow-seeding hosted service (`AddWorkflowJsonSeeding`) walks `TaskFlow.Api/Workflows/*.json` and upserts each definition into the registry (idempotent — skips existing versions). See [§14.3](#143-shipped-workflows) for the three workflows shipped.

### 9.2 Cloud Deployment (Azure)

```mermaid
graph TB
    subgraph azure["Azure"]
        subgraph compute["Compute"]
            GW_AZ["API Gateway<br/><small>App Service / Container Apps</small>"]
            API_AZ["TaskFlow API<br/><small>App Service / Container Apps</small>"]
            FN_AZ["Azure Functions<br/><small>Flex Consumption</small>"]
            SCH_AZ["Scheduler<br/><small>App Service / Container Apps</small>"]
        end

        subgraph data["Azure Platform Services (Data & Messaging)"]
            SQL_AZ["Azure SQL Database"]
            REDIS_AZ["Azure Cache for Redis"]
            COSMOS_AZ["Azure Cosmos DB"]
            SB_AZ["Azure Service Bus"]
            BLOB_AZ["Azure Blob Storage"]
        end

        subgraph identity["Identity & AI"]
            ENTRA["Microsoft Entra ID"]
            SEARCH["Azure AI Search"]
            OAI["Azure OpenAI"]
        end
    end

    SWA["Static Web Apps<br/><small>Uno WASM + Blazor</small>"]
    USER["Users"]

    USER --> SWA
    SWA --> GW_AZ
    GW_AZ --> API_AZ
    API_AZ --> data
    FN_AZ --> data
    SCH_AZ --> data
    API_AZ --> SEARCH & OAI
    SB_AZ --> FN_AZ
    FN_AZ --> SQL_AZ & COSMOS_AZ
    SCH_AZ --> SQL_AZ & REDIS_AZ & SB_AZ
    API_AZ --> ENTRA
    GW_AZ --> ENTRA

    style azure fill:#0078d4,stroke:#005a9e,color:#fff
```

---

## 10. Observability

### 10.1 OpenTelemetry

Configured via **Aspire Service Defaults** (`Extensions.cs`):

| Signal | Instrumentation |
|--------|----------------|
| **Traces** | ASP.NET Core, HttpClient, custom spans |
| **Metrics** | ASP.NET Core, HttpClient, .NET Runtime, FusionCache |
| **Logs** | Structured logging with `IncludeFormattedMessage` + scopes |
| **Exporter** | OTLP (to Aspire Dashboard locally, Azure Monitor in cloud) |

### 10.2 Health Checks

| Endpoint | Source | Purpose | Checks |
|----------|--------|---------|--------|
| `/healthz` | ServiceDefaults | Liveness | All registered checks |
| `/readyz` | ServiceDefaults | Readiness | Checks tagged `"ready"` (SQL, Redis) |
| `/health` | API | Custom health check | SQL connectivity |
| `/alive` | API | Simple liveness | Always returns 200 |

### 10.3 Correlation Tracking

- `CorrelationIdMiddleware` generates or propagates `X-Correlation-Id` on every request
- Correlation ID flows through: HTTP headers → service calls → integration events → Function triggers → logs
- Enables end-to-end distributed tracing across all services

### 10.4 Aspire Dashboard

Locally at `http://localhost:17179`:
- Resource graph (all services + infra)
- Structured logs with filtering
- Distributed traces (request → service → database)
- Metrics (throughput, latency, errors)

### 10.5 FlowEngine Dashboard

Hosted inside `TaskFlow.Blazor` via `AddFlowEngineDashboard(adminApiBaseUrl: ...)`. Provides a separate, workflow-centric observability surface that complements the Aspire dashboard:

| Page | Purpose |
|------|---------|
| `/workflows/registry` | Active / draft workflow definitions, versions, status |
| `/workflows/new` | Visual designer canvas (`Z.Blazor.Diagrams`) — drag-drop node editing, JSON import/export |
| `/workflows/run` | Manual instance start (pick a workflow, paste params, fire) |
| `/instances` | Running / suspended / completed / faulted instances; click-through to history |
| `/human-tasks` | Open human tasks, claim/approve/reject UI |
| `/circuit-breakers` | Per-key breaker state (closed / half-open / open) |

The dashboard talks to the API over HTTPS via the gateway (`/api/flowengine/*`). Page routes are contributed by the `EF.FlowEngine.Dashboard` assembly through `Routes.razor`'s `AdditionalAssemblies`.

---

## 11. Audit Strategy

### 11.1 Overview

TaskFlow uses an **EF Core SaveChanges interceptor** pattern for entity-level auditing. The `AuditInterceptor<string, Guid?>` (from NuGet package `EF.Data.Interceptors`) intercepts the transactional DbContext and publishes audit records via the internal message bus.

> **Important:** `EntityBase` does **not** define audit properties (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`). All audit persistence flows through the interceptor → message bus → repository pipeline described below.

### 11.2 Audit Flow

```mermaid
sequenceDiagram
    participant EF as EF Core SaveChanges
    participant INT as AuditInterceptor
    participant BUS as IInternalMessageBus<br/>(background Channel)
    participant HDL as AuditHandler
    participant REPO as IAuditLogRepository
    participant TBL as Azure Table Storage<br/>(taskflowaudit)

    EF->>INT: SaveChangesAsync()
    INT->>INT: Capture changed entities
    INT-)BUS: Publish AuditMessage (fire-and-forget)
    Note over INT,BUS: Returns immediately — audit is async
    BUS-->>HDL: Background channel dequeues
    HDL->>REPO: AppendAsync(auditEntries)
    REPO->>TBL: Insert AuditLogTableEntity rows
```

> The `IInternalMessageBus` uses a `System.Threading.Channels` background task. The interceptor enqueues the audit message and **returns immediately**, so `SaveChangesAsync()` is not blocked by audit persistence.

### 11.3 AuditLogTableEntity Schema

| Field | Type | Description |
|-------|------|-------------|
| `Id` | `string` | Unique audit record ID |
| `AuditId` | `string` | Correlation ID grouping related changes |
| `TenantId` | `string` | Tenant identifier |
| `EntityType` | `string` | CLR type name of the audited entity |
| `EntityKey` | `string` | Primary key of the audited entity |
| `Action` | `string` | `Insert`, `Update`, or `Delete` |
| `Status` | `string` | Outcome status |
| `StartTimeTicks` | `long` | Operation start (ticks) |
| `ElapsedTimeTicks` | `long` | Duration (ticks) |
| `RecordedUtc` | `DateTimeOffset` | When the audit was recorded |
| `Metadata` | `string` | Serialized property changes / extra context |
| `Error` | `string` | Error details (if failed) |

**Partition/Row key strategy:** `PartitionKey` = tenant ID (or `"_system"` for non-tenant operations); `RowKey` = reverse-ticks for newest-first queries.

### 11.4 Fallback

When Azure Table Storage is unavailable (e.g., local dev without emulator), the DI container registers `NoOpAuditLogRepository`, which silently discards audit entries.

> **FlowEngine outbox is a separate concern.** The `AuditInterceptor` audits **application** entities (TaskItem, Comment, etc.) and writes to Azure Table Storage. FlowEngine has its own outbox (`flowengine.Outbox`) that stages `message` / `integration` / `agent` side effects produced by workflow nodes; it is persisted by the **same** `SaveChangesAsync` that writes the workflow execution row (atomic save+enqueue — see [§14.4](#144-state-isolation--atomic-outbox)). The two outboxes do not overlap: app-side mutations go through the audit pipeline; workflow-side mutations go through the FE outbox.

### 11.5 Key Source Files

| File | Purpose |
|------|---------|
| `TaskFlow.Bootstrapper/Registration/RegisterServices.Database.cs` | Registers `AuditInterceptor` on Trxn DbContext |
| `TaskFlow.Application.MessageHandlers/AuditHandler.cs` | Handles audit messages from internal bus |
| `TaskFlow.Application.Contracts/Storage/IAuditLogRepository.cs` | Repository contract |
| `TaskFlow.Infrastructure.Storage/AuditLogRepository.cs` | Azure Table Storage implementation |
| `TaskFlow.Infrastructure.Storage/AuditLogTableEntity.cs` | Table entity mapping |
| `TaskFlow.Infrastructure.Storage/NoOpAuditLogRepository.cs` | No-op fallback |

---

## 12. Testing Strategy

### 12.1 Test Pyramid

```mermaid
graph TB
    subgraph pyramid["Test Pyramid"]
        direction BT
        UNIT["🧪 Unit Tests<br/><small>Domain, Mappers, Services, Repos, UI</small>"]
        ENDPOINTS["🌐 Endpoint Tests<br/><small>HTTP cycles via WebApplicationFactory</small>"]
        ARCH["🏗️ Architecture Tests<br/><small>Layer deps, naming conventions, tenant contracts</small>"]
        INT["🔗 Integration Tests<br/><small>Aspire AppHost — real SQL, Service Bus, Storage, Functions</small>"]
        E2E["🔄 E2E Tests<br/><small>Full stack via WebApplicationFactory + Testcontainers SQL</small>"]
        UIE2E["🖱️ UI E2E (Playwright)<br/><small>Real browser against running Blazor / Uno + API</small>"]
        LOAD["📊 Load Tests<br/><small>NBomber throughput scenarios</small>"]
        BENCH["⏱️ Benchmarks<br/><small>BenchmarkDotNet micro-perf</small>"]
    end

    UNIT --- ENDPOINTS --- ARCH --- INT --- E2E --- UIE2E --- LOAD --- BENCH

    style UNIT fill:#27ae60,color:#fff
    style ENDPOINTS fill:#2ecc71,color:#fff
    style ARCH fill:#2980b9,color:#fff
    style INT fill:#8e44ad,color:#fff
    style E2E fill:#9b59b6,color:#fff
    style UIE2E fill:#e84393,color:#fff
    style LOAD fill:#d35400,color:#fff
    style BENCH fill:#c0392b,color:#fff
```

### 12.2 Test Projects

| Project | Purpose | Value | Primary Tools |
|---------|---------|-------|---------------|
| **Test.Unit** | Pure-CPU verification of domain logic, DTO ↔ entity mapping, application service success/failure/conflict paths, in-memory repository CRUD, and Uno API-service mappers. | Fastest feedback loop — millisecond runs, zero infrastructure. Catches regressions in pure logic before slower suites are touched. | MSTest, **Moq**, EF Core InMemory provider |
| **Test.Endpoints** | Drives every HTTP endpoint through the full ASP.NET Core pipeline (middleware → endpoint → service → repo) and asserts status codes (200/201/400/404/409/422), envelopes, and ProblemDetails shapes. | Confirms wire contract without paying for real infrastructure. The whole API surface boots in under a second and runs against an isolated EF InMemory database per factory instance. | MSTest, `Microsoft.AspNetCore.Mvc.Testing` (**WebApplicationFactory**), EF Core InMemory |
| **Test.Architecture** | Asserts compile-time layering and naming rules: Domain has zero outward references; `Application.Services` cannot reference Infrastructure or Hosts; every tenant entity implements `ITenantEntity<Guid>`; services have matching `I*` interfaces; entity setters are private. | Architectural drift is caught by CI rather than by a future code review. Rules are expressed in fluent C#, run with `dotnet test`, and travel with the code instead of living in a wiki. | MSTest, **NetArchTest.Rules** |
| **Test.Integration** | End-to-end verification of cross-service workflows by booting the full **Aspire AppHost** in-process: SQL Server, Service Bus emulator, Azure Table Storage, and (when `func.exe` is on PATH) Azure Functions. Covers EF migrations, repository CRUD with paging, the audit pipeline (interceptor → channel → table storage), and domain-event flow (API publish → Service Bus → Function projection → audit row). | Highest-fidelity tests that still run on a developer laptop. Because Aspire wires the same resources used by `dotnet run`, behavior matches the local dev experience and the cloud deployment. | MSTest, **Aspire.Hosting.Testing** (`DistributedApplicationTestingBuilder`), Testcontainers.MsSql, `Azure.Data.Tables` |
| **Test.E2E** | Multi-endpoint workflow tests (create → search → update → delete) against a real SQL Server container — cases where the InMemory provider's missing semantics (FK constraints, projection plans, concurrency tokens) would hide bugs. | Bridges Test.Endpoints (fast, in-memory) and Test.Integration (full AppHost). Trades the InMemory shortcut for real RDBMS semantics without paying for the rest of the Aspire graph. | MSTest, WebApplicationFactory, **Testcontainers.MsSql** |
| **Test.Load** | NBomber HTTP scenarios — task-search throughput and CRUD generation — with assertions on success rate (≥ 95 %) and P99 latency (< 2 s). | Catches pre-prod throughput regressions and gives a reproducible perf baseline. Tests are `[Ignore]`'d by default (manual run) so they never gate CI on infra availability. | MSTest, **NBomber**, NBomber.Http |
| **Test.Benchmarks** | BenchmarkDotNet console runner exercising hot-path mappers (`ToDto`, `ToEntity`) with `[MemoryDiagnoser]` for allocation tracking. | Quantifies the cost of mapping changes — guards against silent allocation regressions when DTOs are extended. | **BenchmarkDotNet** |
| **Test.Support** | Reusable test infrastructure: `WebApplicationFactoryBase<TProgram, TTrxn, TQuery>`, fluent entity builders (`CategoryBuilder`, `TaskItemBuilder`, `CommentBuilder`, `TagBuilder`), shared constants. | Removes ~100 lines of duplicated DI-rewiring boilerplate from every WebApplicationFactory test project. Builders give tests intent-revealing fixture data. | `Microsoft.AspNetCore.Mvc.Testing`, EF Core InMemory |
| **Test.PlaywrightUI** | Browser-driven UI tests against the running Blazor (`https://localhost:7201`) and Uno WASM (`https://localhost:7069`) frontends — full CRUD lifecycle (create → edit → delete), dashboard smoke, regression scenarios. | The only suite that actually clicks the UI. Catches binding errors, MudBlazor / Uno render bugs, and broken navigation that all server-side tests miss. | **Playwright** (TypeScript, `@playwright/test`) |
| **Test.Integration.FlowEngine** | Workflow-definition validity tier for every JSON file shipped under `TaskFlow.Api/Workflows/`. Asserts JSON → `WorkflowDefinition` deserialization, `WorkflowDefinitionValidator.ValidateAndThrow` passes (unknown node types, dangling edges, malformed schemas), in-memory `IWorkflowRegistry` round-trip preserves node count + status, `WorkflowDefinitionBuilder.FromJson` hydrates id/version/nodes, and the copy-on-build glob does not silently drop files. | Catches authoring mistakes that would otherwise only surface at first-instance-start in dev. Runs without any Aspire stack or Docker — uses `EF.FlowEngine.Testing`'s in-memory registry. Fast (sub-second) and is the first line of defense on every PR that touches a workflow JSON. | MSTest, **EF.FlowEngine.Testing** (`InMemoryWorkflowRegistry`) |

### 12.3 Testing Tools

| Tool | Role | What It Provides |
|------|------|------------------|
| **MSTest** | Test runner (Microsoft) | `[TestClass] / [TestMethod] / [TestCategory]`, `Assert.*`, parallelization (`MSTestParallelize{Assembly,TestClasses}`), `[AssemblyInitialize]` / `[AssemblyCleanup]` for shared fixtures. |
| **Moq** | Mocking framework | Lambda-based fakes for service interfaces (`Mock<IFoo>`); used in `Test.Unit` to isolate services from repositories and external clients. |
| **NetArchTest.Rules** | Architecture assertion DSL | Fluent rules over reflected assemblies — `Types.InAssembly(asm).ShouldNot().HaveDependencyOnAny(...).GetResult()`. Failure surfaces the offending types. Run as ordinary MSTest cases. |
| **BenchmarkDotNet** | Micro-benchmark harness | Warmup / iteration control, statistical noise rejection, `[MemoryDiagnoser]` for GC allocations, console summary tables. Run as `dotnet run -c Release --project src/Test/Test.Benchmarks`. |
| **NBomber + NBomber.Http** | Load-test framework | `Scenario.Create(...)`, `Simulation.Inject(rate, interval, during)` for arrival-rate load, percentile assertions on latency / success. HTTP helpers for request building. |
| **`Microsoft.AspNetCore.Mvc.Testing`** | In-process API host (**WebApplicationFactory**) | Boots `Program.cs` against a `TestServer` — full DI, middleware, routing, model binding — without Kestrel. Returns an `HttpClient` and exposes the `IServiceCollection` for test-time service replacement. |
| **Testcontainers** (`Testcontainers.MsSql`) | Ephemeral Docker containers | Spawns SQL Server 2025 in a throwaway container per fixture; tests use the real engine with no manual install. Disposes the container automatically. |
| **Aspire.Hosting.Testing** | AppHost in-process orchestration | `DistributedApplicationTestingBuilder.CreateAsync(typeof(AppHostProgram))` boots the whole resource graph (SQL, Service Bus, Storage, Functions) in one call. `app.GetConnectionStringAsync(...)` and `app.CreateHttpClient(...)` return wired clients. |
| **Playwright** (`@playwright/test`) | Cross-browser automation | Headless Chrome/Firefox/WebKit, auto-waiting locators, `screenshot: "only-on-failure"`, `trace: "on-first-retry"`. Driven from a TypeScript `playwright.config.ts`. |

### 12.4 Test.Support — `WebApplicationFactoryBase`

`Test.Support` centralizes the WebApplicationFactory plumbing that would otherwise be re-implemented in every HTTP test project. The base class is generic over the host program and both DbContexts:

```csharp
public abstract class WebApplicationFactoryBase<TProgram, TTrxnContext, TQueryContext>
    : WebApplicationFactory<TProgram>
    where TProgram : class
    where TTrxnContext : DbContextBase<string, Guid?>
    where TQueryContext : DbContextBase<string, Guid?>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IHostedService>();
            RemoveStandardEfInfrastructure(services);   // pooled context, audit + nolock interceptors,
                                                        // scoped factory, IDbContextFactory
            RemoveAppSpecificServices(services);        // overridable hook
            var trxnOptions  = BuildTrxnOptions();      // abstract — subclass picks the DB
            var queryOptions = BuildQueryOptions();
            services.AddScoped(_ => CreateContext<TTrxnContext>(trxnOptions));
            services.AddScoped(_ => CreateContext<TQueryContext>(queryOptions));
            services.AddSingleton<IDbContextFactory<TTrxnContext>>(new TestDbContextFactory<TTrxnContext>(trxnOptions));
            services.AddSingleton<IDbContextFactory<TQueryContext>>(new TestDbContextFactory<TQueryContext>(queryOptions));
        });
    }
    protected abstract DbContextOptions BuildTrxnOptions();
    protected abstract DbContextOptions BuildQueryOptions();
}
```

| Concern | How it's handled |
|---------|------------------|
| **Hosted services** | All `IHostedService` registrations stripped — tests never start TickerQ jobs, Service Bus listeners, or background workers by accident. |
| **Pooled DbContext** | Removed via `RemoveDescriptorsByImplPartialName("DbContextPool")` so test contexts get the lifetime the test wants. |
| **Audit + ConnectionNoLock interceptors** | Removed — audit is asserted in dedicated integration tests, not on every endpoint cycle. |
| **DbContext construction** | `TestDbContextFactory<T>` builds contexts via reflection, side-stepping `required` members on `DbContextBase`. |
| **DB choice** | Subclass overrides `BuildTrxnOptions()` / `BuildQueryOptions()` — `UseInMemoryDatabase(...)` for endpoint contract tests, `UseSqlServer(connString)` for E2E. |

Concrete subclasses are tiny:

```csharp
// Test.Endpoints — fast, in-memory
public sealed class CustomApiFactory
    : WebApplicationFactoryBase<Program, TaskFlowDbContextTrxn, TaskFlowDbContextQuery>
{
    private readonly string _dbName = $"TestDb_{Guid.NewGuid()}";
    protected override DbContextOptions BuildTrxnOptions()  =>
        new DbContextOptionsBuilder<TaskFlowDbContextTrxn>().UseInMemoryDatabase(_dbName).Options;
    protected override DbContextOptions BuildQueryOptions() =>
        new DbContextOptionsBuilder<TaskFlowDbContextQuery>().UseInMemoryDatabase(_dbName).Options;
}

// Test.E2E — real SQL via Testcontainers
public sealed class SqlApiFactory
    : WebApplicationFactoryBase<Program, TaskFlowDbContextTrxn, TaskFlowDbContextQuery>
{
    public static async Task StartContainerAsync() { /* MsSqlBuilder(...).StartAsync() once per assembly */ }
    protected override DbContextOptions BuildTrxnOptions()  =>
        new DbContextOptionsBuilder<TaskFlowDbContextTrxn>().UseSqlServer(_connectionString).Options;
    protected override DbContextOptions BuildQueryOptions() =>
        new DbContextOptionsBuilder<TaskFlowDbContextQuery>().UseSqlServer(_connectionString).Options;
}
```

**Why a base class:** every HTTP test project (Endpoints, E2E) needs the same surgical DI rewiring. Centralising it means a fix for an interceptor or pooled-context bug applies to all suites at once — and new test projects only need to choose a database backend.

#### `WebApplicationFactory` itself

`Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<TProgram>` boots the application's `Program.cs` against an in-memory `TestServer`. The full ASP.NET Core pipeline runs (auth, middleware, routing, endpoint binding, exception handling, ProblemDetails) but no socket is opened — calls go through `factory.CreateClient()`, an `HttpClient` wired to the `TestServer`. The factory exposes `ConfigureWebHost(...)` so tests can replace services in the same DI container the host uses, which is how this codebase swaps SQL Server for InMemory or container-backed SQL.

### 12.5 Choosing the Right Host: WebApplicationFactory vs Testcontainers vs Aspire.Hosting.Testing

The three approaches are complementary, not competing — pick by the boundary you want to test:

| Aspect | WebApplicationFactory | Testcontainers | Aspire.Hosting.Testing |
|--------|----------------------|----------------|------------------------|
| **What it boots** | One ASP.NET Core app in-process | One Docker container per resource (SQL, Redis, etc.) | The whole `AppHost` graph in-process — every service + every backing resource Aspire knows about |
| **Networking** | None (TestServer) | Real TCP via Docker | Real TCP between Aspire-managed services |
| **Process model** | Single test process | Test process + N Docker containers | Test process + Aspire orchestrator + N containers / emulators |
| **Startup cost** | < 1 s | Seconds (container pull / start) | Tens of seconds (whole graph) |
| **DI override** | Trivial — same `IServiceCollection` | N/A (configure container, then connection-string into your code) | Limited — you get connection strings & HTTP clients; you don't reach inside other services |
| **Test isolation** | Per-factory instance | Per-fixture container | Shared via `[AssemblyInitialize]` (one Aspire app per test assembly) |
| **Used in this repo** | `Test.Endpoints`, `Test.E2E` (composed with Testcontainers) | `Test.E2E` (`SqlApiFactory`) | `Test.Integration` (`AspireTestHost`) |

**When to reach for which:**

- **WebApplicationFactory** — endpoint contract tests, ProblemDetails shapes, auth / authz routing, anything where the API is the system under test and the database layer can be substituted.
- **Testcontainers** — workflows that depend on real RDBMS semantics (FK cascades, optimistic concurrency, EF projection plans) but only need *one* backing service.
- **Aspire.Hosting.Testing** — multi-service workflows: API publishes a domain event → Service Bus → Function consumes → Cosmos projection → Azure Table audit row appears. No other tool wires that graph for you with a one-liner.

`DistributedApplicationTestingBuilder.CreateAsync(typeof(AppHostProgram))` reflectively loads the AppHost's `Program` type and invokes its `Main` against a builder configured for testing. The resulting `DistributedApplication` exposes `GetConnectionStringAsync(name)` and `CreateHttpClient(serviceName)` to talk to any resource — including ones gated behind environment flags (e.g., `TASKFLOW_INCLUDE_FUNCTIONS=true` in `AspireTestHost` to include the Functions host only when `func.exe` is present on the developer's PATH).

**Aspire test-host best practices** — `AspireTestHost` codifies the canonical recipe from `learn.microsoft.com/dotnet/aspire/testing`:

- Bound every async Aspire call with `.WaitAsync(DefaultTimeout, ct)` (build, start, `GetConnectionStringAsync`, `WaitForResourceHealthyAsync`) so a hung container or stuck DCP step fails fast instead of hanging the run.
- Gate test work on `app.ResourceNotifications.WaitForResourceHealthyAsync(name, ct)` — a resource reaching `Running` does not mean it accepts connections (SQL warm-up, Functions cold-start, Azurite first request).
- Pass parameters via `configureBuilder: (appOptions, hostSettings) => hostSettings.Configuration["Parameters:sql-password"] = ...` instead of mutating process env vars; the AppHost picks them up through normal `IConfiguration` binding.
- Set `appOptions.DisableDashboard = true` (default in the testing builder, but explicit beats implicit).
- Quiet framework chatter with `builder.Services.AddLogging(l => { l.SetMinimumLevel(Information); l.AddFilter("Microsoft.AspNetCore", Warning); l.AddFilter("Aspire.", Warning); })`.

**Aspire tier by reuse** — `Test.Integration` also hosts three classes that only need a single backing service (`MigrationAndRepositoryTests`, `DomainEventPipelineTests`, `AuditLogRepositoryAzuriteTests`). Per the comparison above they would qualify for Testcontainers, but the project piggybacks them on `AspireTestHost`'s shared SQL/Azurite resources rather than starting a parallel Testcontainers stack — saving the second container per assembly. Class-level `<summary>` blocks call this out on each so the choice doesn't read as drift.

### 12.6 Test.PlaywrightUI

A standalone TypeScript Playwright project that drives the **running** UI and API in a real browser. It is *not* `dotnet test`-orchestrated — it lives outside the .NET solution and runs via `npm`.

**Project layout**

```
Test.PlaywrightUI/
├── package.json              # @playwright/test ^1.59.1
├── playwright.config.ts      # Two projects: 'blazor' and 'uno'
├── tests/
│   ├── blazor/task-crud.spec.ts
│   └── uno/
│       ├── task-crud.spec.ts
│       ├── taskflow-task-list-regression.spec.ts
│       └── taskflow-ui.spec.ts
└── utils/
    ├── blazorTestUtils.ts    # MudBlazor-aware helpers (.mud-table, .mud-dialog, ...)
    └── unoTestUtils.ts
```

**Two browser projects**, configured in `playwright.config.ts`:

| Project | `baseURL` | Tests against |
|---------|-----------|---------------|
| `blazor` | `https://localhost:7201` | Blazor MudBlazor app |
| `uno` | `https://localhost:7069` | Uno Platform WASM app |

Both projects use Desktop Chrome, ignore HTTPS errors (self-signed dev cert), capture screenshots on failure, and record traces on first retry. `workers: 1` and `mode: "serial"` in spec files keep state-dependent CRUD steps in order.

**Prerequisites — Playwright drives a real browser, so the full vertical slice must be running before you run the suite:**

1. **API** — `dotnet run --project src/Host/Aspire/AppHost` boots SQL, Service Bus, Storage, the API, the Gateway, and seed data.
2. **UI** — depending on the project being tested:
   - Blazor at `https://localhost:7201` (`dotnet run --project src/UI/TaskFlow.Blazor`)
   - Uno WASM at `https://localhost:7069` (run separately — Uno SDK constraint)
3. **Browsers** — `npx playwright install --with-deps chromium` (one-time).

**Running**

```bash
cd src/Test/Test.PlaywrightUI
npm install
npm run test:blazor          # Blazor project only
npm run test:uno             # Uno project only
npm run test                 # Both
npm run test:full:fast       # No retries, max 4 failures, 120 s timeout — for local triage
```

**How a test reads.** `blazorTestUtils.ts` encodes MudBlazor's selectors so specs stay readable:

```typescript
await waitForApp(page);                              // GET /tasks, wait for heading
await navigateToNewTask(page);                       // click "New Task" → wait for editor
await fillTextField(page, "Title", uniqueTitle("E2E-Create"));
await selectOption(page, "Status", "In Progress");   // MudSelect popover dance
await clickSave(page);
await expectSnackbar(page, "saved");                 // .mud-snackbar
await expectTaskInTable(page, taskTitle);            // .mud-table-body
```

**What this catches.** Server-side suites cannot observe binding errors, missing `@onclick` wiring, broken MudDialog renders, Uno XAML resource errors, or the dialog-confirm-then-API-call sequence. Playwright drives the entire stack the user touches — UI, Gateway, API, database — so any broken link in that chain surfaces as a failed step with a screenshot and trace.

### 12.7 Running Tests

```bash
# Unit + architecture (fast, no infrastructure)
dotnet test --filter "TestCategory=Unit|TestCategory=Architecture"

# Endpoint contract tests (in-memory DB, no Docker)
dotnet test src/Test/Test.Endpoints

# E2E tests (Docker required — Testcontainers SQL)
dotnet test src/Test/Test.E2E

# Integration tests (boots Aspire AppHost — Docker + emulators)
dotnet test --filter "TestCategory=Integration"

# Load tests (manual — needs API host running on localhost:5000)
dotnet test --filter "TestCategory=Load"

# Benchmarks (Release build, console runner)
dotnet run -c Release --project src/Test/Test.Benchmarks

# UI E2E (requires Blazor and/or Uno running, plus API + seed data)
cd src/Test/Test.PlaywrightUI && npm run test
```

---

## 13. UI Architecture

### 13.1 Uno Platform WASM (Primary UI)

```mermaid
graph TB
    subgraph uno["Uno WASM App"]
        SHELL["Shell<br/><small>Navigation frame + splash</small>"]
        MAIN["MainPage<br/><small>Header + SideNav + Content</small>"]

        subgraph pages["Pages"]
            DASH["Dashboard<br/><small>Stats, summaries</small>"]
            TLIST["TaskList<br/><small>Search, filter, sort</small>"]
            TDETAIL["TaskItem<br/><small>Create/edit form</small>"]
            CAT["CategoryTree<br/><small>Hierarchical CRUD</small>"]
            TAGS["TagManagement<br/><small>Tag CRUD</small>"]
            SETTINGS["Settings<br/><small>App preferences</small>"]
        end

        subgraph viewmodels["MVUX Models"]
            DM["DashboardModel"]
            TLM["TaskListModel"]
            TIM["TaskItemPageModel"]
            CTM["CategoryTreeModel"]
            TMM["TagManagementModel"]
            SM["SettingsModel"]
        end

        subgraph services["API Services"]
            KIOTA["Kiota-Generated Client"]
            HTTP["HTTP Delegating Handlers<br/><small>ProblemDetails, BusyTracker</small>"]
        end
    end

    GW["Gateway / YARP"]

    SHELL --> MAIN --> pages
    pages --> viewmodels
    viewmodels --> services
    services -->|"HTTPS"| GW

    style uno fill:#1a1a2e,stroke:#16213e,color:#fff
```

**Key patterns**:

| Pattern | Implementation |
|---------|---------------|
| **MVUX** | Uno's Model-View-Update-eXtended — reactive state management |
| **Kiota Client** | Auto-generated HTTP client from OpenAPI spec |
| **Mock Mode** | `Features:UseMocks=true` → canned 15-task dataset, no network calls |
| **Form Guard** | `IFormGuard` prevents navigation away from unsaved edits |
| **Navigation** | PanelVisibilityNavigator swaps sibling panels; detail pages push onto frame stack |

### 13.2 Blazor WASM/Server UI

```mermaid
graph TB
    subgraph blazor["Blazor WASM/Server App (.NET 10 Interactive Server)"]
        subgraph layout["MudBlazor Layout"]
            APPBAR["MudAppBar<br/><small>🍔 TaskFlow | breadcrumb | 🔄 | 🌙</small>"]
            DRAWER["MudDrawer<br/><small>Dashboard, Tasks, Categories, Tags, Settings</small>"]
        end

        subgraph pages["Pages"]
            P1["Dashboard /"]
            P2["TaskList /tasks"]
            P3["TaskItemPage /tasks/{id}"]
            P4["CategoryTree /categories"]
            P5["TagManagement /tags"]
            P6["Settings /settings"]
        end

        subgraph services["Services"]
            FLOAT["FloatService<br/><small>Request tracking, snackbar, notifications</small>"]
            REFIT["ITaskFlowApiClient<br/><small>Refit + StandardResilienceHandler</small>"]
        end
    end

    GW["Gateway / YARP"]

    layout --> pages
    pages --> FLOAT
    pages --> REFIT
    REFIT -->|"HTTPS"| GW

    style blazor fill:#1a1a2e,stroke:#16213e,color:#fff
```

`TaskFlow.Blazor` is a fully-built **.NET 10 Interactive Server** application using **MudBlazor** components and a **Refit** HTTP client (`ITaskFlowApiClient`) with `AddStandardResilienceHandler()`. It connects to the API through the YARP Gateway.

**Layout:** MudAppBar (hamburger toggle, "TaskFlow" title, breadcrumb via `FloatService.ModuleName`, progress spinner, dark mode toggle) + MudDrawer (nav: Dashboard, Tasks, Categories, Tags, Settings).

**Pages:**

| Route | Page | Purpose |
|-------|------|---------|
| `/` | Dashboard | Overview stats |
| `/tasks` | TaskList | Search, filter, sort tasks |
| `/tasks/new` | TaskItemPage | Create new task |
| `/tasks/{Id:guid}` | TaskItemPage | Edit task (checklist/comments CRUD, dirty-check) |
| `/categories` | CategoryTree | Hierarchical category CRUD |
| `/tags` | TagManagement | Tag CRUD |
| `/settings` | Settings | App preferences |
| `/Error` | Error | Error display |

**Key patterns:**

| Pattern | Implementation |
|---------|---------------|
| **Component Library** | MudBlazor — Material Design components |
| **HTTP Client** | Refit-generated typed client (`ITaskFlowApiClient`) |
| **Resilience** | `AddStandardResilienceHandler()` (Microsoft.Extensions.Http.Resilience) |
| **FloatService** | Centralized request tracking, snackbar error display, change notifications |
| **Dirty Check** | Form change tracking with navigation guard |

### 13.3 Gateway as BFF

The YARP Gateway acts as a **Backend-for-Frontend (BFF)**:
- Handles user authentication (Entra ID or scaffold)
- Acquires service-to-service tokens for downstream API calls
- Injects `X-Orig-Request` with user claims for the API
- CORS configured for UI origins

---

## 14. Workflow Orchestration (FlowEngine)

TaskFlow embeds **EF.FlowEngine 1.0.104** as a long-running, durable, human-in-the-loop orchestration runtime for AI-driven scenarios. It complements — does not replace — the existing CRUD API, domain events, and TickerQ scheduler:

- **Domain events + Service Bus + Functions** still own per-event side effects (Cosmos projection, AI search indexing, blob processing).
- **TickerQ scheduler** still owns timer-driven cron jobs (overdue checks, recurring task generation, stale cleanup).
- **FlowEngine** owns multi-step, stateful, branching workflows that need to wait — for an AI agent, for a human approval, for a downstream call — and resume on the same instance across process restarts.

### 14.1 Why FlowEngine

| Capability | What it gives the reference app |
|---|---|
| **Stateful suspend/resume** | A workflow waiting on a 24-hour human approval survives API restarts, deploys, and scale-out. |
| **AI agent nodes** | `agent` node type wraps Azure OpenAI with output-schema validation, retry, idempotency keys, prompt versioning. |
| **Human task nodes** | `human` node type produces durable records (assignee role, due date, quorum, escalation) consumed by the dashboard's human-task UI. |
| **Saga compensation** | `compensationNodeId` on a node provides an inverse action invoked when a later node in the same instance faults. |
| **Atomic outbox** | `message` / `integration` / `agent` side effects are staged in the same `SaveChangesAsync` that persists workflow state — no torn-write between state save and external dispatch. |
| **Circuit breaker** | Per-key durable breaker state survives replicas/restarts so a single instance failing doesn't reset the breaker for the others. |
| **Admin API + Dashboard** | Out-of-box REST + Blazor UI for registry / instances / human tasks / breakers — operators don't have to build their own. |

### 14.2 Packages and Layer Placement

All 13 FlowEngine packages are pinned at the same version in `Directory.Packages.props`:

| Package | Project that references it | Purpose |
|---|---|---|
| `EF.FlowEngine` | Bootstrapper, App.MessageHandlers, Test.Integration.FlowEngine | Core runtime: engine, executor pipeline, definition model, built-in node executors (auto-registered in 1.0.104). |
| `EF.FlowEngine.StateStore.Sql` | Infrastructure.Data | `IFlowEngineStateDbContext` mixin + `SqlExecutionStateStore`. |
| `EF.FlowEngine.Locks.Sql` | Bootstrapper | SQL-backed distributed lock provider for engine sweeps + leases. |
| `EF.FlowEngine.WorkflowRegistry.Sql` | Bootstrapper, Infrastructure.Data | `IWorkflowRegistry` over SQL. |
| `EF.FlowEngine.HumanTaskStore.Sql` | Bootstrapper, Infrastructure.Data | Human-task durable queue. |
| `EF.FlowEngine.Outbox.Sql` | Bootstrapper, Infrastructure.Data | `IFlowEngineOutboxDbContext` mixin — atomic state+outbox save. |
| `EF.FlowEngine.CircuitBreaker.Sql` | Bootstrapper, Infrastructure.Data | `IFlowEngineCircuitBreakerDbContext` mixin — durable breaker state. |
| `EF.FlowEngine.Clients.Http` | Bootstrapper | Resilient HTTP client for `integration` nodes. |
| `EF.FlowEngine.Clients.ServiceBus` | Bootstrapper | Service Bus client for `message` nodes. |
| `EF.FlowEngine.Clients.OpenAI` | Bootstrapper | Azure OpenAI client for `agent` nodes. |
| `EF.FlowEngine.AdminApi` | Bootstrapper, TaskFlow.Api | REST endpoints under `/api/flowengine/*` + auth policies. |
| `EF.FlowEngine.Dashboard` | TaskFlow.Blazor | Blazor pages (registry, designer, run, instances, human tasks, breakers). |
| `EF.FlowEngine.Testing` | Test.Integration.FlowEngine | In-memory registry and helpers for fast unit/integration tests. |

Registration entry point: `RegisterServices.FlowEngine.cs` (`TaskFlow.Bootstrapper`), called from `RegisterApplicationServices()` after the AI services. The DI surface composes engine + state + locks + registry + human-task + outbox + circuit-breaker + connector clients + JSON seeding + admin policies in a single fluent chain.

### 14.3 Shipped Workflows

Three workflow JSONs ship under `TaskFlow.Api/Workflows/` and are seeded at startup by the FlowEngine hosted seeding service:

```mermaid
graph LR
    subgraph triage["ai-task-triage (1.0.0)"]
        T1["n-classify<br/>(agent)"] --> T2["n-priority-switch<br/>(decision)"]
        T2 -->|"Critical"| T3["n-quorum-approval<br/>(human, 2-of-3, 24h)"]
        T2 -->|"Default"| T4["n-apply-priority<br/>(integration → PATCH)"]
        T3 -->|"Approved"| T4
        T3 -->|"Rejected"| T5["n-compensate-reject<br/>(integration → comment)"]
        T4 --> T6["n-publish-event<br/>(message)"]
    end
```

```mermaid
graph LR
    subgraph decomposer["ai-task-decomposer (1.0.0)"]
        D1["n-propose-subtasks<br/>(agent)"] --> D2["n-approval-gate<br/>(decision)"]
        D2 -->|"requireApproval"| D3["n-human-review<br/>(human, 1, 24h)"]
        D2 -->|"auto-accept"| D4["n-create-subtasks<br/>(loop → POST)"]
        D3 --> D4
        D4 --> D5["n-publish-decomposed<br/>(message)"]
    end
```

```mermaid
graph LR
    subgraph compliance["compliance-check (1.0.0)"]
        C1["n-query-due<br/>(query: tag=compliance, due&lt;windowDays)"] --> C2["n-loop-each<br/>(parallel, max 5)"]
        C2 --> C3["n-fetch-evidence<br/>(document)"]
        C3 --> C4["n-extract<br/>(agent)"]
        C4 --> C5["n-decide<br/>(decision)"]
        C5 -->|"expired"| C6["n-escalate<br/>(human, compliance-officer, 48h)"]
        C5 -->|"due-soon"| C7["n-remind<br/>(message comment)"]
    end
```

| Workflow | Trigger | Params | Notable patterns |
|---|---|---|---|
| **ai-task-triage** | Manual today; intended to fire on `TaskItemCreatedEvent` via `IWorkflowTrigger` (see §14.6) | `tenantId`, `taskId`, `description` (required) | 2-of-3 human quorum, 12 h escalation, saga `compensationNodeId` revert on downstream fault, idempotency keys on every side-effect node |
| **ai-task-decomposer** | Manual / dashboard | `tenantId`, `taskId`, `description`, `requireApproval` (optional) | Conditional human review, sequential `loop` to create N children via API |
| **compliance-check** | Manual / dashboard / future cron via TickerQ | `tenantId`, `windowDays` (default 7) | Parallel `loop` with bounded concurrency (max 5), `query` node using FilterBuilder, `document` node for evidence retrieval |

All three are validated at every build by `Test.Integration.FlowEngine` (see §12.2).

### 14.4 State Isolation & Atomic Outbox

FlowEngine state lives in a **separate `flowengine` schema on the same SQL Server connection**, owned by `TaskFlowFlowEngineDbContext` (sealed, in `Infrastructure.Data`). This is **Variant A** of the deployment-layout decision (same DB, separate schema):

```mermaid
graph TB
    subgraph sql["SQL Server (single instance, single connection string)"]
        subgraph app["dbo schema"]
            A1[("TaskItems")]
            A2[("Categories, Tags, ...")]
            A3[("__EFMigrationsHistory")]
        end
        subgraph fe["flowengine schema"]
            F1[("Workflows")]
            F2[("Executions")]
            F3[("HumanTasks")]
            F4[("ChildSignals")]
            F5[("Outbox")]
            F6[("CircuitBreakers")]
            F7[("__EFMigrationsHistory_FlowEngine")]
        end
    end

    TRX["TaskFlowDbContextTrxn"] --> app
    QRY["TaskFlowDbContextQuery"] --> app
    FECTX["TaskFlowFlowEngineDbContext<br/>(IFlowEngineStateDbContext<br/>+ IFlowEngineOutboxDbContext<br/>+ IFlowEngineCircuitBreakerDbContext)"] --> fe

    style app fill:#0078d4,stroke:#005a9e,color:#fff
    style fe fill:#8e44ad,stroke:#6c3483,color:#fff
```

**Why a separate DbContext rather than mixing FlowEngine entities into the existing transactional context:**

- TaskFlow's primary DbContext (`TaskFlowDbContextTrxn`) inherits from `EF.Data.DbContextBase<TUser,TKey>` for the audit interceptor. FlowEngine's mixin contexts (`FlowEngineOutboxDbContext`, etc.) are abstract bases — multi-inheritance is impossible.
- FlowEngine's interface-composition pattern (`IFlowEngineStateDbContext` + `IFlowEngineOutboxDbContext` + `IFlowEngineCircuitBreakerDbContext`) lets a single fresh DbContext declare all three roles without subclass conflict. `TaskFlowFlowEngineDbContext` is that DbContext.
- Separate migration history (`__EFMigrationsHistory_FlowEngine`, configured in `ConfigureFlowEngineSqlOptions`) keeps the two schemas evolvable independently.

**Atomic outbox is preserved.** Because state, outbox, and circuit-breaker tables all live in `TaskFlowFlowEngineDbContext`, FlowEngine's `SqlExecutionStateStore.SaveWithOutboxAsync` writes the workflow execution row and the outbox rows in a single `SaveChangesAsync`. There is no window where a node's external side effect is committed without the state advance, or vice versa. This is the gain over Variant B/C (separate DB) and is the reason Variant A was selected — see [DESIGN-DECISIONS.md D-016](../.scaffold/DESIGN-DECISIONS.md).

### 14.5 Connector Wiring

Three connector clients are registered in `AddTaskFlowConnectorClients`:

| `clientRef` | Type | Wiring |
|---|---|---|
| `taskflow-api` | Resilient HTTP | Base URL = `FlowEngine:TaskFlowApiBaseUrl` ?? `Gateway:BaseUrl`. Self-call — workflows mutate TaskItems through the public API to preserve auth, validation, audit, and event publishing. Used by `n-apply-priority`, `n-compensate-reject`, `n-create-subtasks`, `n-revert-priority`. |
| `integration-events` | Service Bus | Connection from `ServiceBus1`; topic from `FlowEngine:ServiceBusTopic` (default `taskflow-integration-events`). Registers only when the connection string is present. Used by `n-publish-event`, `n-publish-decomposed`. |
| `ai-agent` | Azure OpenAI | Resolves the existing DI-registered `AzureOpenAIClient` from `Infrastructure.AI` via factory lambda; reads `TaskFlowAiSettings:ChatDeployment` (default `gpt-4o`) and `:FoundryEndpoint`. Registers only when `FoundryEndpoint` is set. Used by `n-classify`, `n-propose-subtasks`, `n-extract`. |

The agent-client wiring is the integration point with the existing AI stack: FlowEngine does not duplicate the OpenAI client; it borrows the one already registered in `Infrastructure.AI.AddAiServices()`. When `FoundryEndpoint` is absent the `agent` nodes will not register and any workflow with an `agent` step will fault on `n-classify` — that's the expected scaffold-mode posture.

### 14.6 Workflow Triggering

`Application.MessageHandlers.WorkflowTriggerHandler` implements `IWorkflowTrigger` with a single method `OnTaskItemCreatedAsync(TaskItemCreatedEvent)` that calls `engine.StartBackgroundAsync(StartRequest { WorkflowId = "ai-task-triage", ... })`.

> **It is intentionally not wired to `IInternalMessageBus` today.** `TaskItemCreatedEvent` is an integration event traveling over Service Bus, not an in-process `IMessage`. The class exists as a one-line addition wherever the event is raised — typically in `TaskItemService` after `eventPublisher.PublishAsync`, or in a custom Service Bus subscriber inside `TaskFlow.Functions`. For the reference-app demo, manual invocation via the dashboard's `/workflows/run` page is sufficient.

Wiring options when a downstream consumer wants automatic triggering:

1. **Service Bus subscriber in `TaskFlow.Functions`** — add a topic subscription, deserialize `TaskItemCreatedEvent`, call `IWorkflowTrigger.OnTaskItemCreatedAsync`. Preserves the existing event-driven architecture and keeps the API host free of workflow start latency.
2. **Inline call in `TaskItemService`** — DI-resolve `IWorkflowTrigger`, call after `eventPublisher.PublishAsync`. Simpler but ties the request thread to engine startup.
3. **TickerQ job for `compliance-check`** — a cron-triggered scheduler job that calls `engine.StartBackgroundAsync` with the `compliance-check` workflow id and a fresh `windowDays` param.

### 14.7 Admin API and Auth

`MapFlowEngineAdmin(prefix: "/api/flowengine")` (called in `WebApplicationBuilderExtensions.cs`) mounts the REST surface from `EF.FlowEngine.AdminApi`:

| Route group | Purpose |
|---|---|
| `/api/flowengine/workflows` | Workflow registry CRUD (list, get, transition Draft↔Active↔Retired) |
| `/api/flowengine/instances` | Instance list/get/start/cancel/replay; history projection |
| `/api/flowengine/human-tasks` | Human-task list, claim, complete, reject |
| `/api/flowengine/circuit-breakers` | Inspect breaker state per key; manual reset |

Authentication and authorization use the same pipeline as the rest of the API (`Bearer` + tenant-match policies in production; scaffold mode locally). `AddFlowEngineAdminPolicies()` registers the policy names used by the package's endpoint metadata. The gateway forwards the user's `Authorization` header through to the API; the Blazor Dashboard calls the gateway, not the API directly, so user identity travels end-to-end without bespoke header forwarding.

### 14.8 Operational Notes

- **First-instance-start gotcha.** If a workflow JSON references a `clientRef` that hasn't been registered (e.g. `ai-agent` when `FoundryEndpoint` is unset), the failure surfaces on the first instance start, not at boot. The `Test.Integration.FlowEngine` suite validates definition shape but cannot validate connector registration — that requires a live AppHost. Confirm via the demo verification checklist in §14.9.
- **Sweep cadence.** Engine options: `SweepInterval=30s`, `SweepBatchSize=50`, `DefaultLeaseDuration=30s`, `LeaseRenewalInterval=13s`. Tuned for the reference app's load profile; production deployments should profile against expected concurrent-instance counts.
- **Replicas.** The SQL lock provider lets multiple API replicas safely share the engine; only one replica leases an instance at a time. The Dashboard does not lease anything — it's pure read-side over the admin API.
- **Backpressure.** Outbox publishing runs on a background drain; if Service Bus is unavailable the outbox grows. Monitor `flowengine.Outbox` row count in the dashboard / a metric.

### 14.9 Demo Verification

With the Aspire AppHost running, the gateway URL visible in the Aspire dashboard:

```bash
GW="https://localhost:<gateway-port>"

# 1. Verify seeding ran — three workflows present, status Active
curl -sk "$GW/api/flowengine/workflows" | jq '.[] | { id, version, status }'

# 2. Verify the instance list is reachable
curl -sk "$GW/api/flowengine/instances?Status=Running" | jq '. | length'

# 3. Start a manual triage run
curl -sk -X POST "$GW/api/flowengine/instances/start" -H "Content-Type: application/json" -d '{
  "workflowId": "ai-task-triage",
  "params": {
    "tenantId":    "11111111-1111-1111-1111-111111111111",
    "taskId":      "22222222-2222-2222-2222-222222222222",
    "description": "Sample triage task for the demo run"
  },
  "tenantId": "11111111-1111-1111-1111-111111111111"
}' | jq

# 4. Inspect instance state
INSTANCE_ID="<paste from step 3>"
curl -sk "$GW/api/flowengine/instances/$INSTANCE_ID" | jq '{ status, history }'
```

Browser verification (Blazor + Dashboard, run separately from AppHost):

1. `https://<blazor-host>/workflows/registry` — three workflows Active.
2. `/workflows/new` — drag a few tiles; Import JSON of `ai-task-triage.json` and confirm parse.
3. `/workflows/run` — pick `ai-task-triage`, paste params, fire; see it appear under `/instances`.
4. `/human-tasks` — only populated once an instance reaches a `human` node (the Critical branch of triage, or the optional review in decomposer). Requires `FoundryEndpoint` configured for the upstream `agent` step to succeed.

**Without `FoundryEndpoint` configured**, the `n-classify` agent step will fault and the instance will land at `n-faulted` immediately — that's the expected no-AI scaffold posture and matches the reference app's "no-op stub" pattern.

---

## Appendix: Azure Functions & Scheduler Details

### Azure Functions

| Function | Trigger | Binding | Purpose |
|----------|---------|---------|---------|
| `HealthCheck` | HTTP GET `/health` | Anonymous | Health probe |
| `TaskApiProxy` | HTTP GET `/tasks` | Function key | Read-only task query (placeholder) |
| `ProcessTaskEvent` | Service Bus Topic | `DomainEvents` topic, `function-processor` subscription | Projects task events to Cosmos DB read model |
| `ProcessAttachment` | Blob | Attachment container | Validates files, extracts metadata, updates Attachment record |
| `StaleTaskCleanup` | Timer | Config-driven cron | Deletes cancelled/stale tasks older than 90 days |

### Scheduler Jobs (TickerQ)

| Job | Schedule | Purpose |
|-----|----------|---------|
| `OverdueTaskCheck` | Every 6 hours | Finds overdue tasks → publishes `TaskItemOverdueSuspectedEvent` |
| `RecurringTaskGeneration` | Daily 2:00 AM UTC | Generates new task instances from recurring patterns |
| `StaleTaskCleanup` | Weekly Sunday 3:00 AM UTC | Archives/soft-deletes old cancelled tasks |

TickerQ uses an EF Core operational store (`TickerQDbContext`, schema `"ticker"`) for job persistence when `Scheduling:UsePersistence=true`.
