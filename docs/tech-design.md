# TaskFlow — Technical Design Document

> **Audience**: Developers onboarding to the project  
> **Last updated**: April 2026

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
| **Auth** | Microsoft Entra ID (External) / Scaffold mode |
| **Observability** | OpenTelemetry (OTLP), Aspire Dashboard |
| **Testing** | MSTest & TestContainers, NetArchTest, WebApplicationFactory, BenchmarkDotNet, NBomber |

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
| **Bootstrapper** | TaskFlow.Bootstrapper | DI composition root — wires all layers (not a layer itself; referenced by Hosts and Tests) | Application, Infrastructure |
| **Application** | Services, Contracts, Models, Mappers, MessageHandlers | Use-case services, validation, DTO mapping, tenant enforcement, integration event definitions | Domain |
| **Domain** | Domain.Model, Domain.Shared | Entities, aggregates, value objects, enums, marker interfaces | Nothing (no outward deps) |
| **Infrastructure** | Repositories, Data, Storage, AI | EF Core, Azure SDK implementations of Application.Contracts interfaces | Application.Contracts, Domain |

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
| **TaskFlow API** | `TaskFlow.Api` | Core business logic, CRUD, integration events | SQL, Redis, Cosmos, Service Bus, Blob |
| **Azure Functions** | `TaskFlow.Functions` | Async event processing, blob processing, timer cleanup | SQL, Cosmos, Service Bus, Blob |
| **Task Scheduler** | `TaskFlow.Scheduler` | Cron jobs via TickerQ (overdue checks, recurring tasks, cleanup) | SQL, Redis, Service Bus |
| **Uno WASM App** | `TaskFlow.Uno` | Cross-platform UI (browser + desktop + mobile) — Uno Platform MVUX | Gateway |
| **Blazor App** | `TaskFlow.Blazor` | Interactive Server UI — MudBlazor, Refit client, full CRUD | Gateway, Application.Models, Domain.Shared |

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
# Uno WASM runs separately (Uno.Sdk constraint)
```

**Service dependencies**: API waits for SQL + Redis. Gateway waits for API. Functions wait for SQL + Storage.

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
        INT["🔗 Integration Tests<br/><small>EF migrations, repo CRUD, event flow</small>"]
        E2E["🔄 E2E Tests<br/><small>Full stack via WebApplicationFactory + TestContainers</small>"]
        LOAD["📊 Load Tests<br/><small>NBomber throughput scenarios</small>"]
        BENCH["⏱️ Benchmarks<br/><small>BenchmarkDotNet micro-perf</small>"]
    end

    UNIT --- ENDPOINTS --- ARCH --- INT --- E2E --- LOAD --- BENCH

    style UNIT fill:#27ae60,color:#fff
    style ENDPOINTS fill:#2ecc71,color:#fff
    style ARCH fill:#2980b9,color:#fff
    style INT fill:#8e44ad,color:#fff
    style E2E fill:#9b59b6,color:#fff
    style LOAD fill:#d35400,color:#fff
    style BENCH fill:#c0392b,color:#fff
```

### 12.2 Test Projects

| Project | Coverage |
|---------|----------|
| **Test.Unit** | Domain entity logic, DTO mappers, service success/failure/conflict paths, in-memory SQLite repo CRUD, Uno API service mappers |
| **Test.Endpoints** | Endpoint HTTP cycles (200/201/400/404/409/422) via `WebApplicationFactory` + in-memory providers |
| **Test.Architecture** | Layer dependency rules, `ITenantEntity<Guid>` on all entities, interface conventions, private setters |
| **Test.Integration** | EF migrations, real repository CRUD, paging, integration events → Service Bus → projections (TestContainers SQL Server) |
| **Test.E2E** | Full-stack end-to-end via `WebApplicationFactory` + `TestContainers.MsSql` |
| **Test.Load** | NBomber: task search throughput, CRUD scenarios (manual run) |
| **Test.Benchmarks** | BenchmarkDotNet: entity mapping perf, search projection perf (console runner) |

### 12.3 Running Tests

```bash
# All unit + architecture tests
dotnet test --filter "TestCategory=Unit|TestCategory=Architecture"

# Integration tests (requires running infra)
dotnet test --filter "TestCategory=Integration"

# All tests
dotnet test
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
