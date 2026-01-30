namespace Core.Domain.McpServers;

/// <summary>
/// Aggregates all metadata retrieved from an MCP server.
/// </summary>
/// <param name="Tools">The tools exposed by the server.</param>
/// <param name="Prompts">The prompts exposed by the server.</param>
/// <param name="Resources">The resources exposed by the server.</param>
/// <param name="RetrievedAtUtc">When the metadata was retrieved.</param>
/// <param name="RetrievalErrors">Any errors that occurred during retrieval.</param>
public record McpServerMetadata(
    IReadOnlyList<McpTool>? Tools,
    IReadOnlyList<McpPrompt>? Prompts,
    IReadOnlyList<McpResource>? Resources,
    DateTime RetrievedAtUtc,
    IReadOnlyList<McpServerMetadataError>? RetrievalErrors);
