# StockAware: weekend readiness command center

A console-first retail multi-agent application that brings walkthroughs 01–09 together in one business story. All AI responses use Microsoft Foundry; all retail records are synthetic. A **real stdio MCP connection** to [StockAwareMcp](../StockAwareMcp/README.md) supplies evidence and persists local transfer proposals and stock changes.

## What the audience sees

It is Friday before a promotion weekend. Store 047 has milk and coffee shortages. Store 112's apparent milk surplus is mostly reserved. Store 208 has a banana quality hold, despite apparently adequate recorded stock. A supplier catalog incident affects promotion SKUs, but is **not evidence of physical stock loss**.

```text
InventoryAnalyst -> SupplierInvestigator -> DecisionLead
        sequential Agent Framework workflow
                         |
                evidence-aware AI judge
                   refine / complete
                         |
            StockAware streamed conversation
                         |
             MCP local transfer proposal
                         |
              human /approve confirmation
                         |
           atomic local stock + audit update
```

The fixture's clock is fixed at **16 October 2026, 17:15 UTC**, protecting opening on **17 October at 12:00 UTC**. Retrieval timestamps are actual wall-clock time. This is not a current inventory feed.

## Concept map

| Walkthrough | Integrated surface | Presenter cue |
| --- | --- | --- |
| 01: First agent | Foundry-backed named agents; streamed console replies | "The model reasons; the application owns execution." |
| 02: Tools | Eleven discoverable MCP tools; explicit per-agent tool allowlist | Show the real tool arguments and `MCP-001` evidence result. |
| 03: Multi-turn | One copilot session across follow-ups | Ask about "that store" without repeating its ID. |
| 04: Memory | File-backed chat history plus serialized session | Exit, restart, and recall the earlier conversation. |
| 05: Workflows | Inventory → supplier → decision via `AgentWorkflowBuilder` | Show each specialist's labeled handoff. |
| 06: Harness | `/research` uses a separate planning/todo/hosted-search agent | Research a public retail topic with source URLs. |
| 07: Hosting | Local ASP.NET Core HTTP/A2A, discovery, context IDs, SSE | Send the supplied HTTP requests from VS Code. |
| 08: Looping | `LoopAgent` with a ledger-aware `AIJudgeLoopEvaluator` | Show real REFINE feedback or a complete first pass. |
| 09: Skills | The supplier specialist loads `supplier-feed-triage` and its references | Contrast reusable procedure with raw diagnostic tools. |

These are running framework features, not simulated model responses. There is no offline AI fallback.

## Setup

Run from the workspace root, with .NET 10 and Azure CLI installed:

```powershell
az login
dotnet user-secrets set "StockAware:Endpoint" "https://YOUR-RESOURCE.services.ai.azure.com/api/projects/YOUR-PROJECT" --project .\StockAware
dotnet user-secrets set "StockAware:DeploymentName" "YOUR-DEPLOYMENT" --project .\StockAware
dotnet build .\StockAware.slnx
```

The existing shared `stockaware-meijer-demo` secret store is reused. Use a deployed model that supports **Chat Completions and function calling**. `/research` additionally requires **Responses and hosted web search**. The Azure CLI identity needs access to the Foundry project and deployment; configuration does not grant permissions.

| Setting | Environment variable | Behavior |
| --- | --- | --- |
| `StockAware:Endpoint` | `StockAware__Endpoint` | Required HTTPS Foundry **project** endpoint. |
| `StockAware:DeploymentName` | `StockAware__DeploymentName` | Required actual deployment name. |
| `StockAware:DataDirectory` | `StockAware__DataDirectory` | Default: `%LOCALAPPDATA%\StockAware`. Shared with the MCP child. |
| `StockAware:McpProject` | `StockAware__McpProject` | Optional absolute MCP project path; discovered automatically in this checkout. |

Environment variables override user secrets for the same key. Legacy `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_DEPLOYMENT_NAME` are fallback aliases only; the endpoint alias still needs a project endpoint. `.env` files are not loaded. No credentials are written into source or local state.

