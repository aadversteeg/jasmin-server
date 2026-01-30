using System.Text.Json.Serialization;

namespace Core.Infrastructure.McpServers.FileStorage;

/// <summary>
/// Represents the structure of the MCP servers configuration file.
/// </summary>
internal class McpServerConfigFile
{
    [JsonPropertyName("mcpServers")]
    public Dictionary<string, McpServerConfigEntry> McpServers { get; set; } = [];
}

/// <summary>
/// Represents a single MCP server entry in the configuration file.
/// Command is null when the server has no configuration.
/// </summary>
internal class McpServerConfigEntry
{
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    [JsonPropertyName("env")]
    public Dictionary<string, string>? Env { get; set; }

    /// <summary>
    /// Indicates whether this entry has a configuration.
    /// </summary>
    [JsonIgnore]
    public bool HasConfiguration => Command != null;
}
