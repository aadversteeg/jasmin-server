namespace Core.Domain.McpServers;

/// <summary>
/// Represents the verification status of an MCP server.
/// </summary>
public enum McpServerConnectionStatus
{
    /// <summary>
    /// Verification status has not been checked yet.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Successfully verified the MCP server is available.
    /// </summary>
    Verified = 1,

    /// <summary>
    /// Failed to connect to the MCP server.
    /// </summary>
    Failed = 2
}
