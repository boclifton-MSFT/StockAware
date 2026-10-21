using System.Text.Json;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace StockAware;

internal sealed class RetailMcpClient : IAsyncDisposable
{
    private readonly McpClient _client;
    private readonly Dictionary<string, McpClientTool> _tools;
    private readonly List<JsonElement> _evidence = [];
    private readonly Lock _gate = new();

    private RetailMcpClient(McpClient client, IList<McpClientTool> tools)
    {
        _client = client;
        _tools = tools.ToDictionary(t => Canonical(t.Name));
        ReadTools = tools.Where(t => Canonical(t.Name) is "getstockreport" or "getreplenishmentneeds"
            or "getoperationalevidence" or "getstorepolicy" or "evaluatetransfer" or "getactionlog"
            or "getincident" or "getfeeddiagnostics" or "getbatchoutcomes")
            .Select(t => (AITool)new EvidenceFunction(this, t)).ToArray();
        ProposalTool = new EvidenceFunction(this, _tools["proposetransfer"]);
    }

    internal IReadOnlyList<AITool> ReadTools { get; }
    internal AITool ProposalTool { get; }
    internal string Evidence
    {
        get { lock (_gate) return JsonSerializer.Serialize(_evidence); }
    }

    internal static async Task<RetailMcpClient> ConnectAsync(string project, string dataDirectory, CancellationToken cancellationToken)
    {
        var transport = new StdioClientTransport(new()
        {
            Name = "StockAware local retail simulator",
            Command = "dotnet",
            Arguments = ["run", "--no-build", "--no-launch-profile", "--project", project, "--configuration",
#if DEBUG
                "Debug"
#else
                "Release"
#endif
            ],
            EnvironmentVariables = new Dictionary<string, string?> { ["StockAware__DataDirectory"] = dataDirectory }
        });
        var client = await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
        try
        {
            var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            var result = new RetailMcpClient(client, tools);
            Console.WriteLine($"[MCP] Connected: {tools.Count} real stdio tools. Local data: {dataDirectory}");
            return result;
        }
        catch
        {
            await client.DisposeAsync();
            throw;
        }
    }

    internal async Task<JsonElement> CallAsync(string name, IReadOnlyDictionary<string, object?>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        var tool = _tools[Canonical(name)];
        Console.WriteLine($"\n[MCP ->] {tool.Name} {JsonSerializer.Serialize(arguments)}");
        var response = await _client.CallToolAsync(tool.Name, arguments, cancellationToken: cancellationToken);
        var text = string.Join("\n", response.Content.OfType<TextContentBlock>().Select(t => t.Text));
        if (response.IsError == true)
            throw new InvalidOperationException($"MCP tool '{tool.Name}' failed: {text}");
        var data = response.StructuredContent is { } structured
            ? JsonSerializer.SerializeToElement(structured)
            : JsonSerializer.Deserialize<JsonElement>(text);
        JsonElement entry;
        lock (_gate)
        {
            entry = JsonSerializer.SerializeToElement(new
            {
                EvidenceId = $"MCP-{_evidence.Count + 1:000}", Tool = tool.Name,
                Arguments = arguments, RetrievedAtUtc = DateTimeOffset.UtcNow, Data = data
            });
            _evidence.Add(entry);
        }
        Console.WriteLine($"[MCP <-] {entry.GetProperty("EvidenceId").GetString()}");
        return entry;
    }

    internal void ClearEvidence() { lock (_gate) _evidence.Clear(); }
    public ValueTask DisposeAsync() => _client.DisposeAsync();
    private static string Canonical(string name) => name.Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

    private sealed class EvidenceFunction(RetailMcpClient owner, McpClientTool tool) : AIFunction
    {
        public override string Name => tool.Name;
        public override string Description => tool.Description;
        public override JsonElement JsonSchema => tool.JsonSchema;
        protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
            => await owner.CallAsync(tool.Name, arguments, cancellationToken);
    }
}
