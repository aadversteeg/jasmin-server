using Core.Domain.McpServers;

namespace Core.Infrastructure.WebApp.Models.McpServers.Instances;

/// <summary>
/// Mapper for converting between instance log domain models and API models.
/// </summary>
public static class LogMapper
{
    public static LogEntryResponse ToResponse(McpServerInstanceLogEntry source, TimeZoneInfo timeZone)
    {
        return new LogEntryResponse(
            source.LineNumber,
            FormatTimestamp(source.TimestampUtc, timeZone),
            source.Text);
    }

    public static LogListResponse ToListResponse(
        IReadOnlyList<McpServerInstanceLogEntry> entries,
        int totalItems,
        TimeZoneInfo timeZone)
    {
        var items = entries.Select(e => ToResponse(e, timeZone)).ToList().AsReadOnly();
        var lastLineNumber = items.Count > 0 ? items[^1].LineNumber : (long?)null;
        return new LogListResponse(items, totalItems, lastLineNumber);
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
