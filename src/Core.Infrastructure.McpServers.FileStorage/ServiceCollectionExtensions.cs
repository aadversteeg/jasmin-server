using Core.Application.McpServers;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.McpServers.FileStorage;

/// <summary>
/// Extension methods for configuring MCP server file storage services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the file-based MCP server repository to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configFilePath">The path to the MCP servers configuration file.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpServerFileStorage(
        this IServiceCollection services,
        string configFilePath)
    {
        services.AddSingleton<IMcpServerRepository>(new McpServerFileRepository(configFilePath));
        return services;
    }
}
