# Azure Deployment Plan

> **Status:** Complete

Generated: 2026-06-19

---

## 1. Project Overview

**Goal:** Implement Azure AI Foundry plus Foundry Local behind one `Microsoft.Extensions.AI.IChatClient`.

**Path:** Add Components

---

## 2. Requirements

| Attribute | Value |
|-----------|-------|
| Classification | Development |
| Scale | Small |
| Budget | Cost-Optimized |
| Subscription | Not required for this code-only change |
| Location | Not required for this code-only change |

---

## 3. Components Detected

| Component | Type | Technology | Path |
|-----------|------|------------|------|
| TaskFlow.Api | API | ASP.NET Core, .NET Aspire | `src/Host/TaskFlow.Api` |
| AppHost | Orchestrator | .NET Aspire | `src/Host/Aspire/AppHost` |
| TaskFlow.Infrastructure.AI | AI services | Microsoft.Extensions.AI, Microsoft Agent Framework | `src/Infrastructure/TaskFlow.Infrastructure.AI` |
| Test.Aspire | Integration tests | MSTest, Aspire.Hosting.Testing | `src/Test/Test.Aspire` |

---

## 4. Recipe Selection

**Selected:** Existing .NET Aspire app code.

**Rationale:** The repository already contains Aspire orchestration and Azure AI Foundry package references. The requested work is code integration and test verification, not deployment artifact generation.

---

## 5. Architecture

**Stack:** ASP.NET Core API orchestrated by Aspire.

### Service Mapping

| Component | Azure Service | SKU |
|-----------|---------------|-----|
| TaskFlow.Api chat client | Azure AI Foundry deployment when `ConnectionStrings:chat` exists | Existing Aspire default |
| TaskFlow.Api local fallback | Foundry Local SDK in API host only | Local runtime |

### Supporting Services

| Service | Purpose |
|---------|---------|
| Microsoft.Extensions.AI | Common `IChatClient` abstraction |
| Aspire.Azure.AI.Inference | Azure Foundry chat completions client |
| Microsoft.AI.Foundry.Local | Temporary API-host local bootstrap |
| OpenAI SDK | OpenAI-compatible client for Foundry Local web service |

---

## 6. Execution Checklist

### Phase 1: Planning
- [x] Analyze workspace
- [x] Gather requirements
- [x] Confirm subscription and location are not needed for this code-only change
- [x] Scan codebase
- [x] Select recipe
- [x] Plan architecture
- [x] User approved this scoped implementation in chat

### Phase 2: Execution
- [x] Research components
- [x] Add package versions and API-host references
- [x] Implement API-host Foundry Local bootstrap
- [x] Update AppHost local mode to leave local bootstrapping in the API host instead of `RunAsFoundryLocal()`
- [x] Split Azure Aspire smoke from RID-bound Foundry Local smoke
- [x] Update plan status to `Ready for Validation`

### Phase 3: Validation
- [x] Build affected projects
- [x] Run local Foundry tests
- [x] Record validation proof below

---

## 7. Validation Proof

| Check | Command Run | Result | Timestamp |
|-------|-------------|--------|-----------|
| Build affected projects | `rtk dotnet build src\Test\Test.Aspire\Test.Aspire.csproj -m:1` | Passed, 25 projects, 0 errors, 1 warning | 2026-06-19 01:43:31 -04:00 |
| Triage parse guard unit | `rtk dotnet test src\Test\Test.Unit\Test.Unit.csproj --filter FullyQualifiedName~TriageAsync_WithNullSuggestedPriority_ReturnsParseGuardError` | Passed, 1 test | 2026-06-19 01:43:31 -04:00 |
| Foundry Local API-host tests | `rtk dotnet test src\Test\Test.FoundryLocal\Test.FoundryLocal.csproj --filter TestCategory=FoundryLocal --no-build` | Use for current RID-bound local smoke lane | 2026-06-20 |

---

## 8. Files to Generate

| File | Purpose | Status |
|------|---------|--------|
| `.azure/plan.md` | Required Azure plan | Done |
| `src/Host/TaskFlow.Bootstrapper/Registration/FoundryLocalChatClient.cs` | Shared local chat bootstrap | Done |

---

## 9. Next Steps

Current: implementation and validation complete.
