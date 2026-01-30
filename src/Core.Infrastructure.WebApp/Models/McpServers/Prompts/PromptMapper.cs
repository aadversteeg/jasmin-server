using Core.Domain.McpServers;

namespace Core.Infrastructure.WebApp.Models.McpServers.Prompts;

/// <summary>
/// Mapper for converting between prompt domain models and API models.
/// </summary>
public static class PromptMapper
{
    public static PromptResponse ToResponse(McpPrompt source)
    {
        return new PromptResponse(
            source.Name,
            source.Title,
            source.Description,
            source.Arguments?.Select(a => new PromptArgumentResponse(
                a.Name,
                a.Description,
                a.Required)).ToList().AsReadOnly());
    }

    public static PromptListResponse ToListResponse(
        McpServerMetadata? metadata,
        TimeZoneInfo timeZone)
    {
        if (metadata == null)
        {
            return new PromptListResponse([], null, null);
        }

        var items = (metadata.Prompts ?? [])
            .Select(ToResponse)
            .ToList()
            .AsReadOnly();

        var errors = FilterErrors(metadata.RetrievalErrors, "Prompts");
        var retrievedAt = FormatTimestamp(metadata.RetrievedAtUtc, timeZone);

        return new PromptListResponse(items, retrievedAt, errors);
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
