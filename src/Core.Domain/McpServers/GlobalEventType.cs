namespace Core.Domain.McpServers;

/// <summary>
/// Types of global events that can occur in the system.
/// </summary>
public enum GlobalEventType
{
    /// <summary>
    /// A server was created.
    /// </summary>
    ServerCreated = 0,

    /// <summary>
    /// A server was deleted.
    /// </summary>
    ServerDeleted = 1
}
