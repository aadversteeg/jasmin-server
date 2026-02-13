using Core.Domain.Events;

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
        return new List<EventTypeResponse>
        {
            new(EventTypes.McpServer.Created, "lifecycle", "Server was created."),
            new(EventTypes.McpServer.Deleted, "lifecycle", "Server was deleted."),
            new(EventTypes.McpServer.Configuration.Created, "configuration", "Server configuration was created."),
            new(EventTypes.McpServer.Configuration.Updated, "configuration", "Server configuration was updated."),
            new(EventTypes.McpServer.Configuration.Deleted, "configuration", "Server configuration was deleted."),
            new(EventTypes.McpServer.Instance.Starting, "lifecycle", "Server instance is starting."),
            new(EventTypes.McpServer.Instance.Started, "lifecycle", "Server instance has started."),
            new(EventTypes.McpServer.Instance.StartFailed, "lifecycle", "Server instance failed to start."),
            new(EventTypes.McpServer.Instance.Stopping, "lifecycle", "Server instance is stopping."),
            new(EventTypes.McpServer.Instance.Stopped, "lifecycle", "Server instance has stopped."),
            new(EventTypes.McpServer.Instance.StopFailed, "lifecycle", "Server instance failed to stop."),
            new(EventTypes.McpServer.Metadata.Tools.Retrieving, "metadata", "Tools retrieval is starting."),
            new(EventTypes.McpServer.Metadata.Tools.Retrieved, "metadata", "Tools were retrieved successfully."),
            new(EventTypes.McpServer.Metadata.Tools.RetrievalFailed, "metadata", "Tools retrieval failed."),
            new(EventTypes.McpServer.Metadata.Prompts.Retrieving, "metadata", "Prompts retrieval is starting."),
            new(EventTypes.McpServer.Metadata.Prompts.Retrieved, "metadata", "Prompts were retrieved successfully."),
            new(EventTypes.McpServer.Metadata.Prompts.RetrievalFailed, "metadata", "Prompts retrieval failed."),
            new(EventTypes.McpServer.Metadata.Resources.Retrieving, "metadata", "Resources retrieval is starting."),
            new(EventTypes.McpServer.Metadata.Resources.Retrieved, "metadata", "Resources were retrieved successfully."),
            new(EventTypes.McpServer.Metadata.Resources.RetrievalFailed, "metadata", "Resources retrieval failed."),
            new(EventTypes.McpServer.ToolInvocation.Accepted, "tool-invocation", "Tool invocation was accepted and queued."),
            new(EventTypes.McpServer.ToolInvocation.Invoking, "tool-invocation", "Tool invocation is starting."),
            new(EventTypes.McpServer.ToolInvocation.Invoked, "tool-invocation", "Tool was invoked successfully."),
            new(EventTypes.McpServer.ToolInvocation.Failed, "tool-invocation", "Tool invocation failed."),
        }.AsReadOnly();
    }
}
