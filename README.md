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

**Workflow orchestration:** EF.FlowEngine 1.0.132 - three AI-driven workflows (`ai-task-triage`, `ai-task-decomposer`, `compliance-check`) with human-in-the-loop, saga compensation, atomic outbox, and agent nodes backed by the Aspire `IChatClient`; Blazor-hosted Dashboard + Designer; admin REST at `/api/flowengine/*`. See [tech-design.md Section 14](docs/tech-design.md#14-workflow-orchestration-flowengine).

Multi-tenant (row-level tenancy). Event-driven async via Service Bus. IaC via Bicep (`infra/`).

**Detailed docs:** [tech-design.md](docs/tech-design.md) - [DESIGN-DECISIONS.md](.scaffold/DESIGN-DECISIONS.md) - [UBIQUITOUS-LANGUAGE.md](.scaffold/UBIQUITOUS-LANGUAGE.md)

## AI Demos (Azure AI Foundry via Aspire)

The app wires a chat model through Aspire. The same `Microsoft.Extensions.AI.IChatClient` backs every AI demo, including the FlowEngine `ai-agent` connector used by D9. Two independent axes apply: **lifecycle** (where the Foundry resource comes from) and **consumption** (this app consumes raw model inference; Foundry projects + server-hosted agents are an Azure-only escalation, documented as commented opt-ins - see *Projects and agents* below). The AppHost chooses the lifecycle/model source at startup:

| Mode | How to enable | Model |
|------|---------------|-------|
| Foundry Local (on-device, no Azure) | set `TASKFLOW_ENABLE_FOUNDRY_LOCAL=true` before `dotnet run --project src/Host/Aspire/AppHost` | `FoundryModel.Local.Qwen2505b` |
| Provision new Azure AI Foundry | set `AiServices:FoundryEndpoint` (config/user-secrets) or `TASKFLOW_USE_AZURE_FOUNDRY=true` with Azure provisioning configured | `FoundryModel.OpenAI.Gpt4oMini` |
| Connect to existing Azure AI Foundry | uncomment the `RunAsExisting` block in `AppHost.cs` and set `AiServices:FoundryResourceName` + `AiServices:FoundryResourceGroup` (the `chat` deployment must already exist there) | `FoundryModel.OpenAI.Gpt4oMini` |
| Disabled (default) | neither variable set | no-op `IChatClient` (app boots; demos return "not configured") |
| Publish | always | provisions a real Azure Foundry resource |

### Run Fully Local With Foundry Local

Use this path when you want the AI demos to call a local model and avoid Azure model calls.

```powershell
winget install Microsoft.FoundryLocal
foundry --version
foundry service status
foundry model info qwen2.5-0.5b
foundry model download qwen2.5-0.5b

$env:TASKFLOW_ENABLE_FOUNDRY_LOCAL = "true"
dotnet run --project src/Host/Aspire/AppHost
```

The AppHost calls `AddFoundry("foundry").RunAsFoundryLocal().AddDeployment("chat", FoundryModel.Local.Qwen2505b)`. That creates the `chat` deployment and injects the `chat` connection into the API and Functions hosts (`ConnectionStrings:chat`, plus `CHAT_ENDPOINT`, `CHAT_APIKEY`, `CHAT_DEPLOYMENT` environment values). The API and Functions hosts then call `AddAzureChatCompletionsClient("chat").AddChatClient()`.

Use `qwen2.5-0.5b` because D3, D7, and D9 exercise tool/function calling. In Foundry Local `0.8.119`, `phi-4` is `chat` only and is not suitable for those demos. If `foundry model list` logs catalog-processing errors, verify with explicit `foundry model info qwen2.5-0.5b` and `foundry service status` instead.

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

Do nothing. When neither Foundry Local nor Azure Foundry is configured, the app still boots and registers a no-op `IChatClient`. D1-D8 return a "not configured" response instead of calling a model. D9 can start, but schema-constrained FlowEngine agent output is expected to fault because the no-op response is not valid model JSON.

### Aspire-backed AI tests

`dotnet test src/Test/Test.Aspire/Test.Aspire.csproj` boots the AppHost through `Aspire.Hosting.Testing`. The test graph includes API, Gateway, and Blazor by default, adds React and Uno when their local assets are runnable, trims only heavy or tool-dependent resources, and chooses model coverage by capability:

| Test condition | Result |
|----------------|--------|
| Azure Foundry config exists (`AiServices:FoundryEndpoint` or `TASKFLOW_USE_AZURE_FOUNDRY=true`) | Azure Foundry smoke runs |
| No Azure config, Foundry Local CLI/service/model probe succeeds | Foundry Local smoke runs |
| No model provider found | no-op AI fallback tests run |

`TASKFLOW_LIVE_AI_BASE_URL` can override the request target for manual live AI smoke runs. It is not a test opt-in.

### Aspire-backed UI tests

`dotnet test src/Test/Test.PlaywrightUI/Test.PlaywrightUI.csproj` boots the AppHost through `Aspire.Hosting.Testing`, runs the C# Gateway/Blazor happy-path smoke with `Microsoft.Playwright`, and invokes the installed TypeScript Playwright projects for Blazor and React when their local prerequisites are present.

The C# page objects stay intentionally narrow: Gateway root/`/alive` plus Blazor `/tasks`. React and Uno coverage remains in the existing TypeScript suites. `PLAYWRIGHT_GATEWAY_URL`, `PLAYWRIGHT_BLAZOR_URL`, `PLAYWRIGHT_REACT_URL`, and `PLAYWRIGHT_UNO_URL` are target overrides, not test opt-ins. Uno is not selected automatically by the .NET adapter because built WASM assets alone do not prove the app has booted in the browser; use `npm run test:uno` or `PLAYWRIGHT_UNO_URL` when targeting a verified Uno host.

### Projects and agents (opt-in, Azure-only)

The demos above use **code-hosted** agents - a `ChatClientAgent` running in-process over the injected `IChatClient`. That works with every lifecycle mode and boots offline. **Server-hosted** Foundry agents are an Azure-only escalation for hosted memory, centralized tools, or portal/IaC-managed agent definitions. They are documented and wired as commented opt-ins; nothing here runs by default.

- **Aspire-modeled project + prompt agent** (commented in `AppHost.cs`). A project (`foundry.AddProject(...)`) is the container for server-hosted agents, deployments, and tool connections. A prompt agent (`project.AddPromptAgent(model, "name", instructions).WithTool(...)`) is declarative. Tools are project-level resources (code interpreter, web/AI-Search/Bing grounding, function calling). Note: **prompt agents always deploy to Azure Foundry, even under `aspire run`** - there is no offline path. Referencing the project injects `PROJ_URI` (the project endpoint) into the consuming host.

- **Pre-existing agents via the client SDK** (commented in `TaskFlow.Api/Program.cs` `ConfigureChatClient`). When an agent is created in the Foundry portal or by IaC, connect to the existing project endpoint and drive it with `AIProjectClient.AsAIAgent(...)`. Add `Azure.AI.Projects` + `Microsoft.Agents.AI.Foundry`, set `AiServices:FoundryProjectEndpoint` (or read the Aspire-injected `PROJ_URI`) and `AiServices:FoundryAgentName`:

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