## Recommended live presentation: 15–20 minutes

Use a **new data directory per rehearsal** so a previous approved transfer does not alter the opening scene:

```powershell
$env:StockAware__DataDirectory = Join-Path $env:LOCALAPPDATA ("StockAware-Demo-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
dotnet run --no-build --project .\StockAware
```

Keep that same environment variable in the terminal when restarting. In the console:

1. `/stock 047` — expose the initial 12 milk cartons and zero coffee bags.
2. `/demo` — run the four acts:
   - **Investigate:** inventory, supplier and decision specialists exchange handoffs.
   - **Critique:** the judge checks actual retrieved evidence and may request refinement.
   - **Propose:** the copilot streams a follow-up and persists a feasible proposal to move **12 milk cartons from 208 to 047**. It does not apply it.
   - **Authorize:** the application stops short of approval and shows the next controls.
3. `/actions` — copy the exact pending proposal ID. `/stock 047` should still show **12**.
4. `/approve ID` — review the log and type exactly `APPROVE ID`.
5. `/stock 047` and `/stock 208` — show **24** and **36** respectively. Total milk is conserved; no physical truck was dispatched.
6. Ask: `What is the current status of that proposal? Recheck the action log.`
7. Ask: `My name is Morgan. Remember that I manage the weekend promotion.`
8. `/exit`, restart the same command, then ask: `What do you remember about my role?`

The one-shot equivalent is `dotnet run --no-build --project .\StockAware -- --demo`. It saves the report, conversation and proposal, then exits **without approving**. Start the interactive console against the same data directory to approve.

Investigations involve multiple paid model calls and can take several minutes. Rehearse with the venue network and pinned package versions. A complete first pass is valid: extra iterations are not manufactured for the presentation.

### Expected reasoning

- **047 milk:** the initial 12-carton opening gap can be covered from 208, while preserving 208's protected stock. Supplier milk is estimated four hours after opening.
- **112 milk:** 60 on hand minus 30 reserved minus 24 protected leaves **6**, not 36, transferable cartons.
- **047 coffee:** the old planning board is superseded by the latest Monday estimate. Neither donor has surplus coffee. Escalate the uncovered opening risk rather than inventing a transfer.
- **208 bananas:** 24 of the recorded 45 lb **may** be bruised. Usable stock remains unknown; do not write off inventory or assume the requested inbound window is confirmed.
- **SUP-482:** 2,400 messages = 2,216 applied + 176 permanent rejects + 8 retry-pending. The producer sent `"12 EA"` instead of integer `casePack`; 42 rejected SKUs overlap promotions. Store-level catalog publication is unknown. Correct and validate payloads, verify rejected IDs and idempotency, and obtain owner approval before bounded replay.

### Controls

| Command | Result |
| --- | --- |
| `/demo` | Full guided workflow, critique, persistent conversation and proposal. |
| `/investigate` | Specialist workflow and bounded critique only; no proposal. |
| `/stock STORE` | Current local inventory for 047, 112 or 208. |
| `/actions` | Pending and applied proposals with revisions and timestamps. |
| `/approve ID` | Human confirmation; deterministic revalidation and local stock update. |
| `/new` | New conversation; inventory and audit are preserved. |
| `/research PUBLIC TASK` | Isolated harness; explicit `PUBLIC` confirmation on every research turn. |
| `/help`, `/exit` | Controls; save and exit. |
| Other text | Streamed multi-turn conversation, with tools and skills available. |

Approval is idempotent for an already applied ID. A proposal becomes stale after another transfer changes the inventory revision; ask for a fresh evaluation/proposal. Source minimums, reservations, holds, cumulative route capacity and confirmed arrival before opening are enforced by code, both at proposal and at approval.

## Public research harness

Optional, after the core demo:

```text
/research Find two recent public articles about grocery cold-chain resilience. Plan the task, track your steps, then search and cite source URLs and publication dates. Do not use local store data.
```

