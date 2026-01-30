using Core.Application.McpServers;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Application;

/// <summary>
/// Extension methods for registering application services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds application services to the service collection.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IMcpServerService, McpServerService>();
        return services;
    }
}
