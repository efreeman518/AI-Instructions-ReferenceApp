# FlowEngine Integration Gap Log

> **Runtime verification status** (2026-05-14): All 11 integration tests pass against in-memory providers. Live Aspire AppHost boot was attempted but the local container runtime had orphaned containers from earlier sessions and `dotnet run` produced no stdout within 10 minutes — verification deferred to user. See "Demo verification checklist" at the end of this document for exact curl/HTTP probes to run against the live admin API once the AppHost is up.

---

## Triage for the FlowEngine author

Read order if filing tickets / opening PRs against `EF.Packages.Enterprise`:

### 🔴 Real bugs — should be filed as defects

These produce **silent failures** (no exception, or an exception with a misleading message). Each is reproducible against `EF.FlowEngine 1.0.98`.

| # | Title | Why it's a bug, not friction |
|---|---|---|
| **21** | `WorkflowDefinitionBuilder.FromJson(json).Build()` returns an empty `WorkflowDefinition` | Method name and XML doc both say it loads a definition from JSON; in fact it returns a blank shell with empty `Id` / `Version` / `Nodes`. Caller only learns at validator time, and the validator's error message is `"WorkflowDefinition '@' failed validation"` — the `'@'` placeholder is confusing. Either fix `FromJson` to actually deserialize, or fail fast in `Build()` if the builder is empty. |
| **22** | `JsonSerializer.Deserialize<WorkflowDefinition>` requires `JsonStringEnumConverter` and there is no ready-made converter set | The README's "Define a workflow" example uses string enums (`"status": "Active"`, `"on": ["Match"]`). The obvious integrator pattern — read JSON files at startup and `Deserialize` — throws `JsonException` until a `JsonStringEnumConverter` is added. Caught us silently when `WorkflowSeedStartupTask` swallowed the exception in its try/catch and the registry stayed empty on boot. Ship a `WorkflowDefinitionJsonOptions.Default` (or source-gen `IJsonTypeInfoResolver`) so integrators don't have to discover the converter set by trial-and-error. |
| **4** | `dotnet ef migrations add` crashes with `TypeLoadException` unless **four** Roslyn packages are pinned to 5.3.0 | EF.FlowEngine 1.0.98 pulls `Microsoft.CodeAnalysis.CSharp.Scripting 5.3.0 → Common 5.3.0`. EF Core Design 10.0.7 pulls `CSharp.Workspaces 5.0.0 → Common 5.0.0`. Pinning just `Common` + `CSharp` to 5.3.0 lets `dotnet restore` succeed but `dotnet ef migrations add` then dies with `Method 'ReduceExtensionMember' in type 'CodeGenerationConstructedMethodSymbol' from assembly 'Microsoft.CodeAnalysis.Workspaces, Version=5.0.0.0' does not have an implementation`. Need to pin `CSharp.Workspaces` + `Workspaces.Common` to 5.3.0 too. Loosen `EF.FlowEngine.Scripting`'s dep range or document the full four-pin workaround. |
| **19** | `MapFlowEngineAdmin` no-arg default prefix is `/flowengine/admin`, but README + Integration Guide examples use `/api/flowengine` | Copy-paste of the no-arg overload produces a URL the docs don't mention. Either change the default to match the documented form, or call the default out explicitly in the XML doc + README. |

### 🟠 API surface friction — release-note callouts or small additive APIs

Things that work as designed but produce avoidable boilerplate or surprise. Adding overloads / helpers would be net-positive without breaking existing callers.

| # | Title | Suggested addition |
|---|---|---|
| **5** | Must manually register all 19 node executors via `AddNodeExecutor<T>()` | Add `AddBuiltInNodeExecutors()` extension that registers all 19 at once. Keep `AddNodeExecutor<T>()` for selective / custom. |
| **6** | `AddOpenAIAgentClient` takes only `IChatClient` from `Microsoft.Extensions.AI` — no `AzureOpenAIClient`-aware overload | Add `AddAzureOpenAIAgentClient(builder, clientRef, Func<IServiceProvider, AzureOpenAIClient>, string deploymentName, string modelName)` that performs the `.GetChatClient(deployment).AsIChatClient()` adapter internally. Today every Azure-hosted app re-implements that one-liner. |
| **7** + **17** | `AddServiceBusClient` overloads accept either a connection string or a bare `ServiceBusClient` from DI — no factory lambda | Most apps register `ServiceBusClient` via `Microsoft.Extensions.Azure`'s named `AddServiceBusClient` (`IAzureClientFactory<ServiceBusClient>`), not a bare singleton. Add `AddServiceBusClient(builder, clientRef, Func<IServiceProvider, ServiceBusClient>, string queueOrTopic)` matching OpenAI's factory-overload pattern. |
| **16** | Outbox and CircuitBreaker SQL packages ship as **separate** abstract `DbContext` bases (`FlowEngineOutboxDbContext`, `FlowEngineCircuitBreakerDbContext`) — you can only inherit from one | Provide a `FlowEngineAllInDbContext` (or a single `FlowEngineDbContext` that conditionally registers outbox + circuit-breaker entity sets) so production apps can have both without hand-rolling a third DbContext. Diamond inheritance is the only show-stopper that forced us to skip SQL circuit breaker in this integration. |
| **14** | `FlowEngineDbContext` is an abstract base, so apps whose primary DbContext already inherits from something else (e.g. `EF.Data.DbContextBase<TUser,TKey>`) cannot reuse it | Either offer a non-inheritance registration path (e.g. `UseStateStoreSql(typeof(MyDbContext), modelBuilder => { ... })` that pulls in the entity configurations via a fluent extension), or document the "separate FlowEngine DbContext" pattern as first-class. |

