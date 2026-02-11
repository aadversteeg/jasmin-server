namespace Core.Infrastructure.WebApp.Models.McpServers.Instances;

/// <summary>
/// Response model for a single instance log entry.
/// </summary>
public record LogEntryResponse(
    long LineNumber,
    string Timestamp,
    string Text);
