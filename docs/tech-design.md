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
11. [Testing Strategy](#11-testing-strategy)
12. [UI Architecture](#12-ui-architecture)

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
| **UI** | Uno Platform WASM (MVUX), Blazor (planned) |
| **Database** | SQL Server (EF Core, dual DbContext) |
| **Cache** | Redis (FusionCache with L1/L2 + backplane) |
| **Messaging** | Azure Service Bus (topics + queues) |
| **Read Model** | Azure Cosmos DB (denormalized projections) |
| **File Storage** | Azure Blob Storage |
| **AI** | Azure AI Search + Azure OpenAI (stubs) |
| **Auth** | Microsoft Entra ID (External) / Scaffold mode |
| **Observability** | OpenTelemetry (OTLP), Aspire Dashboard |
| **Testing** | xUnit, Architecture tests, NBomber, BenchmarkDotNet |

### Design Principles

- **Domain-Driven Design** — Aggregates, value objects, domain events, bounded contexts
- **CQRS-like** — Separate read/write DbContexts; denormalized Cosmos read model alongside normalized SQL
- **Multi-Tenant First** — Tenant isolation at query filter, service, and authorization layers
- **Event-Driven** — Domain events flow through Service Bus to Azure Functions for async processing
- **Config-Driven Auth** — Single build, multiple deployment profiles (dev scaffold vs Entra ID prod)
- **Emulator-Ready** — All Azure services run as local emulators via Aspire; no cloud account needed for development

---

## 2. C4 Architecture Diagrams

### 2.1 Context Diagram

Shows the TaskFlow system boundary, its users, and external dependencies.

```mermaid
C4Context
    title TaskFlow — System Context

    Person(user, "TaskFlow User", "Manages tasks, categories, tags")
    Person(admin, "Tenant Admin", "Manages tenant settings and users")

    System(taskflow, "TaskFlow System", "Multi-tenant task management platform")

    System_Ext(entra, "Microsoft Entra ID", "Identity & access management")
    System_Ext(aisearch, "Azure AI Search", "Hybrid/vector task search")
    System_Ext(openai, "Azure OpenAI", "Agent chat, AI features")

    Rel(user, taskflow, "Uses", "HTTPS")
    Rel(admin, taskflow, "Administers", "HTTPS")
    Rel(taskflow, entra, "Authenticates via", "OAuth 2.0 / OIDC")
    Rel(taskflow, aisearch, "Searches tasks", "REST")
    Rel(taskflow, openai, "AI agent chat", "REST")
```

### 2.2 Container Diagram

All deployable units and infrastructure resources with their relationships.

```mermaid
C4Container
    title TaskFlow — Container Diagram

    Person(user, "User", "Browser / Desktop")

    Container_Boundary(ui, "Frontend") {
        Container(uno, "Uno WASM App", "Uno Platform, .NET 10", "Cross-platform UI (browser + desktop)")
        Container(blazor, "Blazor App", "Blazor, .NET 10", "Planned alternative UI")
    }

    Container_Boundary(backend, "Backend Services") {
        Container(gateway, "API Gateway", "ASP.NET Core, YARP", "Reverse proxy, auth boundary, claims injection")
        Container(api, "TaskFlow API", "ASP.NET Core Minimal APIs", "Core business logic, CRUD, domain events")
        Container(functions, "Azure Functions", "Isolated Worker v4", "Event processing, blob triggers, cleanup")
        Container(scheduler, "Task Scheduler", "ASP.NET Core, TickerQ", "Cron jobs: overdue checks, recurring tasks, cleanup")
    }

    Container_Boundary(data, "Data & Messaging") {
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
    Rel(services, events, "Publishes domain events")
    Rel(repos_q, sql, "SELECT")
    Rel(repos_t, sql, "INSERT/UPDATE/DELETE")
    Rel(cache, redis, "L2 cache + backplane")
    Rel(events, sb, "Publishes to topic")
    Rel(storage, blobs, "Reads/Writes blobs")
    Rel(views, cosmos, "Queries views")
```

---

## 3. Software Architecture Layers

```mermaid
block-beta
    columns 1
    block:host["Host Layer"]
        h1["TaskFlow.Api"] h2["TaskFlow.Gateway"] h3["TaskFlow.Functions"] h4["TaskFlow.Scheduler"] h5["TaskFlow.Blazor"] h6["TaskFlow.Uno"]
    end
    block:boot["Bootstrapper"]
        b1["TaskFlow.Bootstrapper — DI composition root"]
    end
    block:app["Application Layer"]
        a1["Services"] a2["Contracts"] a3["Models"] a4["Mappers"] a5["MessageHandlers"]
    end
    block:domain["Domain Layer"]
        d1["Domain.Model — Entities, Aggregates, Events, Value Objects"] d2["Domain.Shared — Enums, Interfaces"]
    end
    block:infra["Infrastructure Layer"]
        i1["Repositories (EF Core)"] i2["Storage (Blob, Cosmos)"] i3["AI (Search, OpenAI)"]
    end

    host --> boot
    boot --> app
    boot --> infra
    app --> domain
    infra --> domain
```

### Layer Responsibilities

| Layer | Projects | Responsibility | May Reference |
|-------|----------|---------------|---------------|
| **Host** | Api, Gateway, Functions, Scheduler, Blazor, Uno | HTTP pipeline, function triggers, UI shell, config | Bootstrapper |
| **Bootstrapper** | TaskFlow.Bootstrapper | DI composition root, wires all layers | Application, Infrastructure |
| **Application** | Services, Contracts, Models, Mappers, MessageHandlers | Business logic, validation, DTO mapping, tenant enforcement | Domain |
| **Domain** | Domain.Model, Domain.Shared | Entities, aggregates, value objects, domain events, enums | Nothing (no outward deps) |
| **Infrastructure** | Repositories, Storage, AI | EF Core, Azure SDK implementations of domain contracts | Domain |

### Dependency Rules (Architecture-Test Enforced)

- **Domain** has zero references to Application, Infrastructure, or Host layers
- **Application.Services** has zero references to Infrastructure or Host layers
- All entities implement `ITenantEntity<Guid>`
- All services have corresponding interfaces
- Entity properties use private setters (encapsulation)

---

## 4. Service Topology

A clean representation of the Aspire-orchestrated service graph:

```mermaid
graph TB
    subgraph ui["Frontend"]
        UNO["🖥️ Uno WASM App<br/><small>localhost:5002</small>"]
    end

    subgraph services["Backend Services"]
        GW["🔀 API Gateway<br/><small>YARP Reverse Proxy<br/>localhost:7120</small>"]
        API["⚡ TaskFlow API<br/><small>Minimal APIs<br/>localhost:7067</small>"]
        FN["⚙️ Azure Functions<br/><small>Isolated Worker v4<br/>localhost:7060</small>"]
        SCH["🕐 Task Scheduler<br/><small>TickerQ<br/>localhost:7060</small>"]
    end

    subgraph infra["Infrastructure"]
        SQL[("🗄️ SQL Server<br/><small>port 38433</small>")]
        REDIS[("💾 Redis<br/><small>FusionCache L2</small>")]
        COSMOS[("🌍 Cosmos DB<br/><small>Task View Projections</small>")]
        SB["📬 Service Bus<br/><small>Topics + Queues</small>"]
        BLOB["📦 Blob Storage<br/><small>Attachments</small>"]
    end

    UNO -->|"HTTPS + Bearer"| GW
    GW -->|"HTTP + X-Orig-Request"| API

    API -->|"EF Core (R/W)"| SQL
    API -->|"FusionCache"| REDIS
    API -->|"Read views"| COSMOS
    API -->|"Publish events"| SB
    API -->|"Store/retrieve files"| BLOB

    SB -->|"Topic subscription"| FN
    BLOB -->|"Blob trigger"| FN
    FN -->|"EF Core (R/W)"| SQL
    FN -->|"Project views"| COSMOS

    SCH -->|"EF Core (R/W)"| SQL
    SCH -->|"FusionCache"| REDIS
    SCH -->|"Publish events"| SB

    classDef service fill:#4a9eff,stroke:#2d7ad6,color:#fff
    classDef infra fill:#ff9f43,stroke:#e67e22,color:#fff
    classDef ui fill:#a29bfe,stroke:#6c5ce7,color:#fff

    class UNO ui
    class GW,API,FN,SCH service
    class SQL,REDIS,COSMOS,SB,BLOB infra
```

### Service Summary

| Service | Project | Purpose | Key Dependencies |
|---------|---------|---------|-----------------|
| **API Gateway** | `TaskFlow.Gateway` | Auth boundary, YARP reverse proxy, claims injection | API |
| **TaskFlow API** | `TaskFlow.Api` | Core business logic, CRUD, domain events | SQL, Redis, Cosmos, Service Bus, Blob |
| **Azure Functions** | `TaskFlow.Functions` | Async event processing, blob processing, timer cleanup | SQL, Cosmos, Service Bus, Blob |
| **Task Scheduler** | `TaskFlow.Scheduler` | Cron jobs via TickerQ (overdue checks, recurring tasks, cleanup) | SQL, Redis, Service Bus |
| **Uno WASM App** | `TaskFlow.Uno` | Cross-platform UI (browser + desktop) | Gateway |

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

All entities inherit from `EntityBase` which provides:

| Property | Type | Purpose |
|----------|------|---------|
| `Id` | `Guid` | Primary key |
| `CreatedAt` | `DateTimeOffset` | Auto-set on creation |
| `UpdatedAt` | `DateTimeOffset` | Auto-set on mutation |
| `IsDeleted` | `bool` | Soft delete flag |

All entities also implement `ITenantEntity<Guid>` — enforcing `TenantId` on every row.

### 5.3 Value Objects

| Value Object | Properties | Used By |
|-------------|-----------|---------|
| **DateRange** | `StartDate`, `DueDate` (both `DateTimeOffset?`) | `TaskItem` |
| **RecurrencePattern** | Recurrence interval, frequency, end conditions | `TaskItem` (EF owned type) |

### 5.4 Domain Events

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

### 7.1 Endpoints

All entity endpoints follow a consistent CRUD pattern:

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/{entity}/search` | Paged search with filters and sorting |
| `GET` | `/api/{entity}/{id}` | Get single entity by ID |
| `POST` | `/api/{entity}` | Create new entity |
| `PUT` | `/api/{entity}/{id}` | Update existing entity |
| `DELETE` | `/api/{entity}/{id}` | Delete entity |

**Entities with full CRUD**: `task-items`, `categories`, `tags`, `comments`, `checklist-items`, `attachments`  
**Entities with partial CRUD**: `task-item-tags` (create, get, delete — no search/update)

**Special Endpoints**:

| Method | Route | Purpose |
|--------|-------|---------|
| `POST` | `/api/attachments/upload` | Multipart file upload (file, ownerType, ownerId) |
| `GET` | `/api/search/tasks` | AI-powered hybrid search (`?query=...&mode=hybrid&maxResults=10`) |
| `POST` | `/api/agent/chat` | AI agent chat endpoint |
| `GET` | `/api/task-views` | Cosmos DB denormalized views (`?tenantId=...&pageSize=20`) |
| `GET` | `/api/task-views/{id}` | Single task view (`?tenantId=...`) |
| `GET` | `/health` | Health check |
| `GET` | `/alive` | Liveness probe |

### 7.2 Request/Response Envelopes

```
DefaultRequest<TDto>         → Wraps a DTO for create/update operations
DefaultResponse<TDto>        → Single entity response with metadata
SearchRequest<TFilter>       → Paged search: Page, PageSize, SortBy, SortDirection, Filter
PagedResponse<TDto>          → Items[] + TotalCount + pagination metadata
Result<T>                    → Success | Failure(errors) | None (404)
```

### 7.3 Middleware Pipeline (Order of Execution)

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

        subgraph emulators["Emulated Infrastructure"]
            SQL["SQL Server<br/><small>Container, port 38433<br/>Volume: taskflow-sql-data</small>"]
            REDIS["Redis<br/><small>Emulator<br/>Volume: taskflow-redis-data</small>"]
            SB["Service Bus<br/><small>Emulator</small>"]
            BLOB["Azure Storage<br/><small>Emulator</small>"]
            COSMOS["Cosmos DB<br/><small>Emulator</small>"]
        end

        subgraph apps["Application Services"]
            GW["Gateway :7120"]
            API["API :7067"]
            FN["Functions :7060"]
            SCH["Scheduler"]
        end
    end

    UNO["Uno WASM :5002<br/><small>(run separately)</small>"]
    DASH["Aspire Dashboard :17179"]

    UNO --> GW
    DASH -.->|"Traces, Metrics, Logs"| apps

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

        subgraph data["Data & Messaging"]
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

    SWA["Static Web Apps<br/><small>Uno WASM</small>"]
    USER["Users"]

    USER --> SWA
    SWA --> GW_AZ
    GW_AZ --> API_AZ
    API_AZ --> SQL_AZ & REDIS_AZ & COSMOS_AZ & SB_AZ & BLOB_AZ
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
- Correlation ID flows through: HTTP headers → service calls → domain events → Function triggers → logs
- Enables end-to-end distributed tracing across all services

### 10.4 Aspire Dashboard

Locally at `http://localhost:17179`:
- Resource graph (all services + infra)
- Structured logs with filtering
- Distributed traces (request → service → database)
- Metrics (throughput, latency, errors)

---

## 11. Testing Strategy

### 11.1 Test Pyramid

```mermaid
graph TB
    subgraph pyramid["Test Pyramid"]
        direction BT
        UNIT["🧪 Unit Tests (204)<br/><small>Domain, Mappers, Services, Repos, Endpoints, UI</small>"]
        ARCH["🏗️ Architecture Tests (12)<br/><small>Layer deps, naming conventions, tenant contracts</small>"]
        INT["🔗 Integration Tests (11)<br/><small>EF migrations, repo CRUD, event flow</small>"]
        LOAD["📊 Load Tests<br/><small>NBomber throughput scenarios</small>"]
        BENCH["⏱️ Benchmarks<br/><small>BenchmarkDotNet micro-perf</small>"]
    end

    UNIT --- ARCH --- INT --- LOAD --- BENCH

    style UNIT fill:#27ae60,color:#fff
    style ARCH fill:#2980b9,color:#fff
    style INT fill:#8e44ad,color:#fff
    style LOAD fill:#d35400,color:#fff
    style BENCH fill:#c0392b,color:#fff
```

### 11.2 Test Projects

| Project | Count | Coverage |
|---------|-------|----------|
| **Test.Unit** | 204 | Domain entity logic, DTO mappers, service success/failure/conflict paths, in-memory SQLite repo CRUD, endpoint HTTP cycles (200/201/400/404/409/422), Uno API service mappers |
| **Test.Architecture** | 12 | Layer dependency rules, `ITenantEntity<Guid>` on all entities, interface conventions, private setters |
| **Test.Integration** | 11 | EF migrations, real repository CRUD, paging, domain events → Service Bus → projections |
| **Test.Load** | — | NBomber: task search throughput, CRUD scenarios (manual run) |
| **Test.Benchmarks** | — | BenchmarkDotNet: entity mapping perf, search projection perf (console runner) |
| **Test.E2E** | — | End-to-end (planned) |

### 11.3 Running Tests

```bash
# All unit + architecture tests
dotnet test --filter "TestCategory=Unit|TestCategory=Architecture"

# Integration tests (requires running infra)
dotnet test --filter "TestCategory=Integration"

# All tests
dotnet test
```

---

## 12. UI Architecture

### 12.1 Uno Platform WASM (Primary UI)

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

    GW["API Gateway"]

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

### 12.2 Blazor (Planned)

Project `TaskFlow.Blazor` exists as a stub for a future Blazor-based alternative UI.

### 12.3 Gateway as BFF

The YARP Gateway acts as a **Backend-for-Frontend (BFF)**:
- Handles user authentication (Entra ID or scaffold)
- Acquires service-to-service tokens for downstream API calls
- Strips `/gateway` prefix from routes
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
