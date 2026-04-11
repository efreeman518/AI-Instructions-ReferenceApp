# HANDOFF — TaskFlow Reference App

## Session Summary

Phase 1 (Domain Discovery) complete. The `domain-specification.yaml` has been authored and validated against the JSON schema.

## Current State

- **currentPhase:** 2
- **currentSubPhase:** null
- **instructionVersion:** "1.1"
- **contractsScaffolded:** false

## Phase 1 Outputs

- `domain-specification.yaml` — in project root, validated against `schemas/domain-specification.schema.json`

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

## Next Phase (Phase 2 — Resource Definition)

Load `ai/resource-implementation-schema.md` to author `resource-implementation.yaml` defining:
- Hosts: API, Scheduler, Gateway (YARP), Function App, Uno UI (WASM)
- Infrastructure: SQL Server, Redis (FusionCache L2), Service Bus, Azure Storage (Blob+Table), Cosmos DB (projection)
- All external deps set to emulator mode
- AI integration with deployment-only / no-op stubs
- AuthMode: Scaffold

## Target Framework

.NET 10 — all packages at latest versions

## Residual Environment Notes

- No MCP servers required for Phase 1
- Python (pyyaml + jsonschema) used for schema validation
