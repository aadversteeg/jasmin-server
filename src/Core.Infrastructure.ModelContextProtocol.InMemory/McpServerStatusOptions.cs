namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Configuration options for MCP server status.
/// </summary>
public class McpServerStatusOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "McpServerStatus";

    /// <summary>
    /// The default timezone for displaying timestamps.
    /// If not specified, UTC is used.
    /// </summary>
    public string? DefaultTimeZone { get; set; }
}
