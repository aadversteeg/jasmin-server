using Core.Application.McpServers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Extension methods for configuring MCP server connection status caching.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MCP server connection status caching and initialization.
    /// </summary>
    /// <remarks>
    /// This must be called AFTER the underlying repository is registered.
    /// It decorates the existing IMcpServerRepository with status enrichment.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpServerConnectionStatusCaching(this IServiceCollection services)
    {
        // Register the status cache as singleton (shared state)
        services.AddSingleton<IMcpServerConnectionStatusCache, McpServerConnectionStatusCache>();

        // Register the initialization background service
        services.AddHostedService<McpServerConnectionInitializationService>();

        // Decorate the existing repository with status enrichment
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IMcpServerRepository));
        if (descriptor != null)
        {
            services.Remove(descriptor);

            services.AddSingleton<IMcpServerRepository>(provider =>
            {
                var innerRepository = descriptor.ImplementationInstance as IMcpServerRepository
                    ?? (descriptor.ImplementationFactory?.Invoke(provider) as IMcpServerRepository)
                    ?? throw new InvalidOperationException("Could not resolve inner IMcpServerRepository");

                var statusCache = provider.GetRequiredService<IMcpServerConnectionStatusCache>();
                return new McpServerRepositoryWithStatus(innerRepository, statusCache);
            });
        }

        return services;
    }
}
