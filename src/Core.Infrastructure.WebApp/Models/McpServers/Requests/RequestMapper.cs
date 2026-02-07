using System.Text.Json;
using Ave.Extensions.Functional;
using Core.Application.McpServers;
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

        // Map tool invocation output if present
        ToolInvocationOutputResponse? output = null;
        PromptOutputResponse? promptOutput = null;

        if (source.Output.HasValue)
        {
            if (source.Action == McpServerRequestAction.InvokeTool)
            {
                output = MapToolInvocationOutput(source.Output.Value);
            }
            else if (source.Action == McpServerRequestAction.GetPrompt)
            {
                promptOutput = MapPromptOutput(source.Output.Value);
            }
        }

        return new RequestResponse(
            source.Id.Value,
            source.ServerName.Value,
            source.Action.ToString().ToLowerInvariant(),
            source.Status.ToString().ToLowerInvariant(),
            FormatTimestamp(source.CreatedAtUtc, timeZone),
            source.CompletedAtUtc.HasValue ? FormatTimestamp(source.CompletedAtUtc.Value, timeZone) : null,
            source.TargetInstanceId?.Value,
            source.ResultInstanceId?.Value,
            errors,
            source.ToolName,
            source.Input,
            output,
            source.PromptName,
            source.Arguments,
            promptOutput);
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
        else if (action == McpServerRequestAction.InvokeTool)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                return Result<McpServerRequest, Error>.Failure(
                    Errors.InstanceIdRequiredForInvokeTool);
            }
            if (string.IsNullOrEmpty(request.ToolName))
            {
                return Result<McpServerRequest, Error>.Failure(
                    Errors.ToolNameRequired);
            }
            targetInstanceId = McpServerInstanceId.From(request.InstanceId);
        }
        else if (action == McpServerRequestAction.GetPrompt)
        {
            if (string.IsNullOrEmpty(request.InstanceId))
            {
                return Result<McpServerRequest, Error>.Failure(
                    Errors.InstanceIdRequiredForGetPrompt);
            }
            if (string.IsNullOrEmpty(request.PromptName))
            {
                return Result<McpServerRequest, Error>.Failure(
                    Errors.PromptNameRequired);
            }
            targetInstanceId = McpServerInstanceId.From(request.InstanceId);
        }

        var requestId = McpServerRequestId.Create();
        var domainRequest = new McpServerRequest(
            requestId,
            serverName,
            action,
            targetInstanceId,
            request.ToolName,
            request.Input,
            request.PromptName,
            request.Arguments);

        return Result<McpServerRequest, Error>.Success(domainRequest);
    }

    private static ToolInvocationOutputResponse MapToolInvocationOutput(JsonElement outputJson)
    {
        var result = JsonSerializer.Deserialize<McpToolInvocationResult>(outputJson);
        if (result == null)
        {
            return new ToolInvocationOutputResponse(
                Array.Empty<ToolContentBlockResponse>().AsReadOnly(),
                null,
                false);
        }

        var content = result.Content
            .Select(c => new ToolContentBlockResponse(c.Type, c.Text, c.MimeType, c.Data, c.Uri))
            .ToList()
            .AsReadOnly();

        return new ToolInvocationOutputResponse(content, result.StructuredContent, result.IsError);
    }

    private static PromptOutputResponse MapPromptOutput(JsonElement outputJson)
    {
        var result = JsonSerializer.Deserialize<McpPromptResult>(outputJson);
        if (result == null)
        {
            return new PromptOutputResponse(
                Array.Empty<PromptMessageResponse>().AsReadOnly(),
                null);
        }

        var messages = result.Messages
            .Select(m => new PromptMessageResponse(
                m.Role,
                new PromptMessageContentResponse(
                    m.Content.Type,
                    m.Content.Text,
                    m.Content.MimeType,
                    m.Content.Data,
                    m.Content.Uri)))
            .ToList()
            .AsReadOnly();

        return new PromptOutputResponse(messages, result.Description);
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
