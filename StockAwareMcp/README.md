# StockAware MCP: local retail simulator

A real MCP **stdio** server used by the integrated [StockAware demo](../StockAware/README.md). The client launches it as a child process, discovers its tools and invokes them over the MCP protocol. It does not expose HTTP endpoints. Logs go to stderr; stdout is reserved for protocol messages.

## Tools

| Tool | Behavior |
| --- | --- |
| `get_stock_report(storeId)` | Current local inventory, reservations, protected minimums, quality holds and revision for 047, 112 or 208. |
| `get_replenishment_needs()` | Recorded gaps across all stores; not demand forecasts or purchase quantities. |
| `get_operational_evidence()` | Timestamped manager, receiving and latest supplier-delivery evidence. |
| `get_store_policy(storeId)` | Transfer constraints and replenishment/substitution guidance. |
| `evaluate_transfer(sourceStoreId, destinationStoreId, sku, quantity)` | Check an exact option without changing state. |
| `propose_transfer(sourceStoreId, destinationStoreId, sku, quantity)` | Persist a feasible **pending proposal**, without changing inventory. |
| `get_action_log()` | Pending/applied proposals and audit timestamps. |
| `approve_transfer(proposalId)` | Apply a pending proposal to **local synthetic balances** after revalidation; manager control-plane only. |
| `get_incident(incidentId)` | SUP-482 timeline and change context. |
| `get_feed_diagnostics(incidentId)` | Permanent validation rejects, transient failures and untrusted supplier payload text. |
| `get_batch_outcomes(incidentId)` | Applied/rejected/retry counts and limited promotion exposure evidence. |

Store IDs `047`, `112`, `208` and labels such as `Store 047` are accepted. SKUs: `10482` milk, `44109` coffee, `33041` bananas. Unsupported stores/SKUs and nonpositive quantities return tool errors. The incident tools support only `SUP-482`; they expose **no replay or correction actions**.

The scene is fixed at 16 October 2026, 17:15 UTC, protecting opening on 17 October at 12:00 UTC. It is not live inventory.

## Approval and persistence

State lives in `retail-state.json` under `StockAware:DataDirectory`, defaulting to `%LOCALAPPDATA%\StockAware`. The main client passes its configured directory to the child process. Reads and mutations take a cross-process file lock; writes use atomic replacement. Lock contention and corrupt state surface as failures, not successful defaults.

Proposal and approval both check reservations, source protected minimum, source/destination quality holds, confirmed route, cumulative route capacity, and arrival before opening. Identical pending proposals at the same revision reuse their ID. Every applied transfer increments the revision; other pending proposals then become stale and must be recreated. Approving an already applied ID is idempotent.

The initial demo option moves 12 milk cartons from 208 to 047: a proposal leaves balances at **48/12**, while explicit approval changes them to **36/24**. This models an instantaneous local movement, not physical dispatch or delivery. Inventory and audit are persisted together.

**The stdio server trusts its local caller.** `approve_transfer` is a discoverable control-plane tool, not an authenticated production API. StockAware excludes it from **every agent's tool list** and only calls it after `/approve ID` plus typed confirmation. Read-only specialists also cannot create proposals. Do not expose the full server tool list to an AI client without implementing an equivalent approval boundary.

## Build and run

From the workspace root:

```powershell
dotnet build .\StockAware.slnx
dotnet run --no-build --project .\StockAware
```

StockAware builds and launches this server automatically. For a separate trusted MCP client:

```json
{
  "servers": {
    "StockAwareMcp": {
      "type": "stdio",
      "command": "dotnet",
      "args": ["run", "--no-build", "--project", "C:\\ABSOLUTE\\PATH\\StockAwareMcp\\StockAwareMcp.csproj"],
      "env": { "StockAware__DataDirectory": "C:\\ABSOLUTE\\PATH\\demo-state" }
    }
  }
}
```

Normal development builds are framework-dependent to support the main application's build dependency. The existing packaging settings retain self-contained, single-file **publishing**; publishing a packaged MCP server is not needed for this source-checkout demo.

Use a new `StockAware__DataDirectory` for a fresh rehearsal instead of deleting an existing store. All data is local and synthetic; no retailer system, purchasing API or supplier integration is contacted by these tools.

## Verification

```powershell
dotnet run --no-build --project .\StockAware -- --verify-tools
```

This launches real stdio MCP clients against an isolated temporary store and checks the policy and persistence boundaries, including a server restart. No Azure inference is used by this integration test. See the main [presenter runbook](../StockAware/README.md) for the live AI, approval and HTTP/A2A sequence.
