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

    // Text goes out in pieces of TextChunkSize characters, like a real model streams it, so
    // anything that reassembles the text (e.g. marker filtering) is exercised across boundaries.
    public int TextChunkSize { get; init; } = 3;

    // Called before each streamed update; tests use it to cancel mid-answer.
    public Action<int>? OnUpdate { get; init; }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        var sent = 0;
        foreach (var message in response.Messages)
        {
            foreach (var content in message.Contents)
            {
                var pieces = content is TextContent text
                    ? text.Text.Chunk(TextChunkSize).Select(chars => (AIContent)new TextContent(new string(chars)))
                    : [content];
                foreach (var piece in pieces)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    OnUpdate?.Invoke(sent++);
                    await Task.Yield();
                    yield return new ChatResponseUpdate(message.Role, [piece]) { MessageId = message.MessageId };
                }
            }
        }
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    public static ChatResponse Text(string text) => new(new ChatMessage(ChatRole.Assistant, text));

    public static ChatResponse CallTool(string name, Dictionary<string, object?> arguments) =>
        new(new ChatMessage(ChatRole.Assistant, [new FunctionCallContent(Guid.NewGuid().ToString("N"), name, arguments)]));
}
