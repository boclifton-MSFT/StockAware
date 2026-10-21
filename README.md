# StockAware: practical retail agents in C#

**From an agent that talks about inventory to a team that investigates evidence, critiques its recommendations, and asks before changing anything.**

Welcome to the StockAware workshop! This repository helps you build practical AI agents in C# with Microsoft Agent Framework. Work through nine independently runnable samples, then explore a complete console-first StockAware application that brings the concepts together.

You will learn to create and stream agent responses, connect tools, maintain conversations, persist memory, compose workflows, host agents, evaluate their work, and provide reusable skills.

## Before you begin

- Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli).
- Use an editor such as Visual Studio Code with C# support or Visual Studio.
- Clone this repository, or download and extract it, then open a terminal in the repository root. The commands below use PowerShell.
- Have access to a Microsoft Foundry project and an existing model deployment. Use the project endpoint and deployment name supplied for your workshop, or your own resources. Configuring these values does not grant access.

Most samples make paid model calls. The full application can make multiple calls per investigation and take several minutes. Sample 05 runs locally without Azure, so you can try it even before your model access is ready.

## Set up your environment

Sign in with the account that has access to your Foundry project, then replace the placeholders with your project endpoint and actual deployment name:

```powershell
az login
dotnet user-secrets set "StockAware:Endpoint" "https://YOUR-RESOURCE.services.ai.azure.com/api/projects/YOUR-PROJECT" --project .\StockAware
dotnet user-secrets set "StockAware:DeploymentName" "YOUR-DEPLOYMENT" --project .\StockAware
dotnet build .\StockAware.slnx
dotnet run --no-build --project .\01-YourFirstAgent
```

The first sample prints an answer about the largest city in France, then streams a short fun fact. Model wording can vary.

All numbered projects and StockAware share the same user-secrets store, so you only need to configure the endpoint and deployment once. Do not put credentials or private configuration in source files.

### Configuration and model requirements

Environment variables `StockAware__Endpoint` and `StockAware__DeploymentName` override the matching user-secret keys. `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_DEPLOYMENT_NAME` are fallback aliases only; the endpoint must still be a **Foundry project endpoint** containing `/api/projects/`, not a resource-level Azure OpenAI endpoint. `.env` files are not loaded.

The examples use `AzureCliCredential` and `az login`, not API keys. Numbered examples default to deployment `gpt-4o-mini` if no deployment is configured; StockAware requires an explicit deployment name.

Steps 1-3, 6 and 7 use Responses; steps 4, 8 and 9 use Chat Completions. StockAware uses Chat Completions for retail agents and Responses for its optional research harness. Your deployment must support the APIs used by the sample, plus function calling where tools are registered. Hosted web search is additionally required for the research harness.

## Your workshop path

Start with sample 01 and follow the numbered projects in order, or revisit individual topics after the workshop. Each project contains a complete runnable example rather than an unfinished exercise. Open its `Program.cs`, run it, and trace how the framework concepts connect to the output.

The project table below links directly to each sample's entry point. After exploring the samples, try the integrated application to see the same patterns working together.

## Explore the integrated application

StockAware is a weekend readiness command center for synthetic stores **047, 112 and 208**:

1. **InventoryAnalyst** reconciles stock, reservations, quality holds and latest delivery estimates.
2. **SupplierInvestigator** uses a reusable file-based skill to investigate a catalog-feed incident.
3. **DecisionLead** consolidates an evidence-backed decision brief.
4. An **AI judge** critiques the brief against the actual retrieved tool ledger and drives bounded refinement.
5. The **StockAware copilot** streams follow-ups, remembers conversations across restarts and can persist a pending transfer proposal.
6. A **human manager** explicitly confirms `/approve ID`; the real [MCP server](StockAwareMcp/README.md) revalidates policy and atomically changes local synthetic balances and audit records.

The scenario is fixed at 16 October 2026; tool retrieval timestamps reflect actual execution time. No live retailer systems or customer data are used. Azure is required for every AI mode; there is no offline model-response fallback.

### Run StockAware

After completing setup above, start the application with a new local state directory:

