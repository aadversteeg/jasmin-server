using Core.Application.McpServers;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.ModelContextProtocol.Hosting;

/// <summary>
/// Extension methods for configuring MCP server hosting services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds MCP server hosting services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpServerHosting(this IServiceCollection services)
    {
        services.AddSingleton<IMcpServerInstanceManager, McpServerInstanceManager>();
        return services;
    }
}
