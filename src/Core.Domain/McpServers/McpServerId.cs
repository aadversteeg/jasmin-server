using Ave.Extensions.Functional;
using Core.Domain.Models;

namespace Core.Domain.McpServers;

/// <summary>
/// Represents a unique identifier for an MCP server.
/// </summary>
public record McpServerId
{
    public string Value { get; }

    private McpServerId(string value)
    {
        Value = value;
    }

    public static Result<McpServerId, Error> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Result<McpServerId, Error>.Failure(Errors.InvalidMcpServerId);
        }

        return Result<McpServerId, Error>.Success(new McpServerId(value));
    }

    public override string ToString()
    {
        return Value;
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
