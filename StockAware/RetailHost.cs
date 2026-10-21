using A2A;
using A2A.AspNetCore;
using Microsoft.Agents.AI.Hosting;

namespace StockAware;

internal static class RetailHost
{
    internal static async Task RunAsync(RetailAgents agents, CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddProblemDetails();
        builder.AddAIAgent("StockAware", (_, _) => agents.Copilot)
            .WithInMemorySessionStore(withIsolation: false);
        builder.AddAIAgent("weekend-readiness", (_, _) => agents.Workflow())
            .WithInMemorySessionStore(withIsolation: false);
        builder.AddA2AServer("StockAware");
        builder.AddA2AServer("weekend-readiness");
        await using var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        using var gate = new SemaphoreSlim(1, 1);
        app.Use(async (context, next) =>
        {
            await gate.WaitAsync(context.RequestAborted);
            try { await next(context); }
            finally { gate.Release(); }
        });
        app.MapGet("/health", () => TypedResults.Ok(new HostHealth("ready", true, "No HTTP approval endpoint; manager approval is console-only.")))
            .WithName("StockAwareHealth")
            .WithSummary("Local host readiness (does not probe model access).")
            .WithDescription("MCP has connected; successful AI access is verified only by an agent request.");
        app.MapA2AHttpJson("StockAware", "/a2a/stockaware");
        app.MapA2AHttpJson("weekend-readiness", "/a2a/weekend-readiness");
        app.MapWellKnownAgentCard(new AgentCard
        {
            Name = "StockAware",
            Description = "Synthetic retail copilot: inventory tools, supplier skills, follow-ups and local transfer proposals. No AI approval.",
            Version = "1.0",
            DefaultInputModes = ["text"], DefaultOutputModes = ["text"],
            SupportedInterfaces =
            [
                new AgentInterface
                {
                    Url = "http://localhost:5080/a2a/stockaware",
                    ProtocolBinding = ProtocolBindingNames.HttpJson, ProtocolVersion = "1.0"
                }
            ]
        });
        Console.WriteLine("StockAware HTTP/A2A | localhost:5080 | trusted single-user demonstration only.");
        Console.WriteLine("Hosted workflow demonstrates sequential handoffs; /investigate in the console adds the evidence-aware judge.");
        app.Urls.Clear();
        app.Urls.Add("http://localhost:5080");
        await app.RunAsync(cancellationToken);
    }

    private sealed record HostHealth(string Status, bool SyntheticData, string ApprovalBoundary);
}