Type `PUBLIC` to continue. If the harness asks to move from planning to execution, use another `/research ...` turn to approve that **research plan**. This is unrelated to `/approve` for inventory.

The research harness has a separate in-process session and **no retail MCP tools or retail conversation history**. Do not paste internal business data into a research request. Web search has additional charges and external processing; Bing grounding is outside the usual compliance/geographic boundary and Microsoft Data Protection Addendum. See [Microsoft's search disclosures](https://learn.microsoft.com/azure/foundry/openai/how-to/web-search). Research history is not restored after restart.

## HTTP/A2A hosting

```powershell
dotnet run --no-build --project .\StockAware -- --serve
```

Use [StockAware.http](StockAware.http):

- `GET http://localhost:5080/health` checks that the host and MCP initialization succeeded, **not** model inference access.
- `GET /.well-known/agent-card.json` advertises the copilot.
- `POST /a2a/stockaware/message:send` supports tools, skills and multi-turn `contextId`.
- `POST /a2a/stockaware/message:stream` returns Server-Sent Events.
- `POST /a2a/weekend-readiness/message:send` runs the sequential specialist workflow.

The hosted workflow does **not** run the judge loop; use console `/investigate` or `/demo` to demonstrate critique. Requests are serialized for this single-user demo. A2A conversation lookup is in memory and is lost on host restart; the console's saved session is separate.

There is **no HTTP approval endpoint**. Stop the host and use the console, keeping the same data directory, to approve a hosted proposal. The loopback host and stdio tools are for trusted local presenters only: no authentication, tenant isolation or production authorization is provided. Do not expose them to the internet or attach the complete MCP server tool set to an autonomous agent.

## Files, reports and reset

The data directory contains plaintext synthetic data:

- `retail-state.json`: inventory revision, proposals and applied audit records; written atomically under a cross-process lock.
- `conversations`: full chat/tool history, console session envelope and lock file.
- `reports`: brief, reviewed status and actual evidence ledger from guided runs.

Use a new data directory to reset the entire scene without deleting history. `/new` resets only conversation context. Do not edit state during a demo or share a directory across untrusted users. Malformed state fails explicitly rather than silently reseeding. The file store is intentionally not a production database or semantic/vector memory implementation.

If the judge reaches its limit, the guided run saves an **unreviewed** report and stops before proposing anything. A passing judge verdict is critique, not proof of correctness or authorization. Auth, configuration and transport errors are surfaced; live mode never substitutes canned AI responses.

## Validation and code tour

```powershell
dotnet build .\StockAware.slnx
dotnet run --no-build --project .\StockAware -- --verify-tools
```

The deterministic verification launches the **real MCP server twice** with an isolated temporary store. It checks tool separation, invalid inputs, exact transfer feasibility, reservations, quality holds, late/unknown routes, proposal non-mutation/deduplication, approval, conservation, idempotency, stale proposals, cumulative route capacity, incident totals and persistence after restart. It requires no Azure because it tests tools, not AI behavior. It does not replace the live `/demo`, A2A and research checks.

| File | Open on stage to explain |
| --- | --- |
| [Program.cs](Program.cs) / [DemoSettings.cs](DemoSettings.cs) | Modes, Azure requirement, configuration and failures. |
| [RetailAgents.cs](RetailAgents.cs) | Agent personas, workflow, evidence-aware judge and separate harness. |
| [RetailMcpClient.cs](RetailMcpClient.cs) | Real stdio discovery, tool allowlist and captured evidence. |
| [DemoConsole.cs](DemoConsole.cs) | Streaming, durable session and explicit human confirmation. |
| [RetailHost.cs](RetailHost.cs) | ASP.NET Core A2A hosting and SSE routes. |
| [RetailStore.cs](../StockAwareMcp/RetailStore.cs) | Deterministic local-only mutation, version checks and atomic persistence. |
| [Supplier skill](../09-AgentSkills/skills/supplier-feed-triage/SKILL.md) | Progressive-disclosure investigation procedure and references. |
