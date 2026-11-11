using System.Reflection;
using BookStore.Application.Common.Behaviors;
using BookStore.Application.Common.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BookStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddScoped<ISender, Mediator>();
        services.AddRequestHandlers(assembly);

        // Order matters: the first behavior registered is the outermost one.
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static void AddRequestHandlers(this IServiceCollection services, Assembly assembly)
    {
        var registrations =
            from type in assembly.GetTypes()
            where type is { IsAbstract: false, IsInterface: false }
            from contract in type.GetInterfaces()
            where contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
            select (contract, type);

        foreach (var (contract, implementation) in registrations)
        {
            services.AddScoped(contract, implementation);
        }
    }
}
