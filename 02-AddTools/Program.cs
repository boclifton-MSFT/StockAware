#region SETUP
using System.ComponentModel;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
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
        instructions: "You are a helpful assistant.",
        tools: [AIFunctionFactory.Create(GetWeather)]);

Console.WriteLine(await agent.RunAsync("What is the weather like in Amsterdam?"));

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15\u00B0C.";
