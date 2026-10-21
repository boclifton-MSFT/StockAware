using Microsoft.Agents.AI.Workflows;

namespace _05_Workflows;

internal sealed class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
{
    public override ValueTask<string> HandleAsync(
        string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(string.Concat(message.Reverse()));
}
