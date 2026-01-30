namespace Core.Domain.McpServers;

/// <summary>
/// Represents a unique identifier for an MCP server instance.
/// Each running instance of an MCP server has its own unique instance ID.
/// </summary>
public record McpServerInstanceId
{
    public string Value { get; }

    private McpServerInstanceId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new McpServerInstanceId with a random GUID.
    /// </summary>
    public static McpServerInstanceId Create() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Creates an McpServerInstanceId from an existing GUID string.
    /// </summary>
    public static McpServerInstanceId From(string value) => new(value);

    public override string ToString() => Value;

    public override int GetHashCode() => Value.GetHashCode();
}
