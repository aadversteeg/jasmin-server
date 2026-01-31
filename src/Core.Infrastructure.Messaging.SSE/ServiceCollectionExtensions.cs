using Core.Domain.McpServers;
using Core.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.Messaging.SSE;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSseEventStreaming(this IServiceCollection services)
    {
        services.AddSingleton<SseClientManager>();
        services.AddEventHandler<McpServerEvent, SseEventHandler>();
        return services;
    }
}
