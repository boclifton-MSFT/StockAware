using Azure;
using Microsoft.Extensions.Configuration;
using StockAware;

if (args.Contains("--help", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine("""
        StockAware | Azure-backed retail multi-agent demonstration
        dotnet run --project .\StockAware                 Interactive command center
        dotnet run --project .\StockAware -- --demo       Guided investigation, review and proposal
        dotnet run --project .\StockAware -- --serve      Local HTTP/A2A host on localhost:5080
        dotnet run --project .\StockAware -- --verify-tools  MCP integration checks (not an AI demo)
        """);
    return 0;
}

using var shutdown = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
try
{
    if (args.Contains("--verify-tools", StringComparer.OrdinalIgnoreCase))
    {
        await McpVerification.RunAsync(shutdown.Token);
        return 0;
    }
    if (args.Any(a => a is not ("--demo" or "--serve")) || args.Length > 1)
        throw new ArgumentException("Choose no option, --demo, --serve, --verify-tools or --help.");
    var config = new ConfigurationBuilder().AddUserSecrets<Program>(optional: true).AddEnvironmentVariables().Build();
    var settings = DemoSettings.Load(config);
    Directory.CreateDirectory(settings.DataDirectory);
    await RetailAgents.WarmUpAsync(shutdown.Token);
    await using var mcp = await RetailMcpClient.ConnectAsync(settings.McpProject, settings.DataDirectory, shutdown.Token);
    using var agents = new RetailAgents(settings, mcp);
    if (args.Contains("--serve", StringComparer.OrdinalIgnoreCase))
        await RetailHost.RunAsync(agents, shutdown.Token);
    else
        await DemoConsole.RunAsync(settings, mcp, agents, args.Contains("--demo"), shutdown.Token);
    return 0;
}
catch (OperationCanceledException) when (shutdown.IsCancellationRequested)
{
    Console.Error.WriteLine("\nStockAware cancelled. No unapproved proposal was applied.");
    return 130;
}
catch (Exception error) when (error is RequestFailedException or HttpRequestException or IOException
    or InvalidOperationException or ArgumentException or System.Text.Json.JsonException
    or System.ClientModel.ClientResultException or AggregateException
    or Azure.Identity.AuthenticationFailedException)
{
    var detail = error is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.First().Message : error.Message;
    Console.Error.WriteLine($"\nStockAware failed: {detail}");
    Console.Error.WriteLine("Live AI requires the configured Foundry deployment and az login. MCP requires a successful build. No offline AI fallback is used.");
    return 1;
}
