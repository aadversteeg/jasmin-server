namespace Core.Domain.McpServers;

/// <summary>
/// Represents an MCP server with optional configuration.
/// </summary>
public record McpServerDefinition
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
    /// Command line arguments for the server.
    /// Null if the server has no configuration.
    /// </summary>
    public IReadOnlyList<string>? Args { get; }

    /// <summary>
    /// Environment variables to pass to the server process.
    /// Null if the server has no configuration.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Env { get; }

    /// <summary>
    /// Indicates whether the server has a configuration.
    /// A server without configuration cannot be started.
    /// </summary>
    public bool HasConfiguration => Command != null;

    /// <summary>
    /// Creates a new MCP server definition with configuration.
    /// </summary>
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

    /// <summary>
    /// Creates a new MCP server definition without configuration.
    /// </summary>
    public McpServerDefinition(McpServerName id)
    {
        Id = id;
        Command = null;
        Args = null;
        Env = null;
    }

    /// <summary>
    /// Creates a new MCP server definition with the configuration removed.
    /// </summary>
    public McpServerDefinition WithoutConfiguration() => new(Id);

    /// <summary>
    /// Creates a new MCP server definition with the specified configuration.
    /// </summary>
    public McpServerDefinition WithConfiguration(
        string command,
        IReadOnlyList<string> args,
        IReadOnlyDictionary<string, string> env) => new(Id, command, args, env);
}