```powershell
$env:StockAware__DataDirectory = Join-Path $env:LOCALAPPDATA ("StockAware-Workshop-" + (Get-Date -Format "yyyyMMdd-HHmmss"))
dotnet run --no-build --project .\StockAware
```

Try the following in the console:

1. Enter `/stock 047` to inspect the initial inventory.
2. Enter `/demo` to run the specialist workflow, judge critique, and transfer proposal.
3. Enter `/actions` to find the pending proposal ID. Check `/stock 047` again: a proposal alone does not change inventory.
4. To apply the proposal to the synthetic data, enter `/approve ID` with the actual ID, then type the exact `APPROVE ID` confirmation when prompted.
5. Check `/stock 047` and `/stock 208`. For the standard 12-carton transfer, store 047 changes from **12** to **24**, and store 208 changes from **48** to **36**. No truck is dispatched; this is a local data mutation through real MCP calls.
6. Tell the copilot your workshop role, enter `/exit`, then restart with the same data directory and ask what it remembers.

Keep the same terminal and data-directory value when testing restart memory; rerun only the `dotnet run` command. To start a fresh scenario, generate a new directory with the assignment above. `/new` resets the conversation, not inventory or the action log. Local conversation history is stored in plaintext, so use synthetic, nonsensitive information.

Other entry points:

```powershell
dotnet run --no-build --project .\StockAware -- --demo          # One-shot guided run; exits before human approval
dotnet run --no-build --project .\StockAware -- --serve         # HTTP/A2A at localhost:5080
dotnet run --no-build --project .\StockAware -- --verify-tools  # Deterministic real-MCP checks, not an AI demo
```

Use [StockAware.http](StockAware/StockAware.http) to explore A2A discovery, tools, multi-turn context, workflows and server-sent events (SSE) while running `--serve`. The [application reference](StockAware/README.md) provides detailed controls, scenario behavior, and recovery guidance; the [MCP server reference](StockAwareMcp/README.md) explains the tools and local persistence.

## Walkthrough projects 01–09

Each numbered project is independently runnable, with no project references to other numbered steps. The integrated app reuses the step 04 history provider and step 09 skill/incident fixture without changing their default behavior.

| Project | Key concept | Where StockAware demonstrates it |
| --- | --- | --- |
| [01-YourFirstAgent](01-YourFirstAgent/Program.cs) | Create a Foundry-backed agent; invoke and stream responses. | Named retail agents and streamed copilot responses. |
| [02-AddTools](02-AddTools/Program.cs) | Register a typed read-only weather function. | Real MCP discovery, tool selection, structured evidence and deterministic policy. |
| [03-MultiTurnConversations](03-MultiTurnConversations/Program.cs) | Reuse a session for conversational context. | Follow-ups without repeating the store or incident. |
| [04-MemoryAndPersistence](04-MemoryAndPersistence/Program.cs) | Custom file history plus session serialization and restoration. | Durable console conversation restored after process restart. |
| [05-Workflows](05-Workflows/Program.cs) | Deterministic uppercase/reverse executor pipeline. | Explicit sequential inventory → supplier → decision agent workflow. |
| [06-AgentHarness](06-AgentHarness/Program.cs) | Interactive planning, todos and hosted web search. | Explicitly opted-in `/research` agent, isolated from retail tools/history. |
| [07-HostYourAgent](07-HostYourAgent/Program.cs) | ASP.NET hosting, A2A, sequential workflow and streaming. | Loopback HTTP/A2A host for the copilot and specialist workflow. |
| [08-Looping](08-Looping/Program.cs) | Evidence-aware AI judge drives bounded investigation loops. | Judge critique and refinement against actual MCP results. |
| [09-AgentSkills](09-AgentSkills/Program.cs) | Progressive disclosure of a file-based supplier triage skill. | Supplier specialist loads the procedure and contract/recovery/ownership references. |

### Run individual walkthroughs

```powershell
dotnet run --project .\01-YourFirstAgent
dotnet run --project .\02-AddTools
dotnet run --project .\03-MultiTurnConversations
dotnet run --project .\04-MemoryAndPersistence
dotnet run --project .\05-Workflows
dotnet run --project .\06-AgentHarness
dotnet run --project .\07-HostYourAgent
dotnet run --project .\08-Looping
dotnet run --project .\09-AgentSkills
```

