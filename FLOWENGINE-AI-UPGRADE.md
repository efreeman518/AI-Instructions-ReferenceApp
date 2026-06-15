# FlowEngine AI-Integration Upgrade - Handoff Spec

**Audience:** the FlowEngine code agent working in the EF.Packages / FlowEngine source repo (private feed `github.com/efreeman518/EF.Packages`).
**Status:** requirements/spec. This document does not change FlowEngine source; it tells you what to change.
**Consumer:** the TaskFlow reference app (`AI-Instructions-ReferenceApp`) will adopt these changes to run FlowEngine `agent` nodes through Microsoft.Extensions.AI `IChatClient`, so workflows reason against **Foundry Local** in local dev and **Azure AI Foundry** in the cloud - the same model client the rest of the app now uses.

## Why

FlowEngine's current agent connector binds to `Azure.AI.OpenAI.AzureOpenAIClient`:

```csharp
// EF.FlowEngine.Clients.OpenAI, as consumed today in TaskFlow:
fe.AddAzureOpenAIAgentClient(
    clientRef: "ai-agent",
    azureClientFactory: sp => sp.GetRequiredService<AzureOpenAIClient>(),
    deploymentName: chatDeployment,
    modelName: chatDeployment);
```

`AzureOpenAIClient` targets the Azure OpenAI API shape and cannot drive a Foundry Local (OpenAI-compatible) endpoint. The rest of TaskFlow is moving its model access to `Microsoft.Extensions.AI.IChatClient` (registered via Aspire's `Aspire.Azure.AI.Inference` -> `AddAzureAIInferenceChatClient("chat").AsIChatClient()`), which works against **both** Foundry Local and Azure Foundry. FlowEngine must be able to consume that same `IChatClient` so the orchestrated workflow demo runs locally and in Azure without a separate, Azure-only model path.

## Required changes

### 1. New connector that accepts an `IChatClient`

Add a connector registration to `EF.FlowEngine.Clients.OpenAI` (or a new `EF.FlowEngine.Clients.AI` package - your call) that binds an `agent`-node `clientRef` to a DI-resolved `Microsoft.Extensions.AI.IChatClient`:

```csharp
// Desired API (final name your discretion; keep it symmetrical with the existing methods):
fe.AddChatClientAgentClient(
    clientRef: "ai-agent",
    chatClientFactory: sp => sp.GetRequiredService<IChatClient>());
```

- The factory resolves an `IChatClient` from DI. The reference app registers it at the host (`IHostApplicationBuilder.AddAzureAIInferenceChatClient("chat").AsIChatClient()`); the deployment/model is already baked into that client, so the connector should **not** require a `deploymentName`/`modelName` argument (the client knows its model). If you need an optional model override, make it optional.
- Support a keyed-client overload (e.g. resolve a keyed `IChatClient`) for hosts that register more than one model.

### 2. Agent-node execution on Microsoft.Extensions.AI / Microsoft.Agents.AI

The `agent` node executor, when bound to the new connector, must run on the current agent-framework primitives:

- Build the call on `Microsoft.Extensions.AI` (`IChatClient.GetResponseAsync` / structured-output) and/or `Microsoft.Agents.AI` (`ChatClientAgent`).
- Preserve existing node semantics unchanged: `promptTemplate`, `promptVersion`, `outputSchema` (structured JSON output), `idempotencyKey`, `storeAs`, and the `Match`/`Error` edge contract.
- Implement `outputSchema` using `IChatClient` structured/JSON response support (response-format / JSON-schema), so the node still emits a schema-validated object into `Context[storeAs]`.

### 3. Keep the Azure-OpenAI connector as a compatibility option

- Do **not** remove `AddAzureOpenAIAgentClient`. Keep it as one supported connector.
- The new `IChatClient` connector becomes the documented default for new workflows.
- Both connectors must satisfy the same `agent`-node contract so a workflow JSON is connector-agnostic (only the host registration differs).

### 4. Package versions

- Reference the **latest** `Microsoft.Extensions.AI` and `Microsoft.Agents.AI` (and `Microsoft.Extensions.AI.Abstractions`) at build time. Do not pin to an old version.
- Update the FlowEngine package(s) accordingly and ship a new build to the private feed.

## Acceptance criteria

1. A host can register `fe.AddChatClientAgentClient("ai-agent", sp => sp.GetRequiredService<IChatClient>())` and run an `agent` node with no `AzureOpenAIClient` in the container.
2. The existing **`ai-task-triage.json`** workflow (agent -> decision -> human quorum -> integration PATCH, with compensation) runs **unchanged** against:
   - an `IChatClient` bound to a **Foundry Local** model in local dev, and
   - an `IChatClient` bound to an **Azure AI Foundry** deployment in the cloud.
   The `n-classify` agent node must still emit the `{suggestedPriority, suggestedCategory, confidence}` object validated against its `outputSchema` into `Context["triage"]`.
3. `AddAzureOpenAIAgentClient` continues to work for existing consumers (no breaking change).
4. The new connector is documented (XML docs + a short README snippet) with the registration example above.

## Reference-app wiring this must satisfy

The reference app will register the model client at the host and hand it to FlowEngine:

```csharp
// TaskFlow.Api / TaskFlow.Functions host (IHostApplicationBuilder):
builder.AddAzureAIInferenceChatClient("chat")   // Aspire.Azure.AI.Inference; "chat" == Foundry deployment name
       .UseFunctionInvocation()
       .AsIChatClient();                         // registers Microsoft.Extensions.AI.IChatClient in DI

// TaskFlow.Bootstrapper FlowEngine wiring (replaces AddAzureOpenAIAgentClient when IChatClient is present):
fe.AddChatClientAgentClient("ai-agent", sp => sp.GetRequiredService<IChatClient>());
```

When you have shipped this, notify the reference-app workstream so demo **D9** (FlowEngine `ai-task-triage` triggered on task creation) can be wired and verified end-to-end.
