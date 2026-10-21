using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace _04_MemoryAndPersistence;

internal sealed class CustomChatHistoryProvider(string directory) : ChatHistoryProvider
{
    private readonly string _directory = Path.GetFullPath(directory);
    private readonly ProviderSessionState<string> _sessionState = new(
        _ => Guid.NewGuid().ToString("N"), nameof(CustomChatHistoryProvider));

    public override IReadOnlyList<string> StateKeys => [_sessionState.StateKey];

    protected override async ValueTask<IEnumerable<ChatMessage>> ProvideChatHistoryAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
        => await ReadMessagesAsync(GetHistoryPath(context.Session), cancellationToken);

    protected override async ValueTask StoreChatHistoryAsync(
        InvokedContext context, CancellationToken cancellationToken = default)
    {
        var path = GetHistoryPath(context.Session);
        var messages = await ReadMessagesAsync(path, cancellationToken);
        messages.AddRange(context.RequestMessages);
        messages.AddRange(context.ResponseMessages ?? []);

        Directory.CreateDirectory(_directory);
        await using (var stream = File.Create(path + ".tmp"))
        {
            await JsonSerializer.SerializeAsync(stream, messages, cancellationToken: cancellationToken);
        }

        File.Move(path + ".tmp", path, overwrite: true);
    }

    private string GetHistoryPath(AgentSession? session)
    {
        var id = _sessionState.GetOrInitializeState(session);
        if (!Guid.TryParseExact(id, "N", out var sessionId))
        {
            throw new InvalidDataException("The stored history identifier is not a valid session ID.");
        }

        return Path.Combine(_directory, $"{sessionId:N}.json");
    }

    private static async Task<List<ChatMessage>> ReadMessagesAsync(
        string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<ChatMessage>>(stream, cancellationToken: cancellationToken)
            ?? throw new InvalidDataException($"Chat history in '{path}' must be a JSON array.");
    }
}
