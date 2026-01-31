using Core.Application.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Messaging;

/// <summary>
/// Extension methods for registering event publisher services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the event publisher infrastructure for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of event to publish.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventPublisher<T>(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = new EventPublisherSettings();
        configuration.GetSection("EventPublisher").Bind(settings);
        services.AddSingleton(settings);

        services.AddSingleton<IEventPublisher<T>, EventPublisher<T>>();

        return services;
    }

    /// <summary>
    /// Adds an event handler with its own queue for the specified event type.
    /// </summary>
    /// <typeparam name="T">The type of event to handle.</typeparam>
    /// <typeparam name="THandler">The handler implementation type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEventHandler<T, THandler>(
        this IServiceCollection services)
        where THandler : class, IEventHandler<T>
    {
        services.AddSingleton<THandler>();
        services.AddSingleton<IEventHandler<T>>(sp => sp.GetRequiredService<THandler>());

        // Use typed runner to ensure each handler gets its own distinct registration
        services.AddSingleton<TypedHandlerRunner<T, THandler>>();
        services.AddSingleton<HandlerRunner<T>>(sp => sp.GetRequiredService<TypedHandlerRunner<T, THandler>>());
        services.AddHostedService(sp => sp.GetRequiredService<TypedHandlerRunner<T, THandler>>());

        return services;
    }
}
