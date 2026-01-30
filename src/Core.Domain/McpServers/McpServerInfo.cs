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

    public McpServerInfo(McpServerId id, string command)
    {
        Id = id;
        Command = command;
    }
}
