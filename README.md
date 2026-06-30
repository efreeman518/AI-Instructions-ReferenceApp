# AI-Instructions-ReferenceApp

Reference implementation for [AI-Instructions-Scaffold](https://github.com/efreeman518/AI-Instructions-Scaffold).

## Description

**TaskFlow** - a production-grade reference app demonstrating AI-assisted development patterns for a multi-tenant task management system. Built with Clean Architecture and Domain-Driven Design, running local with Aspire, deployable to Azure.

## Architecture

| Layer | Path | Responsibility |
|-------|------|----------------|
| Domain | `src/Domain/` | Aggregates, entities, domain events |
| Application | `src/Application/` | Use cases, service contracts, message handlers |
| Infrastructure | `src/Infrastructure/` | EF Core, Azure AI, Storage, Repos |
| Host | `src/Host/` | API, Functions, Scheduler, Gateway, Aspire |

**Azure services:** SQL Server, Cosmos DB, Service Bus, Blob Storage, Azure AI Search, Microsoft Foundry.

**Workflow orchestration:** EF.FlowEngine - three AI-driven workflows (`ai-task-triage`, `ai-task-decomposer`, `compliance-check`) with human-in-the-loop, saga compensation, atomic outbox, and agent nodes backed by the Aspire `IChatClient`; Blazor-hosted Dashboard + Designer; admin REST at `/api/flowengine/*`. See [Tech Design Section 14](docs/tech-design.html#14-workflow-orchestration-flowengine).

Multi-tenant (row-level tenancy). Event-driven async via Service Bus. IaC via Bicep (`infra/`).

**Detailed docs:** [Tech Design](docs/tech-design.html) - [Tech Design Maintenance](docs/TECH-DESIGN-MAINTENANCE.md) - [DESIGN-DECISIONS.md](.scaffold/DESIGN-DECISIONS.md) - [UBIQUITOUS-LANGUAGE.md](.scaffold/UBIQUITOUS-LANGUAGE.md)

## AI Demos (Azure AI Foundry and Foundry Local)

The app wires one `Microsoft.Extensions.AI.IChatClient` for every AI demo, including the FlowEngine `ai-agent` connector used by D9. Azure Foundry is still modeled by Aspire as a `chat` deployment. Foundry Local is temporarily bootstrapped by `TaskFlow.Bootstrapper` with `Microsoft.AI.Foundry.Local` because Aspire `RunAsFoundryLocal()` is avoided until its bundled SDK can discover the GA runtime. Two independent axes apply: **lifecycle** (where the Foundry resource comes from) and **consumption** (this app consumes raw model inference; Foundry projects + server-hosted agents are an Azure-only escalation, documented as commented opt-ins - see *Projects and agents* below).

| Mode | How to enable | Model |
|------|---------------|-------|
| Foundry Local (on-device, no Azure) | default when Azure Foundry is not configured and `AiServices:DisableFoundryLocal` is false | `qwen2.5-0.5b` |
| Provision new Azure AI Foundry | set `AiServices:FoundryEndpoint` (config/user-secrets) or `TASKFLOW_USE_AZURE_FOUNDRY=true` with Azure provisioning configured | `FoundryModel.OpenAI.Gpt4oMini` |
| Connect to existing Azure AI Foundry | uncomment the `RunAsExisting` block in `AppHost.cs` and set `AiServices:FoundryResourceName` + `AiServices:FoundryResourceGroup` (the `chat` deployment must already exist there) | `FoundryModel.OpenAI.Gpt4oMini` |
| Disabled | set `AiServices:DisableFoundryLocal=true`, or leave Azure absent and local bootstrap unavailable | no-op `IChatClient` (app boots; demos return "not configured") |
| Publish | always | provisions a real Azure Foundry resource |

### Run Fully Local With Foundry Local

Use this path when you want the AI demos to call a local model and avoid Azure model calls.

```powershell
dotnet run --project src/Host/Aspire/AppHost
```

The AppHost wires no `chat` resource for local mode. When Azure is absent and `AiServices:DisableFoundryLocal` is false, the bootstrapper uses `Microsoft.AI.Foundry.Local` directly, downloads execution providers and the `qwen2.5-0.5b` model if needed, starts its OpenAI-compatible endpoint at `AiServices:LocalWebUrl` (default `http://127.0.0.1:52415`), records `AiProviderInfo("local")`, and registers it as the shared `IChatClient`. If Foundry Local cannot bootstrap, the bootstrapper logs the failure and falls back to no-op AI with `AiProviderInfo("none")`.

Use `qwen2.5-0.5b` because D3, D7, and D9 exercise tool/function calling. First run can be slow because the SDK downloads execution providers and the model into the local Foundry cache.

### Run With Real Azure AI Foundry

Use this path when you have a real Azure AI Foundry deployment or when testing the publish shape.

```powershell
dotnet user-secrets set "AiServices:FoundryEndpoint" "https://<your-foundry-resource>.services.ai.azure.com/" --project src/Host/Aspire/AppHost
# or
$env:TASKFLOW_USE_AZURE_FOUNDRY = "true"

dotnet run --project src/Host/Aspire/AppHost
```

In this mode the AppHost calls `AddFoundry("foundry").AddDeployment("chat", FoundryModel.OpenAI.Gpt4oMini)`. `aspire publish` always takes the real Azure path and provisions an Azure AI Foundry resource/deployment. If your Azure tenant requires keyless managed-identity auth for the inference client, adjust the host registration around `AddAzureChatCompletionsClient("chat")` to pass the required credential shape before treating local failures as model failures.

### Run With AI Disabled

Set `AiServices:DisableFoundryLocal=true` to force no-op locally. When no provider is active, the app still boots and registers a no-op `IChatClient`. `GET /api/v1/ai/status` reports `provider: none`, D1-D8 return a "not configured" response instead of calling a model, and D9 can start but schema-constrained FlowEngine agent output is expected to fault because the no-op response is not valid model JSON.

### Aspire-backed AI tests

`dotnet test src/Test/Test.Aspire/Test.Aspire.csproj --filter TestCategory=Foundry` boots the AppHost through `Aspire.Hosting.Testing` and runs the Azure live Foundry smoke set only when Azure Foundry is configured. The Aspire mesh is RID-free and forces `AiServices:DisableFoundryLocal=true`, so it never starts a local native model. App-level AI HTTP contract coverage lives in `Test.Endpoints` with a fake `IChatClient`.

`dotnet test src/Test/Test.FoundryLocal/Test.FoundryLocal.csproj --filter TestCategory=FoundryLocal` runs the RID-bound local smoke lane. It boots `TaskFlow.Api` directly, checks `GET /api/v1/ai/status` for `provider: local`, then covers chat, no-tool agent chat, and one safe write-adjacent AI demo. If the local runtime is missing or undiscoverable, the tests are inconclusive rather than green on no-op.

| Test condition | Result |
|----------------|--------|
| Azure Foundry config exists (`AiServices:FoundryEndpoint` or `TASKFLOW_USE_AZURE_FOUNDRY=true`) | `Test.Aspire` `TestCategory=Foundry` runs against Azure Foundry |
| No Azure Foundry config exists | `Test.Aspire` live Foundry tests return inconclusive |
| Foundry Local SDK bootstraps in the RID-bound API host | `Test.FoundryLocal` `TestCategory=FoundryLocal` runs against the local model |
| Foundry Local runtime missing or undiscoverable | `Test.FoundryLocal` returns inconclusive |

`TestCategory=AzureFoundry` is reserved for Azure-specific provider-selection or provisioning checks. The no-op AI fallback path is covered by unit and endpoint tests. Load, benchmark, and mobile suites stay explicit because they require a running target, BenchmarkDotNet process control, or Appium/emulator setup.

`TASKFLOW_LIVE_AI_BASE_URL` can override the request target for manual live AI smoke runs. It is not a test opt-in.

Scaffold agents should preserve these AI test contracts:

- Provider order is Azure Foundry first, then Foundry Local, then no-op. Azure is active only when Aspire injects `ConnectionStrings:chat` or Azure Foundry config is set. Foundry Local is active when Azure is absent and `AiServices:DisableFoundryLocal=false`.
- RID-free suites (`Test.Unit`, `Test.Endpoints`, `Test.Aspire`) do not start native Foundry Local. They use fake clients or `AiServices:DisableFoundryLocal=true`.
- `Test.FoundryLocal` is the only RID-bound local model lane. Missing or undiscoverable Foundry Local runtime is inconclusive. Installed-but-broken runtime, provider mismatch, no-op fallback, or live endpoint timeout is failure.
- Code-hosted agent smoke that does not need tools sends `AgentChatRequest.UseTools=false`; the service maps it to `ChatToolMode.None`. Tool-calling tests must request tools explicitly and carry their own timeout budget.

### CI test lanes

GitHub Actions runs the fast, no-Docker gate on every push and pull request: Unit, Architecture, Endpoint, and FlowEngine definition tests. `workflow_dispatch` exposes the heavier lanes as explicit inputs:

| Input | Test project | Notes |
|-------|--------------|-------|
| `includeE2E` | `Test.E2E` | SQL-backed HTTP workflows; requires Docker |
| `includeIntegration` | `Test.Integration` | SQL + Azurite component tests; requires Docker |
| `includeAspireMesh` | `Test.Aspire` | Full Aspire AppHost graph; Azure Foundry smoke returns inconclusive unless configured |
| `includeFoundryLocal` | `Test.FoundryLocal` | RID-bound Foundry Local live smoke; may download local model assets |
| `includePlaywrightUI` | `Test.PlaywrightUI` | Browser smoke path; restores Playwright and React npm packages |

### Aspire-backed UI tests

`dotnet test src/Test/Test.PlaywrightUI/Test.PlaywrightUI.csproj` boots the AppHost through `Aspire.Hosting.Testing`, runs the C# Gateway/Blazor happy-path smoke with `Microsoft.Playwright`, and invokes the installed TypeScript Playwright projects for Blazor, React, and Uno when their local prerequisites are present.

The C# page objects stay intentionally narrow: Gateway root/`/alive` plus Blazor `/tasks`. React coverage remains DOM/ARIA based. Uno coverage is canvas-first: wait for painted canvas, click stable app chrome, compare visual fingerprints. Do not assert Uno Skia text through DOM selectors. `PLAYWRIGHT_GATEWAY_URL`, `PLAYWRIGHT_BLAZOR_URL`, `PLAYWRIGHT_REACT_URL`, and `PLAYWRIGHT_UNO_URL` are target overrides, not test opt-ins. Uno is selected by the .NET adapter unless `TASKFLOW_WASM_TESTS_ENABLED=false`; the C# `WasmAppHost` fixture restores/builds browserwasm with separate 10-minute restore and 20-minute build budgets, starts it through Aspire, and passes the dynamic endpoint to TypeScript Playwright. `TASKFLOW_PLAYWRIGHT_PROJECT_TIMEOUT_SECONDS` bounds each TypeScript project process; `TASKFLOW_PLAYWRIGHT_TEST_TIMEOUT_SECONDS` bounds each Playwright test.

### Projects and agents (opt-in, Azure-only)

The demos above use **code-hosted** agents - a `ChatClientAgent` running in-process over the injected `IChatClient`. That works with every lifecycle mode and boots offline. **Server-hosted** Foundry agents are an Azure-only escalation for hosted memory, centralized tools, or portal/IaC-managed agent definitions. They are documented and wired as commented opt-ins; nothing here runs by default.

No `.foundry/agent-metadata.yaml` is committed because no server-hosted agent participates in Foundry deploy/eval workflows yet. When enabling a hosted or prompt agent, create `.foundry/agent-metadata.yaml` under that agent source folder and keep the project endpoint, agent name, datasets, evaluators, and thresholds there.

- **Aspire-modeled project + prompt agent** (commented in `AppHost.cs`). A project (`foundry.AddProject(...)`) is the container for server-hosted agents, deployments, and tool connections. A prompt agent (`project.AddPromptAgent(model, "name", instructions).WithTool(...)`) is declarative. Tools are project-level resources (code interpreter, web/AI-Search/Bing grounding, function calling). Note: **prompt agents always deploy to Azure Foundry, even under `aspire run`** - there is no offline path. Referencing the project injects `PROJ_URI` (the project endpoint) into the consuming host.

- **Pre-existing agents via the client SDK** (bootstrapper-owned provider extension). When an agent is created in the Foundry portal or by IaC, connect to the existing project endpoint and drive it with `AIProjectClient.AsAIAgent(...)`. Add `Azure.AI.Projects` + `Microsoft.Agents.AI.Foundry`, set `AiServices:FoundryProjectEndpoint` (or read the Aspire-injected `PROJ_URI`) and `AiServices:FoundryAgentName`:

  ```csharp
  var project = new AIProjectClient(new Uri(projectEndpoint), credential);
  // code-first responses agent (no server-side resource created):
  AIAgent agent = project.AsAIAgent(model: deploymentName, name: "TaskAssistant", instructions: prompt);
  // or bind to a pre-existing versioned agent by name:
  var record = await project.AgentAdministrationClient.GetAgentAsync(agentName);
  AIAgent agent = project.AsAIAgent(record);
  ```

  Both results are `Microsoft.Agents.AI.AIAgent`, so `ITaskAssistantAgent` can wrap either path - only construction differs from the code-hosted `ChatClientAgent`.

Use the Aspire dashboard to discover the Gateway and Blazor URLs. Do not hardcode ports; Aspire allocates them per run. D4/D5/D6/D9 write or enqueue side effects and require the normal tenant/auth context. Local scaffold auth provides a predictable development tenant, but production verification should use a real authenticated tenant.

**Demos** (all under `/api/v1/...`, reachable through the gateway; the Aspire-hosted Blazor `AI Chat` page exercises the chat/agent ones):

| # | Concept | Surface |
|---|---------|---------|
| D1 | Basic completion | `POST /api/v1/ai/chat` |
| D2 | Streaming completion (SSE) | `POST /api/v1/ai/chat/stream` |
| D3 | Conversational tool-calling agent | `POST /api/v1/agent/chat` |
| D4 | Structured classification (triage) | `POST /api/v1/ai/triage/{taskId}?apply=true` |
| D5 | Generative enrichment on create | `POST /api/v1/ai/tasks/draft` |
| D6 | Async event-driven inference (readiness review) | created tasks -> Functions handler -> task comment |
| D7 | Read-only multi-tool reasoning | `POST /api/v1/ai/next-action` |
| D8 | Blazor chat UI | `/ai-chat` |
| D9 | Workflow-orchestrated triage | created tasks -> Functions handler -> `ai-task-triage` FlowEngine workflow |

D9 uses the same `IChatClient` as D1-D8. With AI disabled, the workflow still starts but the no-op model response is expected to route the agent node to the faulted output.

## Local Mobile UI Tests

Use `src/Test/Test.Mobile/run-mobile-tests.ps1` for Android local runs. Test methods do not start Appium or emulators; the runner starts or verifies those dependencies, then enables the mobile lane.

TaskFlow mobile smoke tests use MSTest + Appium for the Uno Android/iOS heads. Android runs locally on Windows; iOS requires macOS or a Mac host with Xcode.

Before building the Android package, restore the Uno app with all mobile targets included:

```powershell
dotnet restore src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj -p:BuildAllUnoTargets=true
dotnet build src/UI/TaskFlow.Uno/TaskFlow.Uno.csproj -p:TargetFrameworkOverride=net10.0-android -p:UseMocks=true --no-restore -m:1
```

The explicit `BuildAllUnoTargets=true` restore is required because the Uno project defaults to a fast Wasm-only restore for local web work. Android/Appium runs need the platform Skia runtime packages in the NuGet asset graph.

Full Appium setup and run commands live in [src/Test/Test.Mobile/README.md](src/Test/Test.Mobile/README.md).

## Mutation Testing

TaskFlow has a focused Stryker.NET sample project at [src/Test/Test.Mutation](src/Test/Test.Mutation/README.md). It mutates selected domain files and runs MSTest samples that show boundary, failure-message, status-transition, and idempotency checks.

```powershell
dotnet tool restore
dotnet test src/Test/Test.Mutation/Test.Mutation.csproj
```

Run Stryker from `src/Test/Test.Mutation`:

```powershell
dotnet tool run dotnet-stryker
```

## Phase 1 Alignment Artifacts

- `.scaffold/domain-specification.yaml`
- `.scaffold/UBIQUITOUS-LANGUAGE.md`
- `.scaffold/DESIGN-DECISIONS.md`