### 🟡 Documentation gaps — PR to docs repo

Behavior is correct, the docs just don't surface what's needed. Each of these cost real wall-clock time to discover during this integration.

| # | Title | What to add |
|---|---|---|
| **1** | Design Doc says "Notable Capabilities Absent: no UI workflow designer" — but the Dashboard package ships one (`Z.Blazor.Diagrams` canvas at `/workflows/new`) | Replace that callout with a Visual Designer section linking to the Operations doc and screenshots. Update the README Document Map. |
| **2** + **13** | Dashboard hosting in a modern `MapRazorComponents<App>()` app needs `Router.AdditionalAssemblies` to discover the package's `@page` routes — only the legacy `MapBlazorHub` + `_Host.cshtml` pattern is shown | Add a ".NET 8+ Razor Components host" section to the Integration Guide with a worked `<Router AdditionalAssemblies="…" />` example and the required `@using EF.FlowEngine.Dashboard` in `_Imports.razor`. |
| **3** | README install snippet assumes individual `dotnet add package` calls; Central Package Management interaction is undocumented | Ship a copy-pasteable `<ItemGroup Label="EF.FlowEngine">` block for CPM users — every sub-package pinned at the same version, so consumers paste once and reference what they need. |
| **8** | Multi-tenancy: `TenantId` is on `StartRequest` but propagation guidance (async trigger handlers, Admin API tenant filtering, Dashboard tenant scoping) is absent | "Multi-tenancy: end-to-end" appendix covering trigger-handler context propagation, Admin API tenant policies, and one-Dashboard-per-tenant vs claim-header patterns. |
| **9** | `EF.FlowEngine.Dashboard` transitively pulls `EF.FlowEngine.Clients.Sql` — undocumented coupling | Either make the dependency optional behind a Dashboard config flag, or document it in both the Dashboard README and the main README's package table. |
| **12** | When co-hosting the Dashboard in an existing MudBlazor app, it's unclear whether the host's `MudThemeProvider` / `MudDialogProvider` / etc. carry through, or whether the Dashboard requires its own | Document the required MudBlazor provider set in the Dashboard README, plus the supported MudBlazor major-version window. Ideally fail-fast at startup with a clear "missing MudXxxProvider" message. |
| **15** | Dashboard `AddFlowEngineDashboard(..., configureClient)` accepts an `Action<HttpClient>` but no recipe for forwarding the current user's bearer token to the Admin API | "Forwarding user authentication" section with an `IHttpContextAccessor` + `DelegatingHandler` example. |

### 🟢 Feature requests / would-be-nice

Not bugs, not friction blockers — useful adds that would smooth the path for the next integrator.

| # | Title | Why |
|---|---|---|
| **10** | Workflow-seeding helper (`AddFlowEngineWorkflowSeeding(opts => { opts.Directory = "Workflows"; opts.ActivateOnSeed = true; })`) | Every integrator hand-rolls the same 30–40 lines: enumerate files, deserialize, save-or-skip, optional transition to Active. |
| **11** | `DelegatingMessageClient` base class in `EF.FlowEngine.Clients.ServiceBus` that wraps a `Func<MessageEnvelope, ValueTask>` | Lets apps plug FlowEngine `message` nodes into their existing `IIntegrationEventPublisher` / MassTransit / etc. without writing a custom `IMessageClient` adapter. Preserves cross-cutting concerns (tenant headers, correlation IDs, OTel baggage). |
| **18** | Integration Guide should explicitly cover the `Microsoft.Extensions.AI.OpenAI` package as a hard dep when using `AzureOpenAIClient` → `IChatClient` | Currently the adapter (`AsIChatClient`) is in a package the docs don't reference. |
| **20** | "Choosing your database layout" three-variant section (same DB / separate DB same server / separate DB separate server) with the atomic-outbox hinge called out | Detailed proposal already in this doc — see the full entry below. |

### Summary

