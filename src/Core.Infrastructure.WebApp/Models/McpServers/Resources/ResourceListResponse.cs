namespace Core.Infrastructure.WebApp.Models.McpServers.Resources;

/// <summary>
/// Response model for a list of resources.
/// </summary>
public record ResourceListResponse(
    IReadOnlyList<ResourceResponse> Items,
    string? RetrievedAt,
    IReadOnlyList<MetadataRetrievalErrorResponse>? Errors);
