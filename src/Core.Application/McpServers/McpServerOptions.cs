namespace Core.Application.McpServers;

/// <summary>
/// Options for configuring MCP server connections.
/// </summary>
public class McpServerOptions
{
    /// <summary>
    /// The configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "McpServers";

    /// <summary>
    /// Dictionary of MCP server definitions keyed by server name.
    /// </summary>
    public Dictionary<string, McpServerConfigEntry> Servers { get; set; } = [];
}

/// <summary>
/// Configuration entry for a single MCP server (matches appsettings structure).
/// </summary>
public class McpServerConfigEntry
{
    /// <summary>
    /// The command to execute (e.g., "npx", "docker", "dotnet").
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Command line arguments for the server.
    /// </summary>
    public List<string> Args { get; set; } = [];

    /// <summary>
    /// Environment variables to pass to the server process.
    /// </summary>
    public Dictionary<string, string> Env { get; set; } = [];
}
