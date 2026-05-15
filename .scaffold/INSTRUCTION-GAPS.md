# INSTRUCTION-GAPS

Gaps in the scaffold instruction set discovered while building / extending the reference app. Each entry is **scaffold-side feedback** — something the next integrator shouldn't have to discover by trial and error. Distinct from FlowEngine package bugs (which were filed against `EF.Packages.Enterprise`) or in-repo work-tracking.

> Last refreshed: 2026-05-14, after the FlowEngine 1.0.104 integration. The pre-1.0.104 working notebook (`FLOWENGINE-INTEGRATION-GAP.md`) was deleted in commit `a03f64a`; bugs from that doc were resolved upstream (notably `WorkflowDefinitionBuilder.FromJson` and `WorkflowDefinitionJsonOptions.Default`) and the remaining scaffold-relevant lessons are captured below.

## Open gaps (scaffold-tool work)

### G-001 — Workflow orchestration is not a first-class scaffold option

**Observation.** The scaffold's domain-specification / resource-implementation schema does not have a flag for "include a workflow engine." Adding FlowEngine required ~10 hand-written files: a separate DbContext implementing three FE interfaces, a SQL options configurator for the separate migration-history table, a startup task to apply FE migrations, a registration partial, an `IWorkflowTrigger` interface + implementation, three workflow JSONs, a test project to validate them, and Dashboard hosting in Blazor.

**Suggested addition.** A scaffold flag `includeFlowEngine: true` that emits:

- `RegisterServices.FlowEngine.cs` partial in Bootstrapper with the canonical fluent chain.
- `<HostName>FlowEngineDbContext.cs` in Infrastructure.Data implementing the three FE mixins, schema name + history table name as constants.
- `ConfigureFlowEngineSqlOptions` extension in the database registration partial.
- `ApplyFlowEngineMigrationsStartup` startup task.
- `Workflows/` folder with a single placeholder workflow + the seeding hosted-service registration.
- `Test.Integration.<HostName>.FlowEngine` project with the four-validity-tier test class (deserialize, validate, registry round-trip, builder, file-presence).
- Dashboard hosting in the chosen UI host (Blazor today; Razor Components for new hosts).
- `MapFlowEngineAdmin(prefix: "/api/flowengine")` in the API's `WebApplicationBuilderExtensions`.

The scaffold should accept the workflow-engine package version as a parameter so the consumer is not locked to a specific FE release.

### G-002 — Atomic-outbox preservation is a load-bearing data-layout decision and the scaffold should make it explicit

**Observation.** FlowEngine's atomic outbox guarantee holds only when state + outbox live in the same DbContext (Variant A: same DB, separate schema). Moving FE to a separate database (Variant B / C) silently degrades `message` / `integration` / `agent` delivery to "best effort" — the failure mode is data loss on crash between state-save and outbox-publish, not a startup error. Today the scaffold has no concept of this trade-off.

**Suggested addition.** When `includeFlowEngine: true`, the scaffold should:

- Default to Variant A (same DB, separate schema) and emit `TaskFlowFlowEngineDbContext` accordingly.
- Document the three variants and their trade-offs in the generated tech-design doc (template for §14.4 from this repo).
- If a future flag `flowEngineDb: separate` is offered, emit a warning in the scaffold's audit log that atomic-outbox is no longer preserved by FE alone — and a follow-up step for the integrator to wire FE `message` nodes to the app's existing at-least-once publisher.

### G-003 — Inheritance conflict between `EF.Data.DbContextBase<TUser,TKey>` and FlowEngine's outbox/circuit-breaker bases

**Observation.** TaskFlow's primary `TaskFlowDbContextTrxn` inherits from `EF.Data.DbContextBase<string,Guid?>` for the audit interceptor. FlowEngine 1.0.x ships `FlowEngineOutboxDbContext` / `FlowEngineCircuitBreakerDbContext` as abstract bases. A single DbContext cannot inherit from both — and the integrator only discovers this when their first FE migration fails. 1.0.104 introduced interface-based composition (`IFlowEngineStateDbContext`, etc.) which lets a fresh DbContext declare all three roles, but the scaffold should document this as the canonical pattern.

