# StockAware

Barebones Microsoft Agent Framework scaffold for the grocery retail demo.

## Presentation story

This project is the starting point for a live lecture that builds a practical generative AI demo incrementally inside VS Code. The path is intentionally staged: the audience sees a simple baseline agent, then a retail persona, then typed tools, structured outputs, orchestration, and guardrails.

The final version lives in `../StockAware-FINAL_1` and represents the polished end-state sample that the live demo builds toward.

## Project layout

- `StockAware.csproj` - .NET 10 console project with `Microsoft.Agents.AI`
- `Program.cs` - minimal baseline Microsoft Agent Framework setup
- `InMemoryVectorStore.cs` - demo-only `VectorStore` mock; storage operations throw `NotSupportedException` rather than implementing persistence or vector search
- `../StockAware-FINAL_1/` - the complete final-state retail agent demo
- `.vscode/CodeSnippets-StockAware.code-snippets` - reusable slide-friendly code snippets for the lecture flow

The agent reads configuration from user secrets first, then from environment variables. Recommended keys are:

```powershell
dotnet user-secrets set "StockAware:Endpoint" "https://your-foundry-project-endpoint"
dotnet user-secrets set "StockAware:DeploymentName" "your-model-deployment-name"
```

Environment variables are also supported for compatibility with the original `AZURE_OPENAI_ENDPOINT` and `AZURE_OPENAI_DEPLOYMENT_NAME` names.

## Run the baseline project

```powershell
dotnet run --project .\StockAware.csproj
```

## Run the final project

```powershell
dotnet run --project ..\StockAware-FINAL_1\StockAware-FINAL_1.csproj
```

The final project works even without Azure configuration by falling back to a local retail demo dataset so the presentation can still flow smoothly when credentials are unavailable.
