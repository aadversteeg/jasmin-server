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
    /// The verification status of the MCP server.
    /// </summary>
    public McpServerConnectionStatus Status { get; }

    /// <summary>
    /// The UTC timestamp when the status was last verified.
    /// </summary>
    public DateTime? VerifiedAtUtc { get; }

    public McpServerInfo(
        McpServerId id,
        string command,
        McpServerConnectionStatus status = McpServerConnectionStatus.Unknown,
        DateTime? verifiedAtUtc = null)
    {
        Id = id;
        Command = command;
        Status = status;
        VerifiedAtUtc = verifiedAtUtc;
    }

    /// <summary>
    /// Creates a new McpServerInfo with an updated status and timestamp.
    /// </summary>
    /// <param name="status">The new connection status.</param>
    /// <param name="verifiedAtUtc">The UTC timestamp when verified.</param>
    /// <returns>A new McpServerInfo instance with the updated status and timestamp.</returns>
    public McpServerInfo WithStatus(McpServerConnectionStatus status, DateTime? verifiedAtUtc) =>
        new(Id, Command, status, verifiedAtUtc);
}
