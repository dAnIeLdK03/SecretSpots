using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace SecretSpots.Features.Assistant;

// Writes assistant events as server-sent events ("event: <type>" + "data: <json>"), flushing each
// one so it reaches the browser as soon as it exists. Ends when the events end or the client goes
// away (RequestAborted), which also cancels the model.
public sealed class AssistantEventStreamResult(IAsyncEnumerable<AssistantEvent> events) : IResult
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        var response = httpContext.Response;
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers.CacheControl = "no-cache";
        // Tells nginx-style proxies not to buffer the stream until it ends.
        response.Headers["X-Accel-Buffering"] = "no";
        httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var cancellationToken = httpContext.RequestAborted;
        try
        {
            await foreach (var assistantEvent in events.WithCancellation(cancellationToken))
            {
                var data = JsonSerializer.Serialize(assistantEvent, JsonOptions);
                await response.WriteAsync($"event: {assistantEvent.Type}\ndata: {data}\n\n", Encoding.UTF8, cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The user stopped the answer or left; nothing left to send.
        }
    }
}
