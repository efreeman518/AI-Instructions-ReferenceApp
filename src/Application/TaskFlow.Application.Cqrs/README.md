# TaskFlow.Application.Cqrs

CQRS code is organized by feature under `Features/*`.

Each feature folder owns request records, handlers, validators, and handler registration fragments for that slice. The root registration entry point stays in `Registration/CqrsApplicationRegistration.cs` so host wiring stays stable.

Shared feature helpers stay under `Features/Shared` and only cover repeated CQRS ceremony such as response wrapping, save error handling, cache keys, best-effort event publishing, and request validation result mapping.

This demo intentionally keeps DTOs and mapper projections in `TaskFlow.Application.Models` and `TaskFlow.Application.Mappers`. Service style and CQRS style share one HTTP contract and one repository projection surface, which keeps the sample small and easier to validate.

A fuller vertical slice implementation would move feature-specific models, mappers, projections, and persistence adapters into each feature folder when those shapes need to diverge from the shared API contract.
