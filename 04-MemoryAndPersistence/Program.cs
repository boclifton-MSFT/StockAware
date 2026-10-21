#region SETUP
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using _04_MemoryAndPersistence;

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

var historyDirectory = Path.Combine(AppContext.BaseDirectory, "memory");
var sessionPath = Path.Combine(historyDirectory, "session.json");

AIAgent agent = new AIProjectClient(new Uri(endpoint), new AzureCliCredential())
    .GetProjectOpenAIClient()
    .GetChatClient(deploymentName)
    .AsIChatClient()
    .AsAIAgent(new ChatClientAgentOptions
    {
        Name = "MemoryAgent",
        ChatOptions = new() { Instructions = "You are a friendly assistant. Keep your answers brief." },
        ChatHistoryProvider = new CustomChatHistoryProvider(historyDirectory)
    });

AgentSession session;
string[] prompts;
if (File.Exists(sessionPath))
{
    using var document = JsonDocument.Parse(await File.ReadAllTextAsync(sessionPath));
    session = await agent.DeserializeSessionAsync(document.RootElement);
    Console.WriteLine($"Resumed session from {sessionPath}");
    prompts = ["What is my name?"];
}
else
{
    session = await agent.CreateSessionAsync();
    prompts = ["Hello! What's the square root of 9?", "My name is Alice", "What is my name?"];
}

foreach (var prompt in prompts)
{
    Console.WriteLine($"User: {prompt}");
    Console.WriteLine(await agent.RunAsync(prompt, session));

    var serializedSession = await agent.SerializeSessionAsync(session);
    await File.WriteAllTextAsync(sessionPath + ".tmp", serializedSession.GetRawText());
    File.Move(sessionPath + ".tmp", sessionPath, overwrite: true);
}

Console.WriteLine($"Session saved to {sessionPath}. Run again to recall Alice after a process restart.");
