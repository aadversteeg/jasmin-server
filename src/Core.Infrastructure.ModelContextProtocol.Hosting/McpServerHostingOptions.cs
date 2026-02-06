namespace Core.Infrastructure.ModelContextProtocol.Hosting;

/// <summary>
/// Configuration options for MCP server hosting.
/// </summary>
public class McpServerHostingOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "McpServerHosting";

    /// <summary>
    /// The timeout for tool invocations in seconds.
    /// If a tool doesn't respond within this time, the invocation will fail.
    /// Default: 120 seconds (2 minutes).
    /// </summary>
    public int ToolInvocationTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// The timeout for establishing connections to MCP servers in seconds.
    /// Default: 30 seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 30;
}
