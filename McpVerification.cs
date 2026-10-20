using System.Text.Json;

namespace StockAware;

internal static class McpVerification
{
    internal static async Task RunAsync(CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"stockaware-verification-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var project = DemoSettings.FindMcpProject();
            string proposalId;
            await using (var mcp = await RetailMcpClient.ConnectAsync(project, directory, cancellationToken))
            {
                Assert(!mcp.ReadTools.Any(t => t.Name.Contains("approve", StringComparison.OrdinalIgnoreCase)
                    || t.Name.Contains("propose", StringComparison.OrdinalIgnoreCase)), "Read agents cannot mutate state.");
                Assert(mcp.ReadTools.Count == 9 && mcp.ProposalTool.Name == "propose_transfer", "Nine read tools and one separately granted proposal capability.");
                var before = await Call(mcp, "get_stock_report", new() { ["storeId"] = "047" });
                Assert(Milk(before) == 12, "Initial 047 milk balance.");
                await ExpectError(mcp, "get_stock_report", new() { ["storeId"] = "999" });
                await ExpectError(mcp, "evaluate_transfer", Transfer("208", "047", "10482", 0));
                await ExpectError(mcp, "evaluate_transfer", Transfer("047", "047", "10482", 1));
                await ExpectError(mcp, "evaluate_transfer", Transfer("208", "047", "unknown", 1));
                await ExpectBlocked(mcp, Transfer("112", "047", "10482", 12), "Only 6 cartons");
                await ExpectBlocked(mcp, Transfer("112", "208", "33041", 10), "Physical-count/quality hold", "after opening");
                await ExpectBlocked(mcp, Transfer("047", "112", "33041", 1), "No confirmed route; ETA unknown.");
                await ExpectError(mcp, "propose_transfer", Transfer("112", "047", "10482", 12));
                var arguments = Transfer("208", "047", "10482", 12);
                var check = await Call(mcp, "evaluate_transfer", arguments);
                Assert(check.GetProperty("feasible").GetBoolean(), "Exact 12-carton option is feasible.");
                Assert(check.GetProperty("availableAfterProtection").GetInt32() == 24
                    && check.GetProperty("blockers").GetArrayLength() == 0, "Feasible option preserves 208's 24-carton protected minimum.");
                var proposal = await Call(mcp, "propose_transfer", arguments);
                proposalId = proposal.GetProperty("id").GetString()!;
                var duplicate = await Call(mcp, "propose_transfer", arguments);
                Assert(duplicate.GetProperty("id").GetString() == proposalId, "Duplicate pending proposal is idempotent.");
                var unchanged = await Call(mcp, "get_stock_report", new() { ["storeId"] = "047" });
                Assert(Milk(unchanged) == 12, "Proposal does not change inventory.");
                var stale = await Call(mcp, "propose_transfer", Transfer("112", "047", "10482", 6));
                await Call(mcp, "approve_transfer", new() { ["proposalId"] = proposalId });
                var after = await Call(mcp, "get_stock_report", new() { ["storeId"] = "047" });
                Assert(Milk(after) == 24, "Manager approval changes destination to 24 cartons.");
                Assert(after.GetProperty("revision").GetInt32() == 1, "Approval advances inventory revision exactly once.");
                var source = await Call(mcp, "get_stock_report", new() { ["storeId"] = "208" });
                Assert(Milk(source) == 36, "Source decremented; total inventory conserved.");
                await Call(mcp, "approve_transfer", new() { ["proposalId"] = proposalId });
                var repeated = await Call(mcp, "get_stock_report", new() { ["storeId"] = "047" });
                Assert(Milk(repeated) == 24 && repeated.GetProperty("revision").GetInt32() == 1, "Repeated approval is idempotent.");
                await ExpectError(mcp, "approve_transfer", new() { ["proposalId"] = stale.GetProperty("id").GetString() });
                await ExpectBlocked(mcp, Transfer("208", "047", "10482", 9), "Only 8 units remain on this route.");
                var outcomes = await Call(mcp, "get_batch_outcomes", new() { ["incidentId"] = "SUP-482" });
                var payload = outcomes.GetProperty("Data");
                Assert(payload.GetProperty("Applied").GetInt32() + payload.GetProperty("PermanentlyRejected").GetInt32()
                    + payload.GetProperty("RetryPending").GetInt32() == payload.GetProperty("TotalMessages").GetInt32(), "Incident totals reconcile.");
            }
            await using (var restarted = await RetailMcpClient.ConnectAsync(project, directory, cancellationToken))
            {
                var persisted = await Call(restarted, "get_stock_report", new() { ["storeId"] = "047" });
                Assert(Milk(persisted) == 24, "Balances persist across real MCP server restart.");
                var log = await Call(restarted, "get_action_log", []);
                Assert(log.GetProperty("proposals").EnumerateArray().Any(p => p.GetProperty("id").GetString() == proposalId
                    && p.GetProperty("status").GetString() == "Applied"), "Applied audit record persists.");
            }
            Console.WriteLine("\nPASS | All real-MCP integration checks passed. No Azure inference was performed by this test.");
        }
        finally
        {
            foreach (var name in new[] { "retail-state.json", "retail-state.json.tmp", "retail-state.lock" })
            {
                var path = Path.Combine(directory, name);
                if (File.Exists(path)) File.Delete(path);
            }
            Directory.Delete(directory);
        }

        async Task<JsonElement> Call(RetailMcpClient mcp, string name, Dictionary<string, object?> arguments)
            => (await mcp.CallAsync(name, arguments, cancellationToken)).GetProperty("Data");

        async Task ExpectBlocked(RetailMcpClient mcp, Dictionary<string, object?> arguments, params string[] reasons)
        {
            var check = await Call(mcp, "evaluate_transfer", arguments);
            var blockers = check.GetProperty("blockers").EnumerateArray().Select(b => b.GetString()!).ToArray();
            Assert(!check.GetProperty("feasible").GetBoolean()
                && reasons.All(reason => blockers.Any(b => b.Contains(reason, StringComparison.Ordinal))),
                $"Blocks for the correct reasons: {string.Join("; ", reasons)}");
        }

        async Task ExpectError(RetailMcpClient mcp, string name, Dictionary<string, object?> arguments)
        {
            try { await mcp.CallAsync(name, arguments, cancellationToken); }
            catch (InvalidOperationException) { Console.WriteLine($"[PASS] {name} rejects invalid/stale input."); return; }
            throw new InvalidOperationException($"FAIL: {name} should have failed.");
        }
    }

    private static Dictionary<string, object?> Transfer(string source, string destination, string sku, int quantity)
        => new() { ["sourceStoreId"] = source, ["destinationStoreId"] = destination, ["sku"] = sku, ["quantity"] = quantity };

    private static int Milk(JsonElement report)
        => report.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("sku").GetString() == "10482").GetProperty("onHand").GetInt32();

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + message);
        Console.WriteLine("[PASS] " + message);
    }
}
