namespace Core.Domain.McpServers;

/// <summary>
/// Types of events that can occur during MCP server lifecycle.
/// </summary>
public enum McpServerEventType
{
    /// <summary>
    /// Server is attempting to start.
    /// </summary>
    Starting = 0,

    /// <summary>
    /// Server has successfully started.
    /// </summary>
    Started = 1,

    /// <summary>
    /// Server failed to start.
    /// </summary>
    StartFailed = 2,

    /// <summary>
    /// Server is attempting to stop.
    /// </summary>
    Stopping = 3,

    /// <summary>
    /// Server has successfully stopped.
    /// </summary>
    Stopped = 4,

    /// <summary>
    /// Server failed to stop.
    /// </summary>
    StopFailed = 5,

    /// <summary>
    /// Server configuration was created.
    /// </summary>
    ConfigurationCreated = 6,

    /// <summary>
    /// Server configuration was updated.
    /// </summary>
    ConfigurationUpdated = 7,

    /// <summary>
    /// Server configuration was deleted.
    /// </summary>
    ConfigurationDeleted = 8,

    /// <summary>
    /// Tools retrieval is starting.
    /// </summary>
    ToolsRetrieving = 9,

    /// <summary>
    /// Tools were retrieved successfully.
    /// </summary>
    ToolsRetrieved = 10,

    /// <summary>
    /// Tools retrieval failed.
    /// </summary>
    ToolsRetrievalFailed = 11,

    /// <summary>
    /// Prompts retrieval is starting.
    /// </summary>
    PromptsRetrieving = 12,

    /// <summary>
    /// Prompts were retrieved successfully.
    /// </summary>
    PromptsRetrieved = 13,

    /// <summary>
    /// Prompts retrieval failed.
    /// </summary>
    PromptsRetrievalFailed = 14,

    /// <summary>
    /// Resources retrieval is starting.
    /// </summary>
    ResourcesRetrieving = 15,

    /// <summary>
    /// Resources were retrieved successfully.
    /// </summary>
    ResourcesRetrieved = 16,

    /// <summary>
    /// Resources retrieval failed.
    /// </summary>
    ResourcesRetrievalFailed = 17,

    /// <summary>
    /// Request to invoke a tool was accepted and queued.
    /// </summary>
    ToolInvocationAccepted = 18,

    /// <summary>
    /// Tool invocation is starting.
    /// </summary>
    ToolInvoking = 19,

    /// <summary>
    /// Tool was invoked successfully.
    /// </summary>
    ToolInvoked = 20,

    /// <summary>
    /// Tool invocation failed.
    /// </summary>
    ToolInvocationFailed = 21
}
