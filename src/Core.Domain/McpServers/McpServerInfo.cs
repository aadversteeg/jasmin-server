namespace Core.Domain.McpServers;

/// <summary>
/// Summary information about an MCP server for list responses.
/// </summary>
public record McpServerInfo
{
    /// <summary>
    /// The unique identifier of the MCP server.
    /// </summary>
    public McpServerName Id { get; }

    /// <summary>
    /// The command to execute (e.g., "npx", "docker", "dotnet").
    /// Null if the server has no configuration.
    /// </summary>
    public string? Command { get; }

    /// <summary>
    /// The verification status of the MCP server.
    /// </summary>
    public McpServerConnectionStatus Status { get; }

    /// <summary>
    /// The UTC timestamp when the status was last updated.
    /// </summary>
    public DateTime? UpdatedOnUtc { get; }

    /// <summary>
    /// Indicates whether the server has a configuration.
    /// </summary>
    public bool HasConfiguration => Command != null;

    public McpServerInfo(
        McpServerName id,
        string? command = null,
        McpServerConnectionStatus status = McpServerConnectionStatus.Unknown,
        DateTime? updatedOnUtc = null)
    {
        Id = id;
        Command = command;
        Status = status;
        UpdatedOnUtc = updatedOnUtc;
    }

    /// <summary>
    /// Creates a new McpServerInfo with an updated status and timestamp.
    /// </summary>
    /// <param name="status">The new connection status.</param>
    /// <param name="updatedOnUtc">The UTC timestamp when updated.</param>
    /// <returns>A new McpServerInfo instance with the updated status and timestamp.</returns>
    public McpServerInfo WithStatus(McpServerConnectionStatus status, DateTime? updatedOnUtc) =>
        new(Id, Command, status, updatedOnUtc);
}