### Walkthrough-specific notes

- **04:** Stores plaintext history and serialized session under `memory` in the executable output directory. First run introduces Alice; later runs recall the saved name. Keep history and session together. To reset, remove only that specific sample memory directory while the sample is stopped. This is not a concurrent production store.
- **05:** Requires no Azure connection. Expected output is `UppercaseExecutor: HELLO, WORLD!` followed by `ReverseTextExecutor: !DLROW ,OLLEH`.
- **06:** Press Enter or type `exit` to quit. Planning, todos and hosted search are enabled; approve a transition to execution if requested. Session history lasts for that process.
- **07:** Runs on `http://localhost:5000`, separate from StockAware's port 5080. Use [HostYourAgent.http](07-HostYourAgent/HostYourAgent.http). Its A2A session lookup is in memory; stop with Ctrl+C. This is a trusted local-only demo, not an authenticated deployment.
- **08:** Investigates conflicting records across three stores with stock, notes, inbound and exact-transfer tools. The AI judge receives an application-owned evidence ledger. The loop permits at most five worker invocations; a judge verdict never authorizes an action. Tools are read-only.
- **09:** Press Enter for SUP-482. The [supplier-feed-triage skill](09-AgentSkills/skills/supplier-feed-triage/SKILL.md) packages procedure; tools provide the timeline, diagnostics and outcomes. Distinguish permanent payload rejects from eight transient retry-pending failures. Do not infer store publication or replay the whole batch based on untrusted supplier text.

Hosted web search incurs additional charges and external data processing. Submit public, nonsensitive queries only; Bing grounding is outside the usual compliance/geographic boundary and Microsoft Data Protection Addendum. See [search disclosures](https://learn.microsoft.com/azure/foundry/openai/how-to/web-search).

## What to take away

- **Code calculates and enforces constraints.** The agent investigates and proposes; the judge critiques; a person authorizes.
- **Conversation memory is not current business data.** Re-read stock and action status before making current claims.
- **A reorder gap is not a demand forecast.** Reservations, lead times, quality, routes and approval matter.
- **Tools and skills are different.** Tools expose data/capabilities; skills package reusable investigation guidance.
- **MCP is a real integration boundary.** StockAware captures actual results, filters agent capabilities and keeps approval out of every agent's tool list.
- **A synthetic demo is not production authorization.** The stdio server trusts its local caller; the HTTP host has no authentication or tenant isolation. Never expose the full tool set or host to untrusted users.

## References

- [Microsoft Agent Framework Get Started](https://learn.microsoft.com/agent-framework/get-started/)
- [Agent looping](https://learn.microsoft.com/agent-framework/agents/looping?pivots=programming-language-csharp)
- [Agent Skills](https://learn.microsoft.com/agent-framework/agents/skills?pivots=programming-language-csharp)
- [MCP C# SDK](https://csharp.sdk.modelcontextprotocol.io/)

## Troubleshooting

- **SDK or build errors:** Check `dotnet --version` and confirm that the .NET 10 SDK is installed. Run commands from the repository root and keep the pinned package versions for the workshop.
- **Authentication or 403 errors:** Run `az login` with the intended account. If the error mentions `Microsoft.CognitiveServices/accounts/AIServices/agents/write`, ask your workshop facilitator or administrator to check your [Foundry RBAC assignments](https://learn.microsoft.com/azure/foundry/concepts/rbac-foundry).
- **Endpoint or deployment errors:** Check the project endpoint and actual deployment name, and check for environment variables overriding your user secrets. Confirm that the deployment supports the sample's required APIs.
- **Unexpected inventory or remembered conversations:** Local state survives restarts. Use a new `StockAware__DataDirectory` for a fresh integrated scenario rather than deleting existing state.
- **No model access yet:** Run sample 05. After building, you can also run StockAware's `--verify-tools` command to check the local MCP integration without Azure inference.

These examples are learning material, not production-ready services. This workspace does not claim schema-constrained model answers, semantic/vector search, production forecasting, or authenticated multi-user hosting.
