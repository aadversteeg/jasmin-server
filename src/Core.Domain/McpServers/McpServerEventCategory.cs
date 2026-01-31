namespace Core.Domain.McpServers;

/// <summary>
/// Categories for MCP server events.
/// </summary>
public enum McpServerEventCategory
{
    /// <summary>
    /// Events related to server lifecycle (start, stop, create, delete).
    /// </summary>
    Lifecycle,

    /// <summary>
    /// Events related to server configuration changes.
    /// </summary>
    Configuration,

    /// <summary>
    /// Events related to metadata retrieval (tools, prompts, resources).
    /// </summary>
    Metadata,

    /// <summary>
    /// Events related to tool invocation.
    /// </summary>
    ToolInvocation
}
