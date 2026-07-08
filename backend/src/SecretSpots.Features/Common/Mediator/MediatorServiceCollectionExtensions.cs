using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace SecretSpots.Features.Common.Mediator;

public static class MediatorServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services, Assembly assembly)
    {
        services.AddScoped<ISender, Sender>();

        var openHandlerInterface = typeof(IRequestHandler<,>);

        var registrations =
            from type in assembly.GetTypes()
            where !type.IsAbstract && !type.IsInterface
            from handlerInterface in type.GetInterfaces()
            where handlerInterface.IsGenericType && handlerInterface.GetGenericTypeDefinition() == openHandlerInterface
            select (Interface: handlerInterface, Implementation: type);

        foreach (var (@interface, implementation) in registrations)
        {
            services.AddScoped(@interface, implementation);
        }

        return services;
    }
}
