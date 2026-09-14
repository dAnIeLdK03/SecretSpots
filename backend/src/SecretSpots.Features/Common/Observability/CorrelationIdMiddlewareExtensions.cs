using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SecretSpots.Features.Common.Observability;

public static class CorrelationIdMiddlewareExtensions
{
    public const string HeaderName = "X-Correlation-Id";

    // Every request gets a correlation id — reused from an incoming X-Correlation-Id header when
    // the caller already has one (e.g. a future upstream service, or a client retrying the same
    // logical request), otherwise a fresh one — pushed into a logging scope so every log line
    // written anywhere while handling this request carries it, and echoed back as a response
    // header so a bug report or a client-side error can be matched to the exact server log lines.
    // Registered as early as possible in the pipeline so it also covers the exception handler.
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var existing)
                && !string.IsNullOrWhiteSpace(existing)
                ? existing.ToString()
                : Guid.NewGuid().ToString("n");

            context.Response.Headers[HeaderName] = correlationId;

            var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                .CreateLogger("SecretSpots.CorrelationId");

            // The message-template overload (not a raw Dictionary) so the scope also renders as
            // readable text in the simple console formatter, not just as a structured field in
            // the JSON one — a raw Dictionary's ToString() is just its type name.
            using (logger.BeginScope("CorrelationId:{CorrelationId}", correlationId))
            {
                await next();
            }
        });
    }
}
