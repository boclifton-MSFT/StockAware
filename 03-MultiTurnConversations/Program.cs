#region SETUP
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;

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

AIAgent agent = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are a friendly assistant. Keep your answers brief.",
        name: "ConversationAgent");

AgentSession session = await agent.CreateSessionAsync();

Console.WriteLine(await agent.RunAsync("My name is Alice and I love hiking.", session));
Console.WriteLine(await agent.RunAsync("What do you remember about me?", session));
