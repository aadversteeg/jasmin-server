using Core.Application.McpServers;
using Core.Application.Requests;
using Core.Domain.Requests;
using Core.Infrastructure.ModelContextProtocol.Hosting.RequestHandlers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpServerHosting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<McpServerHostingOptions>(
            configuration.GetSection(McpServerHostingOptions.SectionName));

        services.AddSingleton<IMcpServerInstanceManager, McpServerInstanceManager>();
        return services;
    }

    /// <summary>
    /// Adds request handler registrations and the handler registry.
    /// Must be called after <c>AddMcpServerHosting()</c> and <c>AddRequestProcessing()</c>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRequestHandlers(this IServiceCollection services)
    {
        services.AddSingleton<IRequestHandlerRegistry>(sp =>
        {
            var instanceManager = sp.GetRequiredService<IMcpServerInstanceManager>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var registry = new RequestHandlerRegistryBuilder();

            registry.Register(RequestActions.McpServer.Start,
                new McpServerStartHandler(instanceManager, loggerFactory.CreateLogger<McpServerStartHandler>()));

            registry.Register(RequestActions.McpServer.Instance.Stop,
                new McpServerInstanceStopHandler(instanceManager, loggerFactory.CreateLogger<McpServerInstanceStopHandler>()));

            registry.Register(RequestActions.McpServer.Instance.InvokeTool,
                new McpServerInvokeToolHandler(instanceManager, loggerFactory.CreateLogger<McpServerInvokeToolHandler>()));

            registry.Register(RequestActions.McpServer.Instance.GetPrompt,
                new McpServerGetPromptHandler(instanceManager, loggerFactory.CreateLogger<McpServerGetPromptHandler>()));

            registry.Register(RequestActions.McpServer.Instance.ReadResource,
                new McpServerReadResourceHandler(instanceManager, loggerFactory.CreateLogger<McpServerReadResourceHandler>()));

            registry.Register(RequestActions.McpServer.Instance.RefreshMetadata,
                new McpServerRefreshMetadataHandler(instanceManager, loggerFactory.CreateLogger<McpServerRefreshMetadataHandler>()));

            var hostingOptions = sp.GetRequiredService<IOptions<McpServerHostingOptions>>();
            registry.Register(RequestActions.McpServer.TestConfiguration,
                new McpServerTestConfigurationHandler(
                    loggerFactory.CreateLogger<McpServerTestConfigurationHandler>(),
                    hostingOptions.Value));

            return registry.Build();
        });

        return services;
    }

    /// <summary>
    /// Builder that collects handler registrations and produces an <see cref="IRequestHandlerRegistry"/>.
    /// </summary>
    private sealed class RequestHandlerRegistryBuilder
    {
        private readonly Dictionary<RequestAction, IRequestHandler> _handlers = new();

        public void Register(RequestAction action, IRequestHandler handler)
        {
            _handlers[action] = handler;
        }

        public IRequestHandlerRegistry Build() => new DictionaryRequestHandlerRegistry(_handlers);
    }

    /// <summary>
    /// Immutable registry backed by a dictionary.
    /// </summary>
    private sealed class DictionaryRequestHandlerRegistry : IRequestHandlerRegistry
    {
        private readonly Dictionary<RequestAction, IRequestHandler> _handlers;

        public DictionaryRequestHandlerRegistry(Dictionary<RequestAction, IRequestHandler> handlers)
        {
            _handlers = new Dictionary<RequestAction, IRequestHandler>(handlers);
        }

        public IRequestHandler? GetHandler(RequestAction action)
        {
            return _handlers.TryGetValue(action, out var handler) ? handler : null;
        }
    }
}
