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
    /// Server metadata retrieval is starting.
    /// </summary>
    MetadataRetrieving = 9,

    /// <summary>
    /// Server metadata was retrieved successfully.
    /// </summary>
    MetadataRetrieved = 10,

    /// <summary>
    /// Server metadata retrieval failed (partially or completely).
    /// </summary>
    MetadataRetrievalFailed = 11
}
