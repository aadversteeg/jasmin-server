namespace Core.Domain.McpServers;

/// <summary>
/// Represents an error that occurred during an MCP server request.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public record McpServerRequestError(string Code, string Message);
