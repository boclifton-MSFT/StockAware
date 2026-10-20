using Microsoft.Extensions.Configuration;

namespace StockAware;

internal sealed record DemoSettings(Uri Endpoint, string Deployment, string DataDirectory, string McpProject)
{
    internal static DemoSettings Load(IConfiguration config)
    {
        var endpoint = config["StockAware:Endpoint"] ?? config["AZURE_OPENAI_ENDPOINT"];
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || !uri.AbsolutePath.Contains("/api/projects/", StringComparison.Ordinal))
            throw new InvalidOperationException("Set StockAware:Endpoint to an HTTPS Foundry project endpoint (.../api/projects/PROJECT). Run az login before live requests.");
        var deployment = config["StockAware:DeploymentName"] ?? config["AZURE_OPENAI_DEPLOYMENT_NAME"];
        if (string.IsNullOrWhiteSpace(deployment))
            throw new InvalidOperationException("Set StockAware:DeploymentName to a deployed model supporting Chat Completions, function calls and Responses (for /research).");
        var data = config["StockAware:DataDirectory"]
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StockAware");
        var project = config["StockAware:McpProject"] ?? FindMcpProject();
        return new(uri, deployment, Path.GetFullPath(data), Path.GetFullPath(project));
    }

    internal static string FindMcpProject()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "StockAwareMcp", "StockAwareMcp.csproj");
            if (File.Exists(path)) return path;
        }
        throw new FileNotFoundException("Cannot locate StockAwareMcp. Set StockAware:McpProject to its absolute project path and build it first.");
    }
}
