namespace Core.Domain.McpServers;

/// <summary>
/// Actions that can be requested for an MCP server.
/// </summary>
public enum McpServerRequestAction
{
    /// <summary>
    /// Request to start a new server instance.
    /// </summary>
    Start = 0,

    /// <summary>
    /// Request to stop an existing server instance.
    /// </summary>
    Stop = 1,

    /// <summary>
    /// Request to invoke a tool on a running instance.
    /// </summary>
    InvokeTool = 2
}
