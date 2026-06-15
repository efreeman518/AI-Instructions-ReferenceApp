# Handoff - Azure AI Foundry: Install Prerequisites + Integrate Updated FlowEngine

This document is self-contained. It assumes **no prior conversation context**. Read it top to bottom; everything you need is here or linked from here.

## Current status update - 2026-06-13

Track A read-side/live model path and Track B are complete in this working tree. FlowEngine packages were bumped to `1.0.132`, `EF.FlowEngine.Clients.AI` was added, the `ai-agent` connector now uses `AddChatClientAgentClient` over the shared Aspire `IChatClient`, and `TaskFlow.Functions` starts the `ai-task-triage` workflow when it consumes `TaskItemCreatedEvent`. Foundry Local `0.8.119` was installed/verified, `qwen2.5-0.5b` was downloaded, D1/D2/D3/D7 returned configured model responses through the Gateway, and the Aspire-hosted Blazor `/ai-chat` page rendered. Verified:

```powershell
dotnet build src/TaskFlow.slnx --no-restore -v minimal
dotnet test src/Test/Test.Unit/Test.Unit.csproj --no-build -v minimal
dotnet test src/Test/Test.Integration.FlowEngine/Test.Integration.FlowEngine.csproj --no-build -v minimal
```

Remaining work: optional authenticated write-path verification for D4/D5/D6/D9 side effects. Keep this file only if you want that follow-up; `FLOWENGINE-AI-UPGRADE.md` is no longer needed for package integration.

## 0. Orientation (read first)

### What this is
A reference .NET app ("TaskFlow") got a set of Azure AI Foundry AI demos wired through .NET Aspire. Most of that work is **already done and building**. Current remaining work:
- **Track A** - install/verify the runtime components so the AI demos actually execute against a model, and fix any prerequisite-doc gaps you find.
- **Track B** - complete as of 2026-06-13; FlowEngine `1.0.132` is consumed and workflow-orchestrated triage ("D9") is wired.

### The two repos involved
| Repo | Path | Role |
|------|------|------|
| Reference app (this repo) | `C:\Users\EbenFreeman\source\repos\AI-Instructions-ReferenceApp` | The TaskFlow app you build/run/verify. |
| Scaffold instructions | `C:\Users\EbenFreeman\source\repos\AI-Instructions-Scaffold` | Markdown guidance these patterns came from. Its AI/Aspire docs were **already updated** to match this work - do not redo that. You only touch it again if Track B changes a documented method name (Step B7). |

You do **not** have access to the FlowEngine source repo (it ships as a private NuGet package on the `efreeman518-github` feed). You are a **consumer** of that package, not its editor. The file [FLOWENGINE-AI-UPGRADE.md](FLOWENGINE-AI-UPGRADE.md) is a spec for a *different* agent working in the FlowEngine repo; you only act once that package is published.

