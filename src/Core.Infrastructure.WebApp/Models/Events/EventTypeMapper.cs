using Core.Domain.McpServers;

namespace Core.Infrastructure.WebApp.Models.Events;

/// <summary>
/// Mapper for converting event types to response models.
/// </summary>
public static class EventTypeMapper
{
    private static readonly IReadOnlyList<EventTypeResponse> _eventTypes = BuildEventTypes();

    /// <summary>
    /// Returns a list response containing all event types.
    /// </summary>
    public static EventTypeListResponse ToListResponse() => new(_eventTypes);

    private static IReadOnlyList<EventTypeResponse> BuildEventTypes()
    {
        return Enum.GetValues<McpServerEventType>()
            .Select(e => new EventTypeResponse(
                e.ToString(),
                (int)e,
                GetCategory(e).ToString(),
                GetDescription(e)))
            .ToList()
            .AsReadOnly();
    }

    private static McpServerEventCategory GetCategory(McpServerEventType eventType) => eventType switch
    {
        McpServerEventType.Starting or
        McpServerEventType.Started or
        McpServerEventType.StartFailed or
        McpServerEventType.Stopping or
        McpServerEventType.Stopped or
        McpServerEventType.StopFailed or
        McpServerEventType.ServerCreated or
        McpServerEventType.ServerDeleted => McpServerEventCategory.Lifecycle,

        McpServerEventType.ConfigurationCreated or
        McpServerEventType.ConfigurationUpdated or
        McpServerEventType.ConfigurationDeleted => McpServerEventCategory.Configuration,

        McpServerEventType.ToolsRetrieving or
        McpServerEventType.ToolsRetrieved or
        McpServerEventType.ToolsRetrievalFailed or
        McpServerEventType.PromptsRetrieving or
        McpServerEventType.PromptsRetrieved or
        McpServerEventType.PromptsRetrievalFailed or
        McpServerEventType.ResourcesRetrieving or
        McpServerEventType.ResourcesRetrieved or
        McpServerEventType.ResourcesRetrievalFailed => McpServerEventCategory.Metadata,

        McpServerEventType.ToolInvocationAccepted or
        McpServerEventType.ToolInvoking or
        McpServerEventType.ToolInvoked or
        McpServerEventType.ToolInvocationFailed => McpServerEventCategory.ToolInvocation,

        _ => McpServerEventCategory.Lifecycle
    };

    private static string GetDescription(McpServerEventType eventType) => eventType switch
    {
        McpServerEventType.Starting => "Server is attempting to start.",
        McpServerEventType.Started => "Server has successfully started.",
        McpServerEventType.StartFailed => "Server failed to start.",
        McpServerEventType.Stopping => "Server is attempting to stop.",
        McpServerEventType.Stopped => "Server has successfully stopped.",
        McpServerEventType.StopFailed => "Server failed to stop.",
        McpServerEventType.ConfigurationCreated => "Server configuration was created.",
        McpServerEventType.ConfigurationUpdated => "Server configuration was updated.",
        McpServerEventType.ConfigurationDeleted => "Server configuration was deleted.",
        McpServerEventType.ToolsRetrieving => "Tools retrieval is starting.",
        McpServerEventType.ToolsRetrieved => "Tools were retrieved successfully.",
        McpServerEventType.ToolsRetrievalFailed => "Tools retrieval failed.",
        McpServerEventType.PromptsRetrieving => "Prompts retrieval is starting.",
        McpServerEventType.PromptsRetrieved => "Prompts were retrieved successfully.",
        McpServerEventType.PromptsRetrievalFailed => "Prompts retrieval failed.",
        McpServerEventType.ResourcesRetrieving => "Resources retrieval is starting.",
        McpServerEventType.ResourcesRetrieved => "Resources were retrieved successfully.",
        McpServerEventType.ResourcesRetrievalFailed => "Resources retrieval failed.",
        McpServerEventType.ToolInvocationAccepted => "Request to invoke a tool was accepted and queued.",
        McpServerEventType.ToolInvoking => "Tool invocation is starting.",
        McpServerEventType.ToolInvoked => "Tool was invoked successfully.",
        McpServerEventType.ToolInvocationFailed => "Tool invocation failed.",
        McpServerEventType.ServerCreated => "Server was created (registered in the system).",
        McpServerEventType.ServerDeleted => "Server was deleted (removed from the system).",
        _ => "Unknown event type."
    };
}
