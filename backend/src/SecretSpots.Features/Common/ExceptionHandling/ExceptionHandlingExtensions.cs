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

    private static async Task HandleAsync(HttpContext context)
    {
        var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (error is ValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                errors = validationException.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }),
            });
            return;
        }

        var localizer = context.RequestServices.GetRequiredService<IStringLocalizer<SharedResources>>();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new
        {
            message = localizer[CommonMessageKeys.UnexpectedError].Value,
        });
    }
}
