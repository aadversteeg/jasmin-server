namespace Core.Infrastructure.WebApp.Models.McpServers.Prompts;

/// <summary>
/// Response model for a list of prompts.
/// </summary>
public record PromptListResponse(
    IReadOnlyList<PromptResponse> Items,
    string? RetrievedAt,
    IReadOnlyList<MetadataRetrievalErrorResponse>? Errors);
