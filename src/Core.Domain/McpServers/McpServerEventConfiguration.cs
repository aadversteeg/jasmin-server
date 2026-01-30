namespace Core.Domain.McpServers;

/// <summary>
/// Represents configuration data captured in an event.
/// </summary>
/// <param name="Command">The command to execute.</param>
/// <param name="Args">Command line arguments.</param>
/// <param name="Env">Environment variables.</param>
public record McpServerEventConfiguration(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env)
{
    /// <summary>
    /// Creates an event configuration from a server definition.
    /// </summary>
    public static McpServerEventConfiguration? FromDefinition(McpServerDefinition? definition)
    {
        if (definition == null || !definition.HasConfiguration)
        {
            return null;
        }

        return new McpServerEventConfiguration(
            definition.Command!,
            definition.Args!,
            definition.Env!);
    }
}
