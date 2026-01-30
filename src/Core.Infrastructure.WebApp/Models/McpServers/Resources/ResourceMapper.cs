using Core.Domain.McpServers;

namespace Core.Infrastructure.WebApp.Models.McpServers.Resources;

/// <summary>
/// Mapper for converting between resource domain models and API models.
/// </summary>
public static class ResourceMapper
{
    public static ResourceResponse ToResponse(McpResource source)
    {
        return new ResourceResponse(
            source.Name,
            source.Uri,
            source.Title,
            source.Description,
            source.MimeType);
    }

    public static ResourceListResponse ToListResponse(
        McpServerMetadata? metadata,
        TimeZoneInfo timeZone)
    {
        if (metadata == null)
        {
            return new ResourceListResponse([], null, null);
        }

        var items = (metadata.Resources ?? [])
            .Select(ToResponse)
            .ToList()
            .AsReadOnly();

        var errors = FilterErrors(metadata.RetrievalErrors, "Resources");
        var retrievedAt = FormatTimestamp(metadata.RetrievedAtUtc, timeZone);

        return new ResourceListResponse(items, retrievedAt, errors);
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
