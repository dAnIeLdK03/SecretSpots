using Microsoft.Extensions.AI;

namespace SecretSpots.Features.Tests.Assistant;

// Stands in for the model: each request is answered by the script, which sees the messages so far
// (including tool results) and the 0-based request number. Wrapped in the real tool-calling loop.
internal sealed class ScriptedChatClient(Func<IReadOnlyList<ChatMessage>, int, ChatResponse> script) : IChatClient
{
    public List<IReadOnlyList<ChatMessage>> Requests { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var snapshot = messages.ToList();
        Requests.Add(snapshot);
        return Task.FromResult(script(snapshot, Requests.Count - 1));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    public static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    public static ChatResponse CallTool(string name, Dictionary<string, object?> arguments) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(Guid.NewGuid().ToString("N"), name, arguments)]));
}
