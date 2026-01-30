using Core.Infrastructure.WebApp.Models.McpServers.Prompts;
using Core.Infrastructure.WebApp.Models.McpServers.Resources;
using Core.Infrastructure.WebApp.Models.McpServers.Tools;

namespace Core.Infrastructure.WebApp.Models.McpServers.Instances;

/// <summary>
/// Response model for an MCP server instance.
/// </summary>
/// <param name="InstanceId">The unique identifier for this instance.</param>
/// <param name="ServerName">The name of the server this instance belongs to.</param>
/// <param name="StartedAt">The timestamp when the instance was started.</param>
/// <param name="Configuration">The configuration used to start this instance.</param>
/// <param name="Tools">The tools exposed by this instance (when requested via include).</param>
/// <param name="Prompts">The prompts exposed by this instance (when requested via include).</param>
/// <param name="Resources">The resources exposed by this instance (when requested via include).</param>
public record InstanceResponse(
    string InstanceId,
    string ServerName,
    string StartedAt,
    InstanceConfigurationResponse? Configuration,
    IReadOnlyList<ToolResponse>? Tools = null,
    IReadOnlyList<PromptResponse>? Prompts = null,
    IReadOnlyList<ResourceResponse>? Resources = null);
