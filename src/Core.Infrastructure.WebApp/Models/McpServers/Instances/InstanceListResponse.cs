namespace Core.Infrastructure.WebApp.Models.McpServers.Instances;

/// <summary>
/// Response model for a list of MCP server instances.
/// </summary>
/// <param name="Items">The list of instances.</param>
public record InstanceListResponse(IReadOnlyList<InstanceResponse> Items);
