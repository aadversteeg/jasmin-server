namespace Core.Domain.McpServers;

/// <summary>
/// Summary information about an MCP server for list responses.
/// </summary>
public record McpServerInfo
{
    /// <summary>
    /// The unique identifier of the MCP server.
    /// </summary>
    public McpServerId Id { get; }

    /// <summary>
    /// The command to execute (e.g., "npx", "docker", "dotnet").
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// The connection status of the MCP server.
    /// </summary>
    public McpServerConnectionStatus Status { get; }

    public McpServerInfo(McpServerId id, string command, McpServerConnectionStatus status = McpServerConnectionStatus.Unknown)
    {
        Id = id;
        Command = command;
        Status = status;
    }

    /// <summary>
    /// Creates a new McpServerInfo with an updated status.
    /// </summary>
    /// <param name="status">The new connection status.</param>
    /// <returns>A new McpServerInfo instance with the updated status.</returns>
    public McpServerInfo WithStatus(McpServerConnectionStatus status) =>
        new(Id, Command, status);
}
