using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Server;

/// <summary>Retail evidence and local-only, approval-gated inventory operations.</summary>
internal sealed class StockAwareTools(RetailStore store)
{
    [McpServerTool, Description("Read synthetic inventory for store 047, 112 or 208, including reservations, protected minimums, quality holds and current local revision.")]
    public object GetStockReport(string storeId = "047") => Run(() => store.Report(storeId));

    [McpServerTool, Description("Read all recorded shortages. Gaps to reorder points are not demand forecasts or purchase quantities.")]
    public object GetReplenishmentNeeds() => store.Shortages();

    [McpServerTool, Description("Read timestamped manager, receiving and delivery evidence for all stores. Distinguish suspect bananas from confirmed losses and old schedules from latest estimates.")]
    public object GetOperationalEvidence() => RetailStore.OperationalEvidence;

    [McpServerTool, Description("Read enforced local transfer policy and illustrative replenishment/substitution guidance.")]
    public object GetStorePolicy(string storeId = "047") => Run(() => store.Policy(storeId));

    [McpServerTool, Description("Check an exact proposed transfer against current stock, reservations, protected minimum, quality holds, route capacity and opening deadline. Does not change stock or grant approval.")]
    public object EvaluateTransfer(string sourceStoreId, string destinationStoreId, string sku, int quantity)
        => Run(() => store.Evaluate(sourceStoreId, destinationStoreId, sku, quantity));

    [McpServerTool, Description("Persist a feasible local-only transfer PROPOSAL for manager review. Does not change inventory. Repeated identical pending requests at the same revision return the existing proposal.")]
    public object ProposeTransfer(string sourceStoreId, string destinationStoreId, string sku, int quantity)
        => Run(() => store.Propose(sourceStoreId, destinationStoreId, sku, quantity));

    [McpServerTool, Description("Read pending and completed local transfer proposals and audit records; never approves or changes inventory.")]
    public object GetActionLog() => store.ActionLog();

    [McpServerTool, Description("Manager control-plane operation. Apply a specific pending proposal to LOCAL SYNTHETIC inventory only. Must not be exposed to an AI agent. Rejects stale proposals and rechecks policy. Already applied IDs are idempotent.")]
    public object ApproveTransfer(string proposalId) => Run(() => store.Approve(proposalId));

    // McpException messages reach the caller; other exception text is hidden by the SDK.
    private static object Run(Func<object> action)
    {
        try { return action(); }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException)
        {
            throw new McpException(error.Message);
        }
    }
}
