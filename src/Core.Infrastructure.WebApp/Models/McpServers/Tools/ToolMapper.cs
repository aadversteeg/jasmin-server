using System.Text.Json;
using Core.Domain.McpServers;

namespace Core.Infrastructure.WebApp.Models.McpServers.Tools;

/// <summary>
/// Mapper for converting between tool domain models and API models.
/// </summary>
public static class ToolMapper
{
    public static ToolResponse ToResponse(McpTool source)
    {
        return new ToolResponse(
            source.Name,
            source.Title,
            source.Description,
            source.InputSchema != null ? JsonSerializer.Deserialize<object>(source.InputSchema) : null);
    }

    public static ToolListResponse ToListResponse(
        McpServerMetadata? metadata,
        TimeZoneInfo timeZone)
    {
        if (metadata == null)
        {
            return new ToolListResponse([], null, null);
        }

        var items = (metadata.Tools ?? [])
            .Select(ToResponse)
            .ToList()
            .AsReadOnly();

        var errors = FilterErrors(metadata.RetrievalErrors, "Tools");
        var retrievedAt = FormatTimestamp(metadata.RetrievedAtUtc, timeZone);

        return new ToolListResponse(items, retrievedAt, errors);
    }

    private static IReadOnlyList<MetadataRetrievalErrorResponse>? FilterErrors(
        IReadOnlyList<McpServerMetadataError>? errors,
        string category)
    {
        if (errors == null) return null;
        var filtered = errors
            .Where(e => e.Category == category)
            .Select(e => new MetadataRetrievalErrorResponse(e.Category, e.ErrorMessage))
            .ToList();
        return filtered.Count > 0 ? filtered.AsReadOnly() : null;
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
