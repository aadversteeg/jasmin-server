namespace Core.Domain.McpServers;

/// <summary>
/// Represents a single line of stderr output from an MCP server instance.
/// </summary>
/// <param name="LineNumber">The 1-based sequential line number within the instance.</param>
/// <param name="TimestampUtc">The UTC timestamp when the line was captured.</param>
/// <param name="Text">The text content of the stderr line.</param>
public record McpServerInstanceLogEntry(
    long LineNumber,
    DateTime TimestampUtc,
    string Text);
