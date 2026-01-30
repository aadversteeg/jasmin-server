using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;

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
}
