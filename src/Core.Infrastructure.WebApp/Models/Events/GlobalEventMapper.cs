using Core.Domain.McpServers;
using Core.Domain.Paging;
using Core.Infrastructure.WebApp.Models.Paging;

namespace Core.Infrastructure.WebApp.Models.Events;

/// <summary>
/// Mapper for converting between domain models and global event response models.
/// </summary>
public static class GlobalEventMapper
{
    public static GlobalEventResponse ToResponse(GlobalEvent source, TimeZoneInfo timeZone)
    {
        var timestamp = FormatTimestamp(source.TimestampUtc, timeZone);
        return new GlobalEventResponse(
            source.EventType.ToString(),
            source.ServerName.Value,
            timestamp);
    }

    public static PagedResponse<GlobalEventResponse> ToPagedResponse(
        PagedResult<GlobalEvent> source,
        TimeZoneInfo timeZone)
    {
        var items = source.Items.Select(e => ToResponse(e, timeZone)).ToList().AsReadOnly();
        return new PagedResponse<GlobalEventResponse>(
            items,
            source.Page,
            source.PageSize,
            source.TotalItems,
            source.TotalPages);
    }

    private static string FormatTimestamp(DateTime utcTimestamp, TimeZoneInfo timeZone)
    {
        var utcDateTime = DateTime.SpecifyKind(utcTimestamp, DateTimeKind.Utc);

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
