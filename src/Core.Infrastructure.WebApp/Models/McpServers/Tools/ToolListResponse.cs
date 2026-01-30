namespace Core.Infrastructure.WebApp.Models.McpServers.Tools;

/// <summary>
/// Response model for a list of tools.
/// </summary>
public record ToolListResponse(
    IReadOnlyList<ToolResponse> Items,
    string? RetrievedAt,
    IReadOnlyList<MetadataRetrievalErrorResponse>? Errors);
