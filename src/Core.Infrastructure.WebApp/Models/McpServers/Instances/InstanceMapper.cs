using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Infrastructure.WebApp.Models.McpServers.Prompts;
using Core.Infrastructure.WebApp.Models.McpServers.Resources;
using Core.Infrastructure.WebApp.Models.McpServers.Tools;

namespace Core.Infrastructure.WebApp.Models.McpServers.Instances;

/// <summary>
/// Mapper for converting between instance domain models and API models.
/// </summary>
public static class InstanceMapper
{
    public static InstanceResponse ToResponse(McpServerInstanceInfo source, TimeZoneInfo timeZone)
    {
        return ToResponse(source, timeZone, McpServerInstanceIncludeOptions.Default);
    }

    public static InstanceResponse ToResponse(
        McpServerInstanceInfo source,
        TimeZoneInfo timeZone,
        McpServerInstanceIncludeOptions includeOptions)
    {
        IReadOnlyList<ToolResponse>? tools = null;
        IReadOnlyList<PromptResponse>? prompts = null;
        IReadOnlyList<ResourceResponse>? resources = null;

        if (source.Metadata != null)
        {
            if (includeOptions.IncludeTools && source.Metadata.Tools != null)
            {
                tools = source.Metadata.Tools.Select(ToolMapper.ToResponse).ToList().AsReadOnly();
            }

            if (includeOptions.IncludePrompts && source.Metadata.Prompts != null)
            {
                prompts = source.Metadata.Prompts.Select(PromptMapper.ToResponse).ToList().AsReadOnly();
            }

            if (includeOptions.IncludeResources && source.Metadata.Resources != null)
            {
                resources = source.Metadata.Resources.Select(ResourceMapper.ToResponse).ToList().AsReadOnly();
            }
        }

        return new InstanceResponse(
            source.InstanceId.Value,
            source.ServerName.Value,
            FormatTimestamp(source.StartedAtUtc, timeZone),
            ToConfigurationResponse(source.Configuration),
            tools,
            prompts,
            resources);
    }

    public static InstanceListResponse ToListResponse(
        IReadOnlyList<McpServerInstanceInfo> source,
        TimeZoneInfo timeZone)
    {
        var items = source.Select(i => ToResponse(i, timeZone)).ToList().AsReadOnly();
        return new InstanceListResponse(items);
    }

    private static InstanceConfigurationResponse? ToConfigurationResponse(
        McpServerEventConfiguration? source)
    {
        if (source == null)
        {
            return null;
        }

        return new InstanceConfigurationResponse(source.Command, source.Args, source.Env);
    }

    private static string FormatTimestamp(DateTime utcDateTime, TimeZoneInfo timeZone)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        if (timeZone == TimeZoneInfo.Utc)
        {
            return utc.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        var converted = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        var offset = timeZone.GetUtcOffset(converted);
        var dateTimeOffset = new DateTimeOffset(converted, offset);
        return dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
    }
}
