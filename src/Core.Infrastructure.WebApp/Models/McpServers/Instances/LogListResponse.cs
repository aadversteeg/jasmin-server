namespace Core.Infrastructure.WebApp.Models.McpServers.Instances;

/// <summary>
/// Response model for a paginated list of instance log entries.
/// </summary>
/// <param name="Items">The log entries in this page.</param>
/// <param name="TotalItems">Total number of log entries for the instance.</param>
/// <param name="LastLineNumber">The line number of the last entry in this page, for use as the cursor in the next request.</param>
public record LogListResponse(
    IReadOnlyList<LogEntryResponse> Items,
    int TotalItems,
    long? LastLineNumber);
