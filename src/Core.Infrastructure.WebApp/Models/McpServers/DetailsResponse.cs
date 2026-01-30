using Core.Infrastructure.WebApp.Models.McpServers.Instances;
using Core.Infrastructure.WebApp.Models.McpServers.Prompts;
using Core.Infrastructure.WebApp.Models.McpServers.Requests;
using Core.Infrastructure.WebApp.Models.McpServers.Resources;
using Core.Infrastructure.WebApp.Models.McpServers.Tools;

namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for MCP server details.
/// </summary>
public record DetailsResponse(
    string Name,
    string Status,
    string? UpdatedAt,
    ConfigurationResponse? Configuration = null,
    IReadOnlyList<EventResponse>? Events = null,
    IReadOnlyList<RequestResponse>? Requests = null,
    IReadOnlyList<InstanceResponse>? Instances = null,
    IReadOnlyList<ToolResponse>? Tools = null,
    IReadOnlyList<PromptResponse>? Prompts = null,
    IReadOnlyList<ResourceResponse>? Resources = null);
