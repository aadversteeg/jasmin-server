using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;
using Core.Infrastructure.WebApp.Models.McpServers.Requests;
using Core.Infrastructure.WebApp.Models.Paging;

using McpServerEvent = Core.Domain.McpServers.McpServerEvent;

namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Mapper for converting between domain models and request/response models.
/// </summary>
public static class Mapper
{
    public static ListResponse ToListResponse(McpServerInfo source, TimeZoneInfo timeZone)
    {
        var updatedOn = FormatTimestamp(source.UpdatedOnUtc, timeZone);
        return new(
            source.Id.Value,
            source.Status.ToString().ToLowerInvariant(),
            updatedOn);
    }

    public static ConfigurationResponse? ToConfigurationResponse(McpServerDefinition source)
    {
        if (!source.HasConfiguration)
        {
            return null;
        }

        return new ConfigurationResponse(source.Command!, source.Args!, source.Env!);
    }

    public static DetailsResponse ToDetailsResponse(
        McpServerName serverName,
        McpServerStatusCacheEntry statusEntry,
        TimeZoneInfo timeZone,
        McpServerDefinition? definition = null,
        IReadOnlyList<McpServerEvent>? events = null,
        IReadOnlyList<McpServerRequest>? requests = null)
    {
        var updatedOn = FormatTimestamp(statusEntry.UpdatedOnUtc, timeZone);

        ConfigurationResponse? configuration = null;
        if (definition != null && definition.HasConfiguration)
        {
            configuration = ToConfigurationResponse(definition);
        }

        IReadOnlyList<EventResponse>? eventResponses = null;
        if (events != null)
        {
            eventResponses = events.Select(e => ToEventResponse(e, timeZone)).ToList().AsReadOnly();
        }

        IReadOnlyList<RequestResponse>? requestResponses = null;
        if (requests != null)
        {
            requestResponses = requests.Select(r => RequestMapper.ToResponse(r, timeZone)).ToList().AsReadOnly();
        }

        return new(
            serverName.Value,
            statusEntry.Status.ToString().ToLowerInvariant(),
            updatedOn,
            configuration,
            eventResponses,
            requestResponses);
    }

    public static DetailsResponse ToDetailsResponseAfterCreate(
        McpServerDefinition definition,
        TimeZoneInfo timeZone)
    {
        var configuration = definition.HasConfiguration ? ToConfigurationResponse(definition) : null;
        return new(
            definition.Id.Value,
            McpServerConnectionStatus.Unknown.ToString().ToLowerInvariant(),
            null,
            configuration);
    }

    public static EventResponse ToEventResponse(McpServerEvent source, TimeZoneInfo timeZone)
    {
        var timestamp = FormatTimestamp(source.TimestampUtc, timeZone)!;
        var errors = source.Errors?.Select(e => new EventErrorResponse(e.Code, e.Message)).ToList().AsReadOnly();
        var oldConfig = ToEventConfigurationResponse(source.OldConfiguration);
        var newConfig = ToEventConfigurationResponse(source.NewConfiguration);

        return new(
            source.EventType.ToString(),
            timestamp,
            errors,
            source.InstanceId?.Value,
            source.RequestId?.Value,
            oldConfig,
            newConfig);
    }

    private static EventConfigurationResponse? ToEventConfigurationResponse(
        Core.Domain.McpServers.McpServerEventConfiguration? source)
    {
        if (source == null)
        {
            return null;
        }

        return new EventConfigurationResponse(source.Command, source.Args, source.Env);
    }

    public static Result<McpServerDefinition, Error> ToDomain(CreateRequest request)
    {
        return McpServerName.Create(request.Name)
            .OnSuccessMap(id =>
            {
                if (request.Configuration == null)
                {
                    return new McpServerDefinition(id);
                }

                return new McpServerDefinition(
                    id,
                    request.Configuration.Command,
                    (request.Configuration.Args ?? []).AsReadOnly(),
                    (request.Configuration.Env ?? new Dictionary<string, string>()).AsReadOnly());
            });
    }

    public static Result<McpServerDefinition, Error> ToDomain(McpServerName id, ConfigurationRequest request) =>
        Result<McpServerDefinition, Error>.Success(new McpServerDefinition(
            id,
            request.Command,
            (request.Args ?? []).AsReadOnly(),
            (request.Env ?? new Dictionary<string, string>()).AsReadOnly()));

    public static PagedResponse<EventResponse> ToPagedResponse(
        PagedResult<McpServerEvent> source,
        TimeZoneInfo timeZone)
    {
        var items = source.Items.Select(e => ToEventResponse(e, timeZone)).ToList().AsReadOnly();
        return new PagedResponse<EventResponse>(
            items,
            source.Page,
            source.PageSize,
            source.TotalItems,
            source.TotalPages);
    }

    private static string? FormatTimestamp(DateTime? utcTimestamp, TimeZoneInfo timeZone)
    {
        if (!utcTimestamp.HasValue)
        {
            return null;
        }

        var utcDateTime = DateTime.SpecifyKind(utcTimestamp.Value, DateTimeKind.Utc);

        if (timeZone == TimeZoneInfo.Utc)
        {
            return utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }

        var convertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        var offset = timeZone.GetUtcOffset(convertedDateTime);
        var dateTimeOffset = new DateTimeOffset(convertedDateTime, offset);
        return dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
    }
}
