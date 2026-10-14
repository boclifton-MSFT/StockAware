#pragma warning disable MAAI001
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

var projectEndpoint = config["StockAware:Endpoint"]
    ?? config["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("Set StockAware:Endpoint or AZURE_OPENAI_ENDPOINT in user secrets/environment variables before starting StockAware.");
var deploymentName = config["StockAware:DeploymentName"]
    ?? config["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? throw new InvalidOperationException("Set StockAware:DeploymentName or AZURE_OPENAI_DEPLOYMENT_NAME in user secrets/environment variables before starting StockAware.");

AIAgent agent = new AIProjectClient(new Uri(projectEndpoint), new AzureCliCredential())
    .AsAIAgent(
        model: deploymentName,
        instructions: "You are a helpful agent.",
        name: "StockAware");

IChatClient chatClient = agent.AsIChatClient();

var response = await chatClient.GetResponseAsync("Hello, how can you assist me today?");

Console.WriteLine(response);
