using Ave.Extensions.Functional;
using Core.Domain.Models;

namespace Core.Domain.McpServers;

/// <summary>
/// Represents a unique name for an MCP server.
/// </summary>
public record McpServerName
{
    public string Value { get; }

    private McpServerName(string value)
    {
        Value = value;
    }

    public static Result<McpServerName, Error> Create(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Result<McpServerName, Error>.Failure(Errors.InvalidMcpServerName);
        }

        return Result<McpServerName, Error>.Success(new McpServerName(value));
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
