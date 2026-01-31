using Core.Application.McpServers;
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
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInMemoryEventStore(this IServiceCollection services)
    {
        services.AddSingleton<IEventStore, EventStore>();
        return services;
    }
}