**Suggested addition.** Add to scaffold templates: a "FlowEngine DbContext alongside an app DbContext that has its own base" recipe, using the interface-composition pattern. Generated comment in the file should explain *why* it's a separate DbContext, not a subclass.

### G-004 — Trigger model is left to the integrator with no template

**Observation.** FlowEngine workflows must be invoked by someone. Today three patterns are viable (Service Bus subscriber in Functions, inline call in service, TickerQ cron job) — but the scaffold emits none of them. The reference app ends up with `WorkflowTriggerHandler` as a half-wired hint: present in DI but not connected to the message bus, because TaskItem events are integration events (out-of-process via Service Bus), not in-process `IMessage`.

**Suggested addition.** When `includeFlowEngine: true` and `includeFunctionApp: true`, the scaffold should emit a placeholder Service Bus subscriber in `TaskFlow.Functions` that takes a typed event and calls `IWorkflowTrigger`. When `includeScheduler: true`, emit a placeholder TickerQ job calling a recurring workflow.

### G-005 — Workflow JSON copy-on-build is silent when it fails

**Observation.** Workflow JSON files are referenced by the API's csproj via `<Content Include="Workflows\*.json">` with `CopyToOutputDirectory=PreserveNewest`. If the glob is missing or the files are excluded by another item group, the seeding service has nothing to seed and the failure surfaces only when an instance start with that workflow id returns "not found." `Test.Integration.FlowEngine` includes a `All_Three_Workflows_Are_Present_In_Output` guard test — this should be a scaffold-generated convention, not a hand-rolled safety net.

**Suggested addition.** Scaffold template for FlowEngine test project should always emit the file-presence guard for whatever workflow JSONs the integrator declares.

### G-006 — Roslyn pin set when combining FlowEngine + EF Core Design

**Observation.** Older FlowEngine releases (1.0.98) pulled `Microsoft.CodeAnalysis.Common 5.3.0` while EF Core Design 10.x pulled 5.0.0; this required pinning **four** Roslyn packages (`Common`, `CSharp`, `Workspaces.Common`, `CSharp.Workspaces`) to 5.3.0 in `Directory.Packages.props`. 1.0.104 widened the range and the pin is no longer required, but the issue can resurface with any future FE bump.

**Suggested addition.** Scaffold's `validate-ef-packages-feed.py` script should detect the diamond and recommend the four-pin block. Or: emit a CI gate that fails if `dotnet ef migrations add` cannot run.

## Closed gaps (resolved upstream or in this repo)

| # | Title | Status |
|---|---|---|
| C-001 | `WorkflowDefinitionBuilder.FromJson(json).Build()` returns empty definition | Fixed in EF.FlowEngine 1.0.104. Test asserts the hydration. |
| C-002 | `JsonSerializer.Deserialize<WorkflowDefinition>` requires `JsonStringEnumConverter` | Fixed in 1.0.104 — `WorkflowDefinitionJsonOptions.Default` shipped. Test uses it directly. |
| C-003 | Workflow seeding hand-rolled in startup task | Replaced by FE-shipped `AddWorkflowJsonSeeding()` hosted service in 1.0.104. The bespoke `WorkflowSeedStartupTask.cs` was deleted in commit `a03f64a`. |
| C-004 | Outbox vs CircuitBreaker DbContext diamond | Resolved upstream — 1.0.104 uses interface composition (`IFlowEngineStateDbContext` + `IFlowEngineOutboxDbContext` + `IFlowEngineCircuitBreakerDbContext`) so a single fresh DbContext can wear all three roles. |
| C-005 | `AddAzureOpenAIAgentClient` factory overload missing | Added in 1.0.104 — takes `Func<IServiceProvider, AzureOpenAIClient>` + deployment + model names. TaskFlow uses this directly. |
| C-006 | Admin API default-prefix mismatch with docs | Worked around in this repo by always passing the explicit prefix: `MapFlowEngineAdmin(prefix: "/api/flowengine")`. Documentation upstream still owes a callout. |
