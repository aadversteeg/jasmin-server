using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;
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
        string? updatedOn = null;
        if (source.UpdatedOnUtc.HasValue)
        {
            var utcDateTime = DateTime.SpecifyKind(source.UpdatedOnUtc.Value, DateTimeKind.Utc);

            if (timeZone == TimeZoneInfo.Utc)
            {
                updatedOn = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
            }
            else
            {
                var convertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
                var offset = timeZone.GetUtcOffset(convertedDateTime);
                var dateTimeOffset = new DateTimeOffset(convertedDateTime, offset);
                updatedOn = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
            }
        }

        return new(
            source.Id.Value,
            source.Command,
            source.Status.ToString().ToLowerInvariant(),
            updatedOn);
    }

    public static DetailsResponse ToDetailsResponse(McpServerDefinition source) =>
        new(source.Id.Value, source.Command, source.Args, source.Env);

    public static DetailsResponse ToDetailsResponse(
        McpServerDefinition source,
        IReadOnlyList<McpServerEvent>? events,
        TimeZoneInfo timeZone)
    {
        IReadOnlyList<EventResponse>? eventResponses = null;
        if (events != null)
        {
            eventResponses = events.Select(e => ToEventResponse(e, timeZone)).ToList().AsReadOnly();
        }

        return new(source.Id.Value, source.Command, source.Args, source.Env, eventResponses);
    }

    public static EventResponse ToEventResponse(McpServerEvent source, TimeZoneInfo timeZone)
    {
        var utcDateTime = DateTime.SpecifyKind(source.TimestampUtc, DateTimeKind.Utc);
        string timestamp;

        if (timeZone == TimeZoneInfo.Utc)
        {
            timestamp = utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");
        }
        else
        {
            var convertedDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            var offset = timeZone.GetUtcOffset(convertedDateTime);
            var dateTimeOffset = new DateTimeOffset(convertedDateTime, offset);
            timestamp = dateTimeOffset.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");
        }

        var errors = source.Errors?.Select(e => new EventErrorResponse(e.Code, e.Message)).ToList().AsReadOnly();

        return new(
            source.EventType.ToString(),
            timestamp,
            errors,
            source.InstanceId?.Value,
            source.RequestId?.Value);
    }

    public static Result<McpServerDefinition, Error> ToDomain(CreateRequest request) =>
        McpServerName.Create(request.Name)
            .OnSuccessMap(id => new McpServerDefinition(
                id,
                request.Command,
                (request.Args ?? []).AsReadOnly(),
                (request.Env ?? new Dictionary<string, string>()).AsReadOnly()));

    public static Result<McpServerDefinition, Error> ToDomain(McpServerName id, UpdateRequest request) =>
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
}
