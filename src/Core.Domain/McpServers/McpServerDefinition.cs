namespace Core.Domain.McpServers;

/// <summary>
/// Represents a complete MCP server configuration.
/// </summary>
public record McpServerDefinition
{
    /// <summary>
    /// The unique identifier of the MCP server.
    /// </summary>
    public McpServerName Id { get; }

    /// <summary>
    /// The command to execute (e.g., "npx", "docker", "dotnet").
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Command line arguments for the server.
    /// </summary>
    public IReadOnlyList<string> Args { get; }

    /// <summary>
    /// Environment variables to pass to the server process.
    /// </summary>
    public IReadOnlyDictionary<string, string> Env { get; }

    public McpServerDefinition(
        McpServerName id,
        string command,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string> env)
    {
        Id = id;
        Command = command;
        Args = args;
        Env = env;
    }
}
