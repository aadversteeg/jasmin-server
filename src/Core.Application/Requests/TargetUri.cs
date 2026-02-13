namespace Core.Application.Requests;

/// <summary>
/// Helper to build and parse target URIs for requests and events.
/// </summary>
public static class TargetUri
{
    /// <summary>
    /// Builds a target URI for an MCP server.
    /// </summary>
    /// <param name="name">The server name.</param>
    /// <returns>A target URI in the form <c>mcp-servers/{name}</c>.</returns>
    public static string McpServer(string name) => $"mcp-servers/{name}";

    /// <summary>
    /// Builds a target URI for an MCP server instance.
    /// </summary>
    /// <param name="name">The server name.</param>
    /// <param name="instanceId">The instance ID.</param>
    /// <returns>A target URI in the form <c>mcp-servers/{name}/instances/{instanceId}</c>.</returns>
    public static string McpServerInstance(string name, string instanceId) => $"mcp-servers/{name}/instances/{instanceId}";

    /// <summary>
    /// Tries to parse a target URI as an MCP server target.
    /// </summary>
    /// <param name="target">The target URI to parse.</param>
    /// <param name="name">The parsed server name.</param>
    /// <returns>true if the target matches the pattern <c>mcp-servers/{name}</c>; otherwise, false.</returns>
    public static bool TryParseMcpServer(string target, out string name)
    {
        name = string.Empty;

        if (string.IsNullOrEmpty(target))
        {
            return false;
        }

        var parts = target.Split('/');
        if (parts.Length == 2 && parts[0] == "mcp-servers" && !string.IsNullOrEmpty(parts[1]))
        {
            name = parts[1];
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to parse a target URI as an MCP server instance target.
    /// </summary>
    /// <param name="target">The target URI to parse.</param>
    /// <param name="name">The parsed server name.</param>
    /// <param name="instanceId">The parsed instance ID.</param>
    /// <returns>true if the target matches the pattern <c>mcp-servers/{name}/instances/{instanceId}</c>; otherwise, false.</returns>
    public static bool TryParseMcpServerInstance(string target, out string name, out string instanceId)
    {
        name = string.Empty;
        instanceId = string.Empty;

        if (string.IsNullOrEmpty(target))
        {
            return false;
        }

        var parts = target.Split('/');
        if (parts.Length == 4 && parts[0] == "mcp-servers" && !string.IsNullOrEmpty(parts[1])
            && parts[2] == "instances" && !string.IsNullOrEmpty(parts[3]))
        {
            name = parts[1];
            instanceId = parts[3];
            return true;
        }

        return false;
    }
}