If you only have time for three:
1. **Fix `WorkflowDefinitionBuilder.FromJson` (#21)** — silent empty deserialize is the single most surprising bug.
2. **Ship `WorkflowDefinitionJsonOptions.Default` and document `JsonStringEnumConverter` (#22)** — the seed-startup-task pattern is the obvious integrator path and fails silently without it.
3. **Add `AddBuiltInNodeExecutors()` (#5)** — 19 lines of boilerplate every consumer copies; trivial to fix.

The Roslyn-pin issue (#4) is the most operationally painful but is primarily a packaging concern, fixable by widening one transitive dep range.

---



Issues, ambiguities, and friction points encountered while integrating **EF.FlowEngine 1.0.98** into TaskFlow (.NET 10, EF Core, Aspire, multi-project). Feed back into:

- `FlowEngine-INTEGRATION-GUIDE.md` (primary target)
- `FlowEngine-Design-Doc.html` "Additional Documentation" section
- Per-package README files

Each entry: **Observation → Impact → Suggested fix**.

---

## 1. UI Designer / Dashboard pages are undocumented in Design Doc

**Observation.** The Design Doc's "Notable Capabilities Absent" section explicitly states the document does **not** describe a UI workflow designer, but `EF.FlowEngine.Dashboard 1.0.98` ships a visual designer (`Pages.Designer`) backed by `Z.Blazor.Diagrams 3.0.4.1`. The Operations / Dashboard pages document it, but the Design Doc itself does not link to that or even mention that a visual canvas exists.

**Impact.** Newcomers reading the Design Doc first conclude FlowEngine is JSON/builder-only and may skip evaluating the Dashboard.

**Suggested fix.** Replace the "Notable Capabilities Absent" callout with a "Visual Designer" section that links to Operations doc and shows a screenshot. Update the Document Map in the package README to call out `/workflows/new` explicitly.

---

## 2. Dashboard ships as Blazor Server (legacy `MapBlazorHub`) — unclear story for `MapRazorComponents<App>()` hosts

**Observation.** `EF.FlowEngine.Dashboard` nuspec describes it as "Blazor Server". The README quick-start uses `app.MapBlazorHub()` + `app.MapFallbackToPage("/_Host")` — the pre-.NET 8 hosting model. Modern hosts using `AddRazorComponents().AddInteractiveServerComponents()` + `MapRazorComponents<App>().AddInteractiveServerRenderMode()` (the default `dotnet new blazor` since .NET 8) need different wiring.

**Impact.** Integrating into a brand-new Blazor app means either:
- Adding legacy `MapBlazorHub`/`_Host.cshtml` plumbing (works but mixes two Blazor hosting models in one app), or
- Adding the Dashboard assembly to `Router.AdditionalAssemblies` so its `@page` routes are discovered by the new component host — but neither path is documented.

**Suggested fix.** Add a "Hosting in a Razor Components App (.NET 8+)" section to the Integration Guide with a worked example covering:
- `<Router AdditionalAssemblies="new[] { typeof(EF.FlowEngine.Dashboard.DashboardServiceExtensions).Assembly }">`
- Whether `MudThemeProvider` / `MudPopoverProvider` / `MudDialogProvider` / `MudSnackbarProvider` need to be in the host `App.razor` or if the Dashboard's components include them.
- Where to put `_Imports.razor` `@using EF.FlowEngine.Dashboard.Pages`.

---

## 3. README installation snippet assumes per-PackageReference adds; central package management story missing

**Observation.** README quick-start uses `dotnet add package EF.FlowEngine --source efreeman518`. Teams using Central Package Management (`Directory.Packages.props` with `ManagePackageVersionsCentrally=true`) get an additional step the docs don't mention. `dotnet add package` correctly adds the version pin to the props file, but the user must then know to remove any per-csproj versions and that the CPM block is the source of truth.

**Impact.** Confusion on first add: the CLI auto-appends a `PackageVersion` entry to props *and* a `PackageReference` to the csproj — easy to leave a stray version=x.x.x.

**Suggested fix.** Add a "Central Package Management" note to the README install section. Provide a copy-pasteable `<ItemGroup Label="EF.FlowEngine">` block with every sub-package pinned to the same version, so CPM users can paste once and pick what they need by adding versionless `PackageReference` entries.

---

## 4. Transitive Roslyn conflict with `Microsoft.EntityFrameworkCore.Design`

**Observation.** Installing `EF.FlowEngine 1.0.98` alongside `Microsoft.EntityFrameworkCore.Design 10.0.7` triggers NU1107:

```
EF.FlowEngine -> Microsoft.CodeAnalysis.CSharp.Scripting 5.3.0 -> Microsoft.CodeAnalysis.Common (= 5.3.0)
Microsoft.EntityFrameworkCore.Design 10.0.7 -> Microsoft.CodeAnalysis.CSharp.Workspaces 5.0.0 -> Microsoft.CodeAnalysis.Common (= 5.0.0)
```

Pinning `Microsoft.CodeAnalysis.Common` and `Microsoft.CodeAnalysis.CSharp` to 5.3.0 lets `dotnet restore` succeed — **but `dotnet ef migrations add` then crashes** with `System.TypeLoadException: Method 'ReduceExtensionMember' in type 'Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationConstructedMethodSymbol' from assembly 'Microsoft.CodeAnalysis.Workspaces, Version=5.0.0.0' does not have an implementation.` because the 5.0.0 `Workspaces` / `CSharp.Workspaces` assemblies still load but their internal calls into `Common 5.3.0` resolve to method signatures that have moved. The full fix is to pin **all four** Roslyn packages to 5.3.0:

```xml
<PackageVersion Include="Microsoft.CodeAnalysis.Common"           Version="5.3.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.CSharp"           Version="5.3.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.Workspaces.Common" Version="5.3.0" />
<PackageVersion Include="Microsoft.CodeAnalysis.CSharp.Workspaces" Version="5.3.0" />
```

…and add an explicit `<PackageReference>` to the four in the project that runs `dotnet ef`.

**Impact.** Two-stage failure: restore succeeds with only two pins, then migrations crash with a misleading TypeLoadException. Easy to mis-diagnose as a FlowEngine bug.

**Suggested fix.**
- Either: bump `EF.FlowEngine.Scripting` to depend on a version range broad enough that NuGet picks 5.0.0 alongside EF Core Design, or
- Document the full four-pin block in the Integration Guide "Known issues" as a copy-pasteable fix, calling out specifically that `dotnet ef` will fail with TypeLoadException if only Common/CSharp are pinned.

---

## 5. Node executor registration is manual and easy to miss

**Observation.** All 19 node executors must be registered individually via `.AddNodeExecutor<T>()`. README quick-start shows all 19 lines verbatim. The pattern is repetitive and error-prone — miss one and the workflow fails at runtime, not registration time, with a less-than-obvious error.

**Impact.** Boilerplate; missing an executor produces runtime confusion.

**Suggested fix.** Ship a `.AddBuiltInNodeExecutors()` extension method that registers all 19. Document it as the default; let users still call `.AddNodeExecutor<T>()` for custom or selective registration. Pre-execution validation already catches unknown node types — extend that check to also surface "executor not registered" with the explicit executor type name.

---

## 6. `AddOpenAIAgentClient` takes `IChatClient`, not `AzureOpenAIClient`

**Observation.** README shows `.AddOpenAIAgentClient("gpt4o", chatClient: openAIClient.AsChatClient("gpt-4o"), modelName: "gpt-4o")`. The `AsChatClient` extension comes from `Microsoft.Extensions.AI` (not `Azure.AI.OpenAI` directly). Consuming apps that already DI-register `AzureOpenAIClient` need to either:
- Resolve it inside a lambda and call `.AsChatClient(deploymentName)` at registration time, or
- DI-register an `IChatClient` directly and pass it in.

**Impact.** Friction translating "I already have an `AzureOpenAIClient`" to a working FlowEngine agent client. The README's one-liner hides the chat-client adapter step.

**Suggested fix.** Add an `AddAzureOpenAIAgentClient(string name, Func<IServiceProvider, AzureOpenAIClient> factory, string deploymentName, string modelName)` overload that performs the `.AsChatClient(deploymentName)` adapter internally. This is the common case for Azure-hosted apps and removes a layer of indirection.

---

## 7. `AddServiceBusClient` takes connection string + topic, not a `ServiceBusClient` instance

**Observation.** README shows `.AddServiceBusClient("order-events", connectionString: "...", topicOrQueueName: "orders")`. Apps that already DI-register a `ServiceBusClient` (e.g. via `Microsoft.Extensions.Azure`'s `AddServiceBusClient(...)` from `Azure.Messaging.ServiceBus`) can't pass it in directly — they have to dig out the connection string again and risk it diverging.

**Impact.** Duplicated configuration; the Azure SDK's recommended `DefaultAzureCredential` flow (no connection string) isn't supported through this overload at all.

**Suggested fix.** Add an overload accepting either:
- `Func<IServiceProvider, ServiceBusClient>` + topic name, or
- `(ServiceBusClient client, string topicOrQueueName)` directly.

This is the same pattern other connector packages use (`AddClient<EfCoreQueryClient<T>>()`) and matches Azure SDK passwordless-auth conventions.

---

## 8. Multi-tenancy story: `TenantId` is first-class on `StartRequest`, but propagation guidance is sparse

**Observation.** README's "Multi-tenancy" section says `TenantId` is on `StartRequest`, propagates to `ExecutionInstance.TenantId`, and "each state store implementation is responsible for filtering on it. The engine does not enforce tenant isolation". For an existing multi-tenant app (TaskFlow has request-scoped tenant context), there's no guidance on:
- How to propagate the tenant from an `IIntegrationEventPublisher`-driven trigger handler into the `StartRequest` (which runs outside an HTTP request — async message processor).
- Whether the Admin API endpoints filter by tenant automatically or require a custom policy.
- How the Dashboard surfaces tenant-scoped views (the README screenshots all show a single tenant).

**Impact.** Easy to bolt on FlowEngine and have all tenants see each other's workflow instances via the Dashboard.

**Suggested fix.** Add a "Multi-tenancy: end-to-end" appendix to the Integration Guide covering:
- Recommended pattern for stashing the trigger-time tenant into a per-message context for `StartAsync`.
- Admin API: which endpoints honor a tenant claim, which don't, and how to wrap them with a policy.
- Dashboard: whether to host one Dashboard per tenant or pass tenant via header / claim.

---

## 9. Dashboard depends on `EF.FlowEngine.Clients.Sql` — undocumented coupling

**Observation.** `EF.FlowEngine.Dashboard 1.0.98` nuspec lists `EF.FlowEngine.Clients.Sql 1.0.98` as a hard dependency. The README's package list calls Clients.Sql out separately ("optional — ready-made `IFlowClient` implementations") with no hint that the Dashboard pulls it in transitively.

**Impact.** Surprising payload — consumers don't realize installing the Dashboard means they're shipping SQL query-client code whether they use a `query` node or not.

**Suggested fix.** Either:
- Move the SQL query-client UI bits behind a Dashboard option flag and make the dep optional, or
- Document the transitive dependency explicitly in the Dashboard package README + the main README's package table.

---

## 10. Workflow registry seeding pattern not shown

**Observation.** README shows how to call `registry.SaveAsync(definition)` once. For real apps the typical pattern is "ship N JSON files in `Workflows/` and seed them on startup" — but there's no canonical helper or sample. Each integrator builds their own startup task: enumerate files, deserialize, save-or-skip-if-exists, optionally transition to Active.

**Impact.** Every integrator reinvents the same 30-40 lines. Slight semantic differences across integrations (e.g. overwrite-on-restart vs respect-existing-version, Draft-vs-Active default).

**Suggested fix.** Ship `EF.FlowEngine.Extensions.JsonFileSeeding` (or fold into core) with:
```csharp
services.AddFlowEngineWorkflowSeeding(opts =>
{
    opts.Directory = "Workflows";
    opts.ActivateOnSeed = true;
    opts.OverwriteExistingVersion = false;
});
```
Wire it as an `IHostedService` that runs after migrations + before the engine starts processing.

---

## 11. Outbox + Service Bus + existing `IIntegrationEventPublisher` — interplay

**Observation.** TaskFlow already has an `IIntegrationEventPublisher` writing to its own Service Bus topics with its own outbox pattern (or direct publish). FlowEngine's `EF.FlowEngine.Outbox.Sql` provides a transactional outbox on top of the engine's state DbContext. When a workflow's `message` node publishes through `EF.FlowEngine.Clients.ServiceBus`, the engine's outbox handles delivery — but if the integrator wants events to go through the existing app's publisher (preserving tenant headers, correlation IDs, OpenTelemetry baggage), they have to write a custom `IMessageClient` adapter.

**Impact.** Two outbox systems risk parallel growth; integrators choose between "lose the existing publisher's cross-cutting concerns" or "write a custom adapter to keep them".

**Suggested fix.** Provide a `DelegatingMessageClient` base class in `EF.FlowEngine.Clients.ServiceBus` that takes a `Func<MessageEnvelope, ValueTask>` so apps can plug in their existing publisher with one wrapper class. Document the pattern in the Connectors doc.

---

## 12. `_Imports.razor` / MudBlazor provider setup for embedded Dashboard

**Observation.** When co-hosting the Dashboard in an app that already configures MudBlazor (MudThemeProvider, MudPopoverProvider, MudDialogProvider, MudSnackbarProvider) in its own `App.razor`, it's unclear whether:
- The Dashboard pages will pick up the host's providers, or
- The Dashboard expects its own providers and will fail-silent without them.

The Dashboard's MudBlazor dep is `9.4.0` — same as TaskFlow's. Version mismatch handling not documented.

**Impact.** Possible runtime failures (missing provider) only visible when navigating to a Dashboard page, not at startup.

**Suggested fix.** Document the required MudBlazor providers in the Dashboard README. Note version compatibility window (which MudBlazor majors are supported). Ideally fail-fast at startup with a clear "missing MudDialogProvider" message.

---

## 13. `MapRazorComponents<App>()` + Dashboard's `@page` routes — discovery

**Observation.** In a `MapRazorComponents<App>()` setup, `@page` directives in another assembly are only discovered if that assembly is added to `Router.AdditionalAssemblies`. The Dashboard README's hosting example uses `MapBlazorHub` + `MapFallbackToPage("/_Host")` (Razor Pages), which auto-discovers component routes. No equivalent guidance for the modern setup.

**Impact.** Navigating to `/workflows/new` returns 404 with no startup error — confusing failure mode.

**Suggested fix.** README's Dashboard section: add a snippet showing `<Router AppAssembly="@typeof(App).Assembly" AdditionalAssemblies="new[] { typeof(EF.FlowEngine.Dashboard.DashboardServiceExtensions).Assembly }">` and mention it's required for .NET 8+ Razor Components hosts.

---

## 14. SQL migrations: which DbContext owns the FlowEngine tables?

**Observation.** `UseStateStoreSql<TContext>()` requires `TContext : FlowEngineDbContext` (abstract base). The README example shows `public class AppDbContext : FlowEngineDbContext { }` — the consuming app's main DbContext directly inherits. The Integration Guide reinforces this. For apps whose primary DbContext **already inherits from a different base** (e.g. `EF.Data.DbContextBase<TUser,TKey>` for audit interceptors) this is impossible without multi-inheritance.

**Impact.** Apps with a non-trivial base DbContext have to:
- Define a **separate** `TaskFlowFlowEngineDbContext : FlowEngineDbContext` co-located with the same connection string but its own migration history.
- Lose audit/multi-tenant interceptors on FlowEngine tables (not necessarily a problem — engine state is per-tenant via `ExecutionInstance.TenantId` anyway).

**Suggested fix.** Add an "Integrating into apps with an existing custom DbContext base" subsection to the Integration Guide with a worked example of the separate-DbContext approach (separate migrations folder, separate `__EFMigrationsHistory_FlowEngine` table, shared connection string). Note that interceptors do not transfer.

## 16. Outbox vs. Circuit Breaker DbContext diamond inheritance

**Observation.** `EF.FlowEngine.Outbox.Sql` ships `FlowEngineOutboxDbContext : FlowEngineDbContext` (adds outbox tables). `EF.FlowEngine.CircuitBreaker.Sql` ships `FlowEngineCircuitBreakerDbContext : FlowEngineDbContext` (adds circuit-breaker state table). Both are abstract bases. A consumer can only inherit from one — there's no way to get both outbox tables and SQL circuit-breaker state into a single DbContext via inheritance.

**Impact.** Forces a choice: outbox **or** SQL circuit breaker. Apps wanting both have to either:
- Mirror the entity configurations manually into a third DbContext class, or
- Run two FlowEngine DbContexts (separate migration histories) — adds operational complexity, breaks the "single SaveChangesAsync transactional save" promise of the outbox.

**Suggested fix.** Provide a single `FlowEngineAllInDbContext` (or `FlowEngineFullDbContext`) that includes both outbox and circuit-breaker tables. Document it as the recommended base for production deployments. Keep the narrower bases for consumers who want only one feature.

## 17. `AddServiceBusClient` DI overload requires a bare `ServiceBusClient` singleton

**Observation.** `AddServiceBusClient(builder, clientRef, queueOrTopic)` resolves `ServiceBusClient` from the DI container. Apps using `Microsoft.Extensions.Azure`'s `services.AddAzureClients(b => b.AddServiceBusClient(connStr).WithName(...))` register a **named** client behind `IAzureClientFactory<ServiceBusClient>` — the bare `ServiceBusClient` type is not in DI, so the FlowEngine overload throws at registration validation.

**Impact.** Either use the connection-string overload (duplicating config), or write boilerplate to register a bare `ServiceBusClient` singleton resolved from the factory just for FlowEngine.

**Suggested fix.** Add an overload `AddServiceBusClient(builder, clientRef, Func<IServiceProvider, ServiceBusClient> factory, queueOrTopic)` matching the OpenAI client's factory-based pattern. Document the `IAzureClientFactory<T>` bridge pattern in the Connectors doc.

## 18. `AddOpenAIAgentClient` requires `IChatClient` in DI — no `AzureOpenAIClient`-aware overload

**Observation.** The connector takes an `IChatClient` from `Microsoft.Extensions.AI` either factory-supplied or DI-resolved. Apps that already DI-register `AzureOpenAIClient` (idiomatic for Azure SDK) need to bridge: `azureClient.GetChatClient(deployment).AsIChatClient()` (requires `Microsoft.Extensions.AI.OpenAI`). The README's one-liner `openAIClient.AsChatClient("gpt-4o")` hides which package the extension lives in.

**Impact.** First-time wiring requires hunting for the right adapter package. Easy to silently miss telemetry by registering the wrong adapter.

**Suggested fix.** Add `AddAzureOpenAIAgentClient(builder, clientRef, Func<IServiceProvider, AzureOpenAIClient> factory, string deploymentName, string modelName)` that wraps the chat-client adapter internally. Document the required `Microsoft.Extensions.AI.OpenAI` dependency in the OpenAI connector README.

## 19. Admin API default prefix is `/flowengine/admin` (not `/api/flowengine`)

**Observation.** README and Integration Guide examples consistently show `MapFlowEngineAdmin(prefix: "/api/flowengine")`. The actual default (when called without args) is `/flowengine/admin`. Minor but easy to mis-document downstream.

**Impact.** Copy-paste of the no-arg overload produces a different URL than the docs imply.

**Suggested fix.** Either change the default to match the docs, or update the docs to call out the default explicitly.

## 20. Database placement: same-DB-different-schema vs separate-DB — should be an explicit upfront decision in the integration doc

**Observation.** The Integration Guide implicitly assumes the FlowEngine tables live alongside the application's data in the same DbContext (`UseStateStoreSql<AppDbContext>()` where the app subclasses `FlowEngineDbContext`). It does not call out that this is a deployment-architecture choice with real tradeoffs, nor that the choice is **load-bearing on the atomic-outbox guarantee** and effectively permanent once data exists.

**Why this matters.** `SqlExecutionStateStore.SaveWithOutboxAsync` writes execution state + outbox rows in a single `SaveChangesAsync` precisely because both entity sets live in the same DbContext. The moment FlowEngine tables move to a different database (different DbContext / different connection), the engine falls back to "separate save + per-entry outbox enqueue" — at-least-once delivery on `message` / `agent` / `integration` nodes is no longer guaranteed by transactional means, only by retry/idempotency. Integrators who don't see this called out can pick a "cleaner" separate-DB layout for the right reasons (isolation, ops separation) and accidentally degrade delivery guarantees for the wrong reasons (didn't realize).

**Suggested addition — a three-variant table in the Integration Guide:**

| Variant | Atomic outbox | Resource isolation | Cross-DB joins | Ops cost | Best for |
|---|---|---|---|---|---|
| **A. Same DB, separate schema + migration history** | ✅ kept (single `SaveChangesAsync`) | shared instance / pool; EF `AddPooledDbContextFactory` per-context separation | n/a — single DB | lowest — one DB resource, one backup window, one connection string | Monolith deployments, demos, teams without dedicated platform ops, smaller workloads, anywhere `message`/`agent`/`integration` nodes rely on FlowEngine's outbox |
| **B. Separate DB on the same server/instance** | ❌ lost — separate DbContext, separate connection | full buffer pool / IO isolation; shared compute | possible via 3-part names | medium — second DB on existing instance, second migration history, two backup operations on same maintenance window | Want connection-pool / buffer-cache isolation without paying for a second SQL instance; OK with best-effort or app-publisher-based delivery |
| **C. Separate DB on a separate server** | ❌ lost | full IO + compute isolation | not practical | highest — second instance, separate failure domain, separate PITR coordination | Platform-team-owned FlowEngine serving multiple apps; SaaS platform pattern; aggressive blast-radius / compliance requirements |

**Hinge:** the atomic-outbox guarantee. If outbox atomicity is required (workflows make `message` / `agent` / `integration` sends that must not be lost on crash between state-save and external dispatch), pick A. If the integrator's app already has its own at-least-once publisher (Service Bus + transactional outbox, MassTransit, etc.) and FlowEngine nodes delegate to it instead of using FlowEngine's outbox directly, B or C become viable.

**Other tradeoffs to call out:**
- **Cross-DB PITR drift** (B/C): workflow `Entity` JSON snapshots reference business-data IDs by value. Point-in-time-restore one DB without the other surfaces dangling references in suspended-instance context. Co-located backup window mitigates but does not eliminate.
- **Migration coordination** (B/C): two migration histories, two deploy steps. Recommend `__EFMigrationsHistory_FlowEngine` table name to make grep / audit trivial.
- **Dev experience** (B/C): one more connection string, one more Aspire resource, one more emulator startup. Worth the friction in prod; not worth it for demos.
- **`FlowEngineDbContext` shoehorn (gap #14) and Outbox/CircuitBreaker diamond (gap #16) disappear** when the integrator owns a dedicated FlowEngine DB — the FlowEngine DbContext subclass is the *only* DbContext touching that DB and can inherit whichever mixin it needs.

**Suggested fix.**
- Add a "Choosing your database layout" section to `FlowEngine-INTEGRATION-GUIDE.md` near the top, before the `UseStateStoreSql<TContext>()` example. Use the three-variant table above. Lead with the atomic-outbox hinge.
- Ship sample registrations for all three: A registers a single DbContext subclass; B registers two DbContexts with two connection strings against the same `AppDbContext`-style options builder; C is identical to B but with two `AddSqlServer(...)` Aspire resources.
- Note that once FlowEngine has been running in production for any length of time, moving between variants requires data migration (Executions / HumanTasks / WorkflowDefinitions / Outbox / CircuitBreaker rows). Recommend deciding upfront.

## 21. `WorkflowDefinitionBuilder.FromJson(json).Build()` returns an empty `WorkflowDefinition`

**Observation.** Per the `EF.FlowEngine.Definition.WorkflowDefinitionBuilder` XML docs and the README, `WorkflowDefinitionBuilder.FromJson(json)` "load[s] existing definitions for incremental modification" and `.Build()` materialises them. In practice, calling `WorkflowDefinitionBuilder.FromJson(workflowJson).Build()` against a well-formed workflow JSON returns a definition with empty `Id`, empty `Version`, empty `Nodes` — the validator reports it as workflow `'@'` with three "X is required" errors.

**Repro.** Test [WorkflowDefinitionValidityTests.WorkflowDefinitionBuilder_FromJson_Documented_Bug](src/Test/Test.Integration.FlowEngine/WorkflowDefinitionValidityTests.cs) — currently asserts the empty result; flip when fixed.

**Impact.** Integrators following the README's "loading existing definitions for incremental modification" recipe will get a silently-empty definition. `WorkflowDefinitionValidator.ValidateAndThrow` catches it at save time with a confusing error (workflow `'@'`), but if a caller skips validation they'd persist garbage.

**Workaround used.** `JsonSerializer.Deserialize<WorkflowDefinition>(json, options)` with `JsonStringEnumConverter` (see gap #22). Same path the seed startup task uses.

**Suggested fix.**
- Confirm whether `FromJson` is expected to deserialize the JSON into the builder's internal state, or whether it requires the caller to first `JsonSerializer.Deserialize` and pass the result to `From(WorkflowDefinition)`. If the former, fix the implementation; if the latter, fix the XML doc and README to make the two-step requirement explicit and direct integrators at `From(definition)` instead.
- Either way, surface a clear error from `Build()` if the builder is empty, rather than returning a definition that only fails at validator time.

## 22. `JsonSerializer.Deserialize<WorkflowDefinition>` requires `JsonStringEnumConverter`

**Observation.** Workflow JSON uses string values for several enums: `"status": "Active"` (DefinitionStatus), `"on": ["Match"]` (DecisionOutcome), etc. Without registering `JsonStringEnumConverter` in the `JsonSerializerOptions`, `System.Text.Json.JsonSerializer.Deserialize<WorkflowDefinition>` throws `JsonException: The JSON value could not be converted to EF.FlowEngine.Definition.DefinitionStatus. Path: $.status`. The README's "Define a workflow" JSON example uses string enums, and the seed-startup-task pattern that integrators copy ("read folder, deserialize, save") trips on this on the first run.

**Impact.** Integrators write a seed startup task with `new JsonSerializerOptions { PropertyNameCaseInsensitive = true }`, paste in the README's JSON, and discover at first boot that nothing seeds — the exception is logged by the seed task's try/catch and the registry stays empty silently. (We hit this in [src/Host/TaskFlow.Api/Workflows/WorkflowSeedStartupTask.cs](src/Host/TaskFlow.Api/Workflows/WorkflowSeedStartupTask.cs) and surfaced it via the integration tests; fix was a one-liner.)

**Suggested fix.**
- Ship a `WorkflowDefinitionJsonOptions.Default` (or expose an `IJsonTypeInfoResolver` for source generation) on the `EF.FlowEngine.Definition` package that integrators can use directly: `JsonSerializer.Deserialize<WorkflowDefinition>(json, WorkflowDefinitionJsonOptions.Default)`. Pre-configures all converters the model needs, source-of-truth for the JSON shape.
- Update the README's "Register and start a workflow" + "Workflow Authoring Lifecycle" sections to reference that helper instead of leaving each integrator to figure out the converter set.

---

## 15. Dashboard authentication: pass-through pattern for an authenticated Admin API

**Observation.** `AddFlowEngineDashboard(adminApiBaseUrl, configureClient)` accepts an `Action<HttpClient>` for auth header config — but the typical case is "forward the current user's bearer token to the Admin API", which requires DI-resolving an `IHttpContextAccessor` or a token broker inside the configureClient lambda. Not shown in any doc.

**Impact.** Teams use the configureClient overload to hardcode an API key or a long-lived token instead of doing per-request token forwarding properly.

**Suggested fix.** Add a "Forwarding user authentication" section to the Dashboard README. Example with `IHttpContextAccessor` + `DelegatingHandler` that copies the `Authorization` header from the inbound request to the outbound Admin API call.

---

## Resolution status

| # | Title | Status |
|---|---|---|
| 1 | Designer in Design Doc | open |
| 2 | Dashboard Blazor host model | open |
| 3 | CPM install snippet | open |
| 4 | Roslyn conflict | worked around (pin) |
| 5 | Node executor registration | open |
| 6 | OpenAI client adapter | open |
| 7 | Service Bus client overload | open |
| 8 | Multi-tenancy guidance | open |
| 9 | Dashboard transitive Clients.Sql | open |
| 10 | Workflow seeding pattern | open |
| 11 | Existing publisher interop | open |
| 12 | MudBlazor provider setup | open |
| 13 | Router AdditionalAssemblies | open |
| 14 | DbContext ownership | open |
| 15 | Dashboard auth forwarding | open |
| 16 | Outbox vs CircuitBreaker diamond | worked around (skipped SQL circuit breaker) |
| 17 | ServiceBusClient DI overload | worked around (connection-string overload) |
| 18 | OpenAI `IChatClient` adapter friction | worked around (factory lambda calls `AsIChatClient`) |
| 19 | Admin API default prefix mismatch | worked around (explicit `/api/flowengine` arg) |
| 20 | DB placement decision needs explicit guidance | open — proposed three-variant table for integration doc |
| 21 | `WorkflowDefinitionBuilder.FromJson(json).Build()` returns an empty definition | **suspected bug** — worked around with `JsonSerializer.Deserialize<WorkflowDefinition>` |
| 22 | `JsonSerializer.Deserialize<WorkflowDefinition>` requires `JsonStringEnumConverter` | docs gap — fixed in seed task + tests |

---

## Demo verification checklist (run when Aspire AppHost is up)

`dotnet run --project src/Host/Aspire/AppHost` then in the Aspire Dashboard find the URLs for `taskflowapi` and `taskflowgateway`. Run the following from a separate shell. Gateway base URL is shown in the dashboard (typically `https://localhost:5xxx`); substitute it below.

```bash
GW="https://localhost:<gateway-port>"   # from Aspire dashboard

# 1. Verify migrations applied (FlowEngine schema tables exist)
curl -sk "$GW/api/flowengine/workflows" | jq '.[] | { id, version, status }'
# Expected: 3 entries → ai-task-triage, ai-task-decomposer, compliance-check, all status "Active"

# 2. List instances (should be empty before any run)
curl -sk "$GW/api/flowengine/instances?Status=Running" | jq '. | length'

# 3. Start a manual run of ai-task-triage
curl -sk -X POST "$GW/api/flowengine/instances/start" -H "Content-Type: application/json" -d '{
  "workflowId": "ai-task-triage",
  "params": {
    "tenantId":    "11111111-1111-1111-1111-111111111111",
    "taskId":      "22222222-2222-2222-2222-222222222222",
    "description": "Sample triage task for the demo run"
  },
  "tenantId": "11111111-1111-1111-1111-111111111111"
}' | jq

# 4. Tail the instance state until it reaches Suspended (waiting on agent or human) or Completed
INSTANCE_ID="<paste from step 3>"
curl -sk "$GW/api/flowengine/instances/$INSTANCE_ID" | jq '{ status, history: (.history | map({ nodeId, outcome, completedAt })) }'
```

**What "good" looks like:**
- Step 1: three workflows present with `status: "Active"` (proves seed task ran and migrations applied).
- Step 3: 201/200 response with an `instanceId` (proves engine + admin API are wired end-to-end).
- Step 4: history shows `n-classify` as the first node executed; status `Faulted` is expected when `AiServices:FoundryEndpoint` is not configured (gap #18 documented this — agent client only registers when the endpoint is set). Configure `FoundryEndpoint` + `ChatDeployment` to exercise the real path.

**Browser verification (Blazor dashboard — run `TaskFlow.Blazor` separately, not via AppHost):**
1. Navigate to `https://<blazor-host>/workflows/registry` — confirm three Active workflows listed.
2. `/workflows/new` — drag a few node tiles onto the canvas; click "Import JSON" and paste `ai-task-triage.json` to verify the designer parses it.
3. `/workflows/run` — pick `ai-task-triage`, paste the params JSON above, click Run; check `/instances` for the new instance.
4. `/human-tasks` — only populated after a workflow reaches a `human` node, which requires the agent step to succeed first.
