#region SETUP
using System.ComponentModel;
using A2A;
using A2A.AspNetCore;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OpenAI;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

var endpoint = builder.Configuration["StockAware:Endpoint"]
    ?? builder.Configuration["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException("Set StockAware:Endpoint in user secrets or StockAware__Endpoint in environment variables to your Microsoft Foundry project endpoint.");
var deploymentName = builder.Configuration["StockAware:DeploymentName"]
    ?? builder.Configuration["AZURE_OPENAI_DEPLOYMENT_NAME"]
    ?? "gpt-4o-mini";
#endregion

// The walkthrough uses the experimental Foundry Responses client for hosting.
#pragma warning disable OPENAI001
IChatClient chatClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetProjectResponsesClient()
    .AsIChatClient(deploymentName);
#pragma warning restore OPENAI001

builder.Services.AddKeyedSingleton<IChatClient>("chat-model", chatClient);

builder.AddAIAgent(
        "pirate",
        instructions: "You are a pirate. Speak like a pirate. Use the weather tool for weather questions.",
        description: "An agent that speaks like a pirate.",
        chatClientServiceKey: "chat-model")
    .WithAITool(AIFunctionFactory.Create(GetWeather))
    // This loopback-only, single-user demo has no authenticated isolation key.
    .WithInMemorySessionStore(withIsolation: false);

builder.AddAIAgent("agent-1", instructions: "You are agent 1! Draft a brief answer to the user's question.",
    chatClientServiceKey: "chat-model");
builder.AddAIAgent("agent-2", instructions: "You are agent 2! Review and improve the previous agent's answer.",
    chatClientServiceKey: "chat-model");

builder.AddWorkflow("my-workflow", (sp, key) =>
{
    var agent1 = sp.GetRequiredKeyedService<AIAgent>("agent-1");
    var agent2 = sp.GetRequiredKeyedService<AIAgent>("agent-2");
    return AgentWorkflowBuilder.BuildSequential(key, [agent1, agent2]);
}).AddAsAIAgent();

builder.AddA2AServer("pirate");
builder.AddA2AServer("my-workflow");

var app = builder.Build();
app.MapA2AHttpJson("pirate", "/a2a/pirate");
app.MapA2AHttpJson("my-workflow", "/a2a/my-workflow");
app.MapWellKnownAgentCard(new AgentCard
{
    Name = "Pirate",
    Description = "An agent that speaks like a pirate and can report illustrative weather.",
    Version = "1.0",
    DefaultInputModes = ["text"],
    DefaultOutputModes = ["text"],
    SupportedInterfaces =
    [
        new AgentInterface
        {
            Url = "http://localhost:5000/a2a/pirate",
            ProtocolBinding = ProtocolBindingNames.HttpJson,
            ProtocolVersion = "1.0"
        }
    ]
});

app.Run("http://localhost:5000");

[Description("Get the weather for a given location.")]
static string GetWeather([Description("The location to get the weather for.")] string location)
    => $"The weather in {location} is cloudy with a high of 15\u00B0C.";
