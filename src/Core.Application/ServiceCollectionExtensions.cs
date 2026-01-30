using Core.Application.McpServers;
using Microsoft.Extensions.Configuration;
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
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<McpServerOptions>(options =>
        {
            var section = configuration.GetSection(McpServerOptions.SectionName);

            foreach (var serverSection in section.GetChildren())
            {
                var serverName = serverSection.Key;
                var entry = new McpServerConfigEntry
                {
                    Command = serverSection["command"] ?? string.Empty
                };

                var argsSection = serverSection.GetSection("args");
                if (argsSection.Exists())
                {
                    entry.Args = argsSection.Get<List<string>>() ?? [];
                }

                var envSection = serverSection.GetSection("env");
                if (envSection.Exists())
                {
                    foreach (var envVar in envSection.GetChildren())
                    {
                        entry.Env[envVar.Key] = envVar.Value ?? string.Empty;
                    }
                }

                options.Servers[serverName] = entry;
            }
        });

        services.AddScoped<IMcpServerService, McpServerService>();

        return services;
    }
}
