# StockAware

Barebones Microsoft Agent Framework scaffold for the Meijer demo.

## Project layout

- `StockAware.csproj` - .NET 8 console project with `Microsoft.Agents.AI`
- `Program.cs` - Microsoft Agent Framework agent setup
- `ShelfAware.slnx` - solution file

The agent reads configuration from user secrets first, then from environment variables. Recommended keys are:

```powershell
dotnet user-secrets set "StockAware:Endpoint" "https://your-foundry-project-endpoint"
dotnet user-secrets set "StockAware:DeploymentName" "your-model-deployment-name"
```

Environment variables are also supported for compatibility with the original `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_DEPLOYMENT_NAME` names.

## Run

```powershell
dotnet run --project .\StockAware.csproj
```
