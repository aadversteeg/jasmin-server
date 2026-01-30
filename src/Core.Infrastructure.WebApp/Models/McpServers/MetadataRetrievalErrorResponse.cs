namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for metadata retrieval errors.
/// </summary>
public record MetadataRetrievalErrorResponse(
    string Category,
    string Message);
