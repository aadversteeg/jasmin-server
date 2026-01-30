namespace Core.Domain.McpServers;

/// <summary>
/// Represents a unique identifier for an MCP server, used internally for cache tracking.
/// </summary>
public record McpServerId
{
    public string Value { get; }

    private McpServerId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new McpServerId with a random GUID.
    /// </summary>
    public static McpServerId Create() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Creates an McpServerId from an existing GUID string.
    /// </summary>
    public static McpServerId From(string value) => new(value);

    public override string ToString() => Value;

    public override int GetHashCode() => Value.GetHashCode();
}
