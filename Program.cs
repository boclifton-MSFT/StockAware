#pragma warning disable MAAI001
using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.VectorData;
using StockAware;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var projectEndpoint = config["StockAware:Endpoint"]
    ?? config["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("Set StockAware:Endpoint or AZURE_OPENAI_ENDPOINT in user secrets/environment variables before starting StockAware.");

// GPT 5 for this demo
var deploymentName = config["StockAware:DeploymentName"]
    ?? config["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("Set StockAware:DeploymentName or AZURE_OPENAI_DEPLOYMENT_NAME in user secrets/environment variables before starting StockAware.");

VectorStore vectorStore = new InMemoryVectorStore();

ChatClientAgentOptions options = new()
{
    ChatOptions = new()
    {
        ModelId = deploymentName,
        Instructions = "You are a grocery operations copilot. Focus on inventory health, replenishment, substitutions, and safe retail actions."
    },
    Name = "StockAware",
    ChatHistoryProvider = new VectorChatHistoryProvider(vectorStore)
};

AIAgent agent = new AIProjectClient(new Uri(projectEndpoint), new AzureCliCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are a grocery operations copilot. Focus on inventory health, replenishment, substitutions, and safe retail actions.",
        name: "StockAware",
        tools: [
            AIFunctionFactory.Create(GetWeather)
        ]);

AgentSession session = await agent.CreateSessionAsync();

await foreach (var update in agent.RunStreamingAsync("What's the weather in Detroit?", session))
{
    Console.Write(update);
}

Console.WriteLine();

await foreach (var update in agent.RunStreamingAsync("How might that affect our ice cream deliveries to stores?", session))
{
    Console.Write(update);
}

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15°C.";