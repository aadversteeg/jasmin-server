using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;
using Core.Infrastructure.WebApp.Models.Paging;

namespace Core.Infrastructure.WebApp.Models.McpServers.Requests;

/// <summary>
/// Mapper for converting between request domain models and API models.
/// </summary>
public static class RequestMapper
{
    public static RequestResponse ToResponse(McpServerRequest source, TimeZoneInfo timeZone)
    {
        var errors = source.Errors?.Select(e => new RequestErrorResponse(e.Code, e.Message)).ToList().AsReadOnly();

        return new RequestResponse(
            source.Id.Value,
            source.ServerName.Value,
            source.Action.ToString().ToLowerInvariant(),
            source.Status.ToString().ToLowerInvariant(),
            FormatTimestamp(source.CreatedAtUtc, timeZone),
            source.CompletedAtUtc.HasValue ? FormatTimestamp(source.CompletedAtUtc.Value, timeZone) : null,
            source.TargetInstanceId?.Value,
            source.ResultInstanceId?.Value,
            errors);
    }

    public static Result<McpServerRequest, Error> ToDomain(
        McpServerName serverName,
        CreateRequestRequest request)
    {
        if (!Enum.TryParse<McpServerRequestAction>(request.Action, ignoreCase: true, out var action))
        {
            return Result<McpServerRequest, Error>.Failure(
                Errors.InvalidRequestAction(request.Action));
        }

        McpServerInstanceId? targetInstanceId = null;
        if (action == McpServerRequestAction.Stop)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                return Result<McpServerRequest, Error>.Failure(
                    Errors.InstanceIdRequiredForStop);
            }
            targetInstanceId = McpServerInstanceId.From(request.InstanceId);
        }

        var requestId = McpServerRequestId.Create();
        var domainRequest = new McpServerRequest(requestId, serverName, action, targetInstanceId);

        return Result<McpServerRequest, Error>.Success(domainRequest);
    }

    public static PagedResponse<RequestResponse> ToPagedResponse(
        PagedResult<McpServerRequest> source,
        TimeZoneInfo timeZone)
    {
        var items = source.Items.Select(r => ToResponse(r, timeZone)).ToList().AsReadOnly();
        return new PagedResponse<RequestResponse>(
            items,
            source.Page,
            source.PageSize,
            source.TotalItems,
            source.TotalPages);
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
