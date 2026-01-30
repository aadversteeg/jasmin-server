namespace Core.Domain.McpServers;

/// <summary>
/// Represents a unique identifier for an MCP server request.
/// Each async request (start/stop) has its own unique request ID.
/// </summary>
public record McpServerRequestId
{
    public string Value { get; }

    private McpServerRequestId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new McpServerRequestId with a random GUID.
    /// </summary>
    public static McpServerRequestId Create() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Creates an McpServerRequestId from an existing GUID string.
    /// </summary>
    public static McpServerRequestId From(string value) => new(value);

    public override string ToString() => Value;

    public override int GetHashCode() => Value.GetHashCode();
}
