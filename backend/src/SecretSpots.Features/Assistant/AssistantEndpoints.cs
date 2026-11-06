using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using SecretSpots.Features.Common.Configuration;
using SecretSpots.Features.Common.Mediator;
using SecretSpots.Features.Common.Results;

namespace SecretSpots.Features.Assistant;

public static class AssistantEndpoints
{
    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/assistant/chat", async (
                AskAssistant.Command command, IOptions<AssistantOptions> assistantOptions,
                ISender sender, CancellationToken cancellationToken) =>
            {
                // Checked before validation too, so a disabled assistant is simply not there.
                if (!assistantOptions.Value.Enabled)
                {
                    return Results.NotFound();
                }

                var result = await sender.Send(command, cancellationToken);
                return result.ToOkOrProblem();
            })
            .WithTags("Assistant")
            .RequireAuthorization()
            .Produces<AssistantAnswerResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return app;
    }
}
