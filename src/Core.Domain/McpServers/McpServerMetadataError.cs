namespace Core.Domain.McpServers;

/// <summary>
/// Represents an error that occurred during metadata retrieval.
/// </summary>
/// <param name="Category">The category of metadata (Tools, Prompts, Resources).</param>
/// <param name="ErrorMessage">The error message.</param>
public record McpServerMetadataError(
    string Category,
    string ErrorMessage);
