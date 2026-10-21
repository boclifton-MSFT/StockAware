#region SETUP
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var endpoint = config["StockAware:Endpoint"]
    ?? config["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("Set StockAware:Endpoint in user secrets or StockAware__Endpoint in environment variables to your Microsoft Foundry project endpoint.");
var deploymentName = config["StockAware:DeploymentName"]
    ?? config["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? "gpt-4o-mini";
#endregion

// The project Responses client is experimental in the pinned SDK version.
#pragma warning disable OPENAI001
using IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(deploymentName);
#pragma warning restore OPENAI001

AIAgent agent = chatClient.AsHarnessAgent(new HarnessAgentOptions
{
    ChatOptions = new()
    {
        Instructions = "Use web search for current information and cite your sources."
    }
});
AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine("Harness agent ready. Type 'exit' to quit.");
while (true)
{
    Console.Write("> ");
    string? input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    await foreach (var update in agent.RunStreamingAsync(input, session))
    {
        Console.Write(update);
    }

    Console.WriteLine();
}
