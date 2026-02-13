using Core.Application.Events;
using Core.Domain.Events;
using Core.Infrastructure.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.Messaging.InMemory;

/// <summary>
/// Extension methods for registering in-memory messaging services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-memory event store to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryEventStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add event publisher infrastructure
        services.AddEventPublisher<Event>(configuration);

        // Register event store
        services.AddSingleton<EventStore>();
        services.AddSingleton<IEventStore>(sp => sp.GetRequiredService<EventStore>());

        // Register event store handler (with its own queue)
        services.AddEventHandler<Event, EventStoreHandler>();

        return services;
    }
}
