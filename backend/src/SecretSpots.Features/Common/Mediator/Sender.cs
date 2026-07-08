using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace SecretSpots.Features.Common.Mediator;

public class Sender(IServiceProvider serviceProvider) : ISender
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();

        await ValidateAsync(request, requestType, cancellationToken);

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var handler = serviceProvider.GetRequiredService(handlerType);
        var handleMethod = handlerType.GetMethod(nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle))!;

        return await (Task<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;
    }

    private async Task ValidateAsync(object request, Type requestType, CancellationToken cancellationToken)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
        if (serviceProvider.GetService(validatorType) is not IValidator validator)
        {
            return;
        }

        var validationResult = await validator.ValidateAsync(new ValidationContext<object>(request), cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
    }
}