### Environment assumptions
- OS/shell: **Windows 11, PowerShell**. Commands below use PowerShell syntax (`$env:VAR = "x"`, `winget`). Adapt if your shell differs.
- Python: use **`py -3`** (the repo's `.venv` is broken; `py -3` has `pyyaml`/`jsonschema`).
- Run tools (`dotnet`, `curl`, `git`, `winget`, `foundry`, `func`, `az`) **directly**. Ignore any reference elsewhere to an `rtk` wrapper - that belonged to a different harness and does not apply to you.
- **Do not `git commit` or `git push` unless explicitly asked.** Avoid em dashes, emoji, and decorative unicode in code/docs/commits (repo house style).

### Current state going in (the baseline you must not regress)
- Solution file: `src/TaskFlow.slnx`. It **builds clean: 38 projects, 0 errors** (a pre-existing transitive `MessagePack` NU1903 vulnerability warning is expected and unrelated).
- `dotnet test src/Test/Test.Unit/Test.Unit.csproj` -> **240 tests pass.**
- AI runs in **no-op mode by default**: with no model wired, an injected `NoOpChatClient` returns a "not configured" message and the app still boots. Demos D1-D9 are implemented; D9 starts on `TaskItemCreatedEvent` and routes to the workflow fault path until a real model produces schema-valid agent output.

### What was already changed this round (so you can orient, not redo)
Reference app - created: `src/Infrastructure/TaskFlow.Infrastructure.AI/NoOpChatClient.cs`; `.../Demos/{AiDemoModels,TaskTriageService,TaskDraftService,NextActionAdvisor,AiTaskReviewer}.cs`; `src/Host/TaskFlow.Api/Endpoints/AiDemoEndpoints.cs`; `src/UI/TaskFlow.Blazor/Components/Pages/AiChat.razor`; `FLOWENGINE-AI-UPGRADE.md`; this file.
Reference app - modified: `src/Directory.Packages.props`; `src/Host/Aspire/AppHost/{AppHost.cs,AppHost.csproj}`; `src/Host/TaskFlow.Api/{Program.cs,TaskFlow.Api.csproj,WebApplicationBuilderExtensions.cs}`; `src/Infrastructure/TaskFlow.Infrastructure.AI/{ServiceCollectionExtensions.cs,TaskFlow.Infrastructure.AI.csproj,Agents/TaskAssistantAgentService.cs}`; `src/Host/TaskFlow.Functions/{Program.cs,TaskFlow.Functions.csproj,FunctionServiceBusTrigger.cs}`; `src/UI/TaskFlow.Blazor/Program.cs`; `.../Components/Layout/MainLayout.razor`; `src/Test/Test.Unit/AI/AiServiceRegistrationTests.cs`; `README.md`.
Scaffold (done, do not redo): `skills/ai-integration.md`, `skills/aspire.md`, `ai/resource-implementation-schema.md`, `patterns/infrastructure-wiring.md`, `skills/package-dependencies.md`.

### How the model is wired (mental model for the whole feature)
The AppHost (`src/Host/Aspire/AppHost/AppHost.cs`) optionally creates a Foundry deployment resource named `chat` and references it from the API and Functions projects. That injects `CHAT_ENDPOINT` / `CHAT_APIKEY` / `CHAT_DEPLOYMENT`. The hosts register a `Microsoft.Extensions.AI.IChatClient` from that connection (`builder.AddAzureChatCompletionsClient("chat").AddChatClient()`). Every AI demo resolves that one `IChatClient`. Selection:

| Mode | Enable | Model |
|------|--------|-------|
| Foundry Local (on-device, no Azure) | `$env:TASKFLOW_ENABLE_FOUNDRY_LOCAL = "true"` before launch | `FoundryModel.Local.Qwen2505b` |
| Real Azure AI Foundry | set `AiServices:FoundryEndpoint`, or `$env:TASKFLOW_USE_AZURE_FOUNDRY = "true"` with Azure provisioning configured | `FoundryModel.OpenAI.Gpt4oMini` |
| Disabled (default) | neither set | no-op `IChatClient` |
| Publish (`aspire publish`) | always | provisions a real Azure Foundry resource |

Pinned package versions (in `src/Directory.Packages.props`; the two Aspire ones are preview-only with no stable release, pinned with an inline reason): `Aspire.Hosting.Foundry` and `Aspire.Azure.AI.Inference` at `13.4.3-preview.1.26305.13`; `Microsoft.Extensions.AI` `10.7.0`; `Microsoft.Agents.AI` `1.10.0`.

### Known caveat - write-path demos need a tenant (read before reporting D4/D5/D6 as broken)
TaskFlow is multi-tenant; `ITaskItemService` enforces a tenant boundary from the caller's claims. The local dev gateway is **unauthenticated**, so there is no tenant claim. Read/stateless demos work regardless (**D1, D2, D3, D7**). The write-path demos can fail with a tenant/boundary error when called without a tenant: **D4** (applies a priority), **D5** (creates a task), **D6** (posts a comment). This is a pre-existing property of the app, not a bug in the demos (the existing `/api/v1/agent/chat` tool that creates tasks behaves the same). If you need D4/D5/D6 to mutate, supply a tenant: send an authenticated request, or set a `tenant_id` claim/header per how `TaskFlow.Api` resolves tenancy (start from the tenant-boundary validator used in `src/Application/TaskFlow.Application.Services/TaskItemService.cs` and the auth setup in `src/Host/TaskFlow.Api`). The inference itself runs regardless; only the persistence step is gated.

---

## Track A - Install prerequisites and verify the demos

### A1. Baseline toolchain (verify; install only if missing)
```powershell
dotnet --version            # expect 10.0.x (repo targets net10.0); install .NET 10 SDK if older
docker info                 # or: podman info   (SQL/Redis/Service Bus/Cosmos emulators)
aspire --version            # Aspire CLI; if missing you can still launch via `dotnet run --project src/Host/Aspire/AppHost`
```

### A2. Foundry Local (on-device model path; no Azure subscription needed)
```powershell
winget install Microsoft.FoundryLocal      # Windows
# macOS: brew install microsoft/foundrylocal/foundrylocal
foundry --version                          # must be on PATH
foundry model list                         # confirm a local catalog is reachable
```
First model use downloads model weights. The AppHost requests `FoundryModel.Local.Qwen2505b`, which maps to the Foundry Local `qwen2.5-0.5b` alias and supports `chat, tools`. This matters because D3/D7/D9 use function tools. `phi-4` is chat-only in Foundry Local `0.8.119` and returns a schema/tool error for these demos.

### A3. SQL password user-secret (AppHost requires it before launch)
```powershell
dotnet user-secrets init --project src/Host/Aspire/AppHost
dotnet user-secrets set "Parameters:sql-password" "<StrongPassword!>" --project src/Host/Aspire/AppHost
```

### A4. (Optional) Real Azure AI Foundry instead of local
```powershell
az login
dotnet user-secrets set "AiServices:FoundryEndpoint" "https://<your-foundry>.services.ai.azure.com/" --project src/Host/TaskFlow.Api
$env:TASKFLOW_USE_AZURE_FOUNDRY = "true"
```
If the resource uses managed identity (no key), the Inference client registration in `src/Host/TaskFlow.Api/Program.cs` (method `ConfigureChatClient`) may need a `DefaultAzureCredential` passed via the settings overload. Apply only if the key-based connection string from Aspire is unavailable.

### A5. (Optional) Azure Functions Core Tools - only to run D6 locally
```powershell
npm i -g azure-functions-core-tools@4 --unsafe-perm true   # provides func.exe
func --version
```

### A6. Build, run, and verify
```powershell
dotnet build src/TaskFlow.slnx                 # expect 38 projects, 0 errors
dotnet test src/Test/Test.Unit/Test.Unit.csproj  # expect 240 passing
$env:TASKFLOW_ENABLE_FOUNDRY_LOCAL = "true"    # use the on-device model (omit to confirm no-op path)
dotnet run --project src/Host/Aspire/AppHost
```
URLs are assigned at runtime - read them from the Aspire dashboard console output, do not assume ports. In the dashboard, confirm the `foundry` / `chat` resource is healthy and that `taskflowapi` received `CHAT_ENDPOINT`. Then exercise the demos (endpoints live under the versioned group and are reachable via the gateway URL from the dashboard):

```powershell
$base = "<gateway-or-api-base-url-from-dashboard>"
# D1 - basic completion
curl -X POST "$base/api/v1/ai/chat" -H "Content-Type: application/json" -d '{"message":"Summarize what TaskFlow does in one sentence."}'
# D2 - streaming (Server-Sent Events)
curl -N -X POST "$base/api/v1/ai/chat/stream" -H "Content-Type: application/json" -d '{"message":"Three tips for managing a backlog."}'
# D3 - conversational tool-calling agent
curl -X POST "$base/api/v1/agent/chat" -H "Content-Type: application/json" -d '{"message":"Summarize my backlog."}'
# D7 - read-only multi-tool advisor
curl -X POST "$base/api/v1/ai/next-action"
# D4 / D5 - WRITE PATH: see the tenant caveat in section 0 before expecting these to persist
curl -X POST "$base/api/v1/ai/triage/<taskId>?apply=true"
curl -X POST "$base/api/v1/ai/tasks/draft" -H "Content-Type: application/json" -d '{"title":"Migrate auth to Entra"}'
```
D8: open the Blazor host's `/ai-chat` page and try Chat / Stream / Agent + "Suggest next action". D6: with the Functions host and Service Bus emulator running and a real model wired, create a task and confirm an "AI readiness review:" comment appears on it (subject to the tenant caveat).

Success bar: D1/D2/D3/D7/D8 return real model output (not the "not configured" no-op string); the build and unit tests stay green.

### A7. Update prerequisite docs for any gap you hit
If any step above was missing/wrong, fix it here, and in:
- `README.md` (AI Demos section, this repo).
- `../AI-Instructions-Scaffold/skills/aspire.md` (Preflight item 6) and `skills/ai-integration.md` (Aspire section).
After verifying the live model path, update `.scaffold/REFERENCE-STATUS.md` and `HANDOFF.md` to record it and the model/runtime versions used. Re-validate scaffold docs after any edit there:
```powershell
cd ../AI-Instructions-Scaffold; py -3 scripts/validate-instructions.py   # expect: all checks passed
```

---

## Track B - Completed FlowEngine package and D9 wiring

Completed on 2026-06-13:

- FlowEngine package pins are `1.0.132`.
- `EF.FlowEngine.Clients.AI` is referenced from `TaskFlow.Bootstrapper`.
- The verified connector method is `AddChatClientAgentClient`.
- `ai-agent` resolves the shared Aspire `IChatClient`, so FlowEngine agent nodes use the same model path as D1-D8.
- The unused direct Azure OpenAI SDK registration and package references were removed.
- `IWorkflowTrigger` is registered in application DI.
- `TaskFlow.Functions.FunctionServiceBusTrigger` starts `ai-task-triage` when it consumes `TaskItemCreatedEvent`.
- README, HANDOFF, REFERENCE-STATUS, and tech-design docs were updated.

Runtime note: with no real model wired, D9 still starts, but the no-op `IChatClient` response is not schema-valid for `n-classify`, so the workflow routes to the faulted output. Track A still needs live Foundry Local or Azure verification.

---

## Guardrails (apply throughout)
- Verify every external API member name against the restored package (its `.xml` doc or IntelliSense) before writing a call site - the Foundry/Inference packages are preview and the FlowEngine connector name is unconfirmed.
- Keep preview packages pinned with a one-line inline reason in `Directory.Packages.props`.
- Do not regress the baseline: `src/TaskFlow.slnx` builds (38 projects) and `Test.Unit` (240 tests) stays green.
- Do not commit or push unless explicitly asked.
