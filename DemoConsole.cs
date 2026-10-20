using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace StockAware;

internal static class DemoConsole
{
    internal static async Task RunAsync(DemoSettings settings, RetailMcpClient mcp, RetailAgents agents,
        bool guidedOnly, CancellationToken cancellationToken)
    {
        var memory = Path.Combine(settings.DataDirectory, "conversations");
        Directory.CreateDirectory(memory);
        using var lease = new FileStream(Path.Combine(memory, "console.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var sessionPath = Path.Combine(memory, "session.json");
        AgentSession session;
        if (File.Exists(sessionPath))
        {
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(sessionPath, cancellationToken));
            var root = document.RootElement;
            if (root.GetProperty("Endpoint").GetString() != settings.Endpoint.ToString()
                || root.GetProperty("Deployment").GetString() != settings.Deployment)
                throw new InvalidOperationException("Saved conversation belongs to a different endpoint/model. Use another StockAware:DataDirectory.");
            session = await agents.Copilot.DeserializeSessionAsync(root.GetProperty("Session"), cancellationToken: cancellationToken);
            Console.WriteLine("[Memory] Restored durable conversation across process restart.");
        }
        else session = await agents.Copilot.CreateSessionAsync(cancellationToken);

        async Task SaveAsync()
        {
            var state = await agents.Copilot.SerializeSessionAsync(session, cancellationToken: cancellationToken);
            await AtomicWriteAsync(sessionPath, JsonSerializer.Serialize(new
            {
                Endpoint = settings.Endpoint.ToString(), settings.Deployment, Session = state
            }), cancellationToken);
        }

        async Task SpeakAsync(string prompt)
        {
            Console.WriteLine($"\nYou: {prompt}\nStockAware:");
            await foreach (var update in agents.Copilot.RunStreamingAsync(prompt, session, cancellationToken: cancellationToken))
                PrintUpdate(update);
            Console.WriteLine();
            await SaveAsync();
        }

        async Task GuidedAsync()
        {
            var brief = await agents.InvestigateAsync(cancellationToken);
            Console.WriteLine($"\nFINAL BRIEF\n{brief}");
            var reportDirectory = Path.Combine(settings.DataDirectory, "reports");
            Directory.CreateDirectory(reportDirectory);
            var reportPath = Path.Combine(reportDirectory, $"weekend-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
            await AtomicWriteAsync(reportPath, JsonSerializer.Serialize(new
            {
                brief.Brief, brief.Reviewed, brief.Status,
                Evidence = JsonSerializer.Deserialize<JsonElement>(mcp.Evidence)
            }), cancellationToken);
            Console.WriteLine($"[Report] Saved decision and evidence: {reportPath}");
            if (!brief.Reviewed)
                throw new InvalidOperationException("The review limit was reached. The draft and evidence were saved, but the guided demo will not label it reviewed or proceed to a proposal. Rerun /investigate to address the critique.");
            await SpeakAsync("The specialist team produced this reviewed brief. Remember it as context, not as authorization:\n" + brief);
            Console.WriteLine("\nACT 3 | Streamed follow-up, durable memory and a REAL MCP proposal");
            await SpeakAsync("Read current inventory and evaluate transferring exactly 12 milk cartons (SKU 10482) from 208 to 047. If feasible, create a local transfer proposal for that exact option. Do not approve it. Give the proposal ID, before/after balances and remaining coffee/banana follow-ups.");
            Console.WriteLine("\nACT 4 | Human control: use /actions, then /approve PROPOSAL_ID, then ask for fresh stock.");
        }

        Console.WriteLine("""

            ==============================================================
            STOCKAWARE | Weekend readiness command center
            Synthetic stores 047 / 112 / 208 | Scenario: 16 October 2026
            Live Azure agents + real MCP tools + local-only inventory writes
            ==============================================================
            """);
        Console.WriteLine($"[Model] {settings.Deployment} | [State] {settings.DataDirectory}");
        Console.WriteLine("A judge checks evidence; a manager authorizes changes. Type /help for demo controls.");
        if (guidedOnly) { await GuidedAsync(); return; }

        AIAgent? harness = null;
        AgentSession? researchSession = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            Console.Write("\nStockAware> ");
            var input = await Console.In.ReadLineAsync(cancellationToken);
            if (input is null || input.Equals("/exit", StringComparison.OrdinalIgnoreCase)) break;
            if (string.IsNullOrWhiteSpace(input)) continue;
            if (input == "/help")
            {
                Console.WriteLine("""
                    /demo                 Guided specialists -> judge -> streamed proposal
                    /investigate          Specialists + bounded judge loop only
                    /stock STORE          Fresh local inventory (047, 112 or 208)
                    /actions              Pending/applied proposals with exact IDs
                    /approve ID           Manager confirms and applies ONE local transfer
                    /new                  Start a new conversation; preserve inventory/audit
                    /research PUBLIC TASK Separate planning/todo/web-search harness (explicit opt-in)
                    /exit                 Save and exit
                    Any other text        Stream a multi-turn retail conversation with tools/skills
                    Memory test: tell me your name; exit; restart; ask what I remember.
                    """);
            }
            else if (input == "/demo") await GuidedAsync();
            else if (input == "/investigate") Console.WriteLine(await agents.InvestigateAsync(cancellationToken));
            else if (input.StartsWith("/stock ", StringComparison.Ordinal))
            {
                try { Print(await mcp.CallAsync("get_stock_report", new Dictionary<string, object?> { ["storeId"] = input[7..].Trim() }, cancellationToken)); }
                catch (InvalidOperationException error) { Console.Error.WriteLine(error.Message); }
            }
            else if (input == "/actions") Print(await mcp.CallAsync("get_action_log", cancellationToken: cancellationToken));
            else if (input.StartsWith("/approve ", StringComparison.Ordinal))
            {
                var id = input[9..].Trim();
                Console.WriteLine("Review the exact pending proposal below. Approval simulates an instantaneous LOCAL stock movement; it does NOT dispatch a truck.");
                Print(await mcp.CallAsync("get_action_log", cancellationToken: cancellationToken));
                Console.Write($"Type APPROVE {id} to apply this local proposal: ");
                var confirmation = await Console.In.ReadLineAsync(cancellationToken);
                if (confirmation == $"APPROVE {id}")
                {
                    try { Print(await mcp.CallAsync("approve_transfer", new Dictionary<string, object?> { ["proposalId"] = id }, cancellationToken)); }
                    catch (InvalidOperationException error) { Console.Error.WriteLine($"[Manager] Approval refused: {error.Message}"); }
                }
                else Console.WriteLine("[Manager] Not approved; no stock changed.");
            }
            else if (input == "/new")
            {
                session = await agents.Copilot.CreateSessionAsync(cancellationToken);
                await SaveAsync();
                Console.WriteLine("[Memory] New conversation saved. Inventory and audit unchanged.");
            }
            else if (input.StartsWith("/research ", StringComparison.Ordinal))
            {
                Console.WriteLine("Hosted web search has additional charges and external data processing. Submit PUBLIC, nonsensitive queries only.");
                Console.Write("Type PUBLIC to start/continue the isolated research harness: ");
                if (await Console.In.ReadLineAsync(cancellationToken) != "PUBLIC") { Console.WriteLine("Research cancelled."); continue; }
                harness ??= agents.Harness();
                researchSession ??= await harness.CreateSessionAsync(cancellationToken);
                await foreach (var update in harness.RunStreamingAsync(input[10..], researchSession, cancellationToken: cancellationToken))
                    PrintUpdate(update);
                Console.WriteLine();
            }
            else if (input.StartsWith('/')) Console.Error.WriteLine("Unknown command. Type /help.");
            else await SpeakAsync(input);
        }
        await SaveAsync();
        Console.WriteLine("[Memory] Conversation saved. Run again to resume.");
    }

    private static void Print(JsonElement value) => Console.WriteLine(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));

    private static void PrintUpdate(AgentResponseUpdate update)
    {
        foreach (var content in update.Contents)
        {
            if (content is FunctionCallContent call)
                Console.WriteLine($"\n[Agent tool] {call.Name}");
            else if (content is WebSearchToolCallContent { Queries.Count: > 0 } search)
                Console.WriteLine($"\n[Hosted web search] {JsonSerializer.Serialize(search.Queries)}");
            else if (content is TextContent text)
                Console.Write(text.Text);

            foreach (var citation in content.Annotations?.OfType<CitationAnnotation>() ?? [])
                Console.WriteLine($"\n[Source] {citation.Title}: {citation.Url}");
        }
    }

    private static async Task AtomicWriteAsync(string path, string content, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(path + ".tmp", content, cancellationToken);
        File.Move(path + ".tmp", path, overwrite: true);
    }
}
