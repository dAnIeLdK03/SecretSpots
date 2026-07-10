using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using SecretSpots.Features.Common.Localization;

namespace SecretSpots.Features.Common.ExceptionHandling;

public static class ExceptionHandlingExtensions
{
    public static IApplicationBuilder UseValidationExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseExceptionHandler(errorApp => errorApp.Run(HandleAsync));
    }

    // Both branches go through the built-in Results.ValidationProblem/Results.Problem so every
    // error response — expected validation failures, Result<T> failures (via ToProblem()), and
    // truly unexpected exceptions — shares the same ProblemDetails shape.
    private static async Task HandleAsync(HttpContext context)
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (error is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            await Microsoft.AspNetCore.Http.Results.ValidationProblem(errors).ExecuteAsync(context);
            return;
        }

        var localizer = context.RequestServices.GetRequiredService<IStringLocalizer<SharedResources>>();
        await Microsoft.AspNetCore.Http.Results.Problem(
            detail: localizer[CommonMessageKeys.UnexpectedError].Value,
            statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(context);
    }
}
