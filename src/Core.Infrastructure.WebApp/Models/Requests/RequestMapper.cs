using System.Text.Json;
using Ave.Extensions.Functional;
using Error = Ave.Extensions.ErrorPaths.Error;
using Core.Application.Requests;
using Core.Domain.Models;
using Core.Domain.Paging;
using Core.Domain.Requests;
using Core.Domain.Requests.Parameters;
using Core.Infrastructure.WebApp.Models.Paging;

namespace Core.Infrastructure.WebApp.Models.Requests;

/// <summary>
/// Mapper for converting between generic request domain models and API models.
/// </summary>
public static class RequestMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly Dictionary<string, RequestAction> KnownActions = new()
    {
        ["mcp-server.start"] = RequestActions.McpServer.Start,
        ["mcp-server.instance.stop"] = RequestActions.McpServer.Instance.Stop,
        ["mcp-server.instance.invoke-tool"] = RequestActions.McpServer.Instance.InvokeTool,
        ["mcp-server.instance.get-prompt"] = RequestActions.McpServer.Instance.GetPrompt,
        ["mcp-server.instance.read-resource"] = RequestActions.McpServer.Instance.ReadResource,
        ["mcp-server.instance.refresh-metadata"] = RequestActions.McpServer.Instance.RefreshMetadata,
    };

    /// <summary>
    /// Maps a domain <see cref="Request"/> to a <see cref="RequestResponse"/>.
    /// </summary>
    public static RequestResponse ToResponse(Request source, TimeZoneInfo timeZone)
    {
        var errors = source.Errors?.Select(e => new RequestErrorResponse(e.Code.Value, e.Message)).ToList().AsReadOnly();

        return new RequestResponse(
            source.Id.Value,
            source.Action.Value,
            source.Target,
            source.Status.ToString().ToLowerInvariant(),
            FormatTimestamp(source.CreatedAtUtc, timeZone),
            source.CompletedAtUtc.HasValue ? FormatTimestamp(source.CompletedAtUtc.Value, timeZone) : null,
            source.Parameters,
            source.Output,
            errors);
    }

    /// <summary>
    /// Validates and maps a <see cref="CreateRequestBody"/> to a domain <see cref="Request"/>.
    /// </summary>
    public static Result<Request, Error> ToDomain(CreateRequestBody body)
    {
        if (string.IsNullOrWhiteSpace(body.Action))
        {
            return Result<Request, Error>.Failure(
                Errors.InvalidRequestAction(body.Action ?? string.Empty));
        }

        var actionKey = body.Action.ToLowerInvariant();
        if (!KnownActions.TryGetValue(actionKey, out var action))
        {
            return Result<Request, Error>.Failure(
                Errors.InvalidRequestAction(body.Action));
        }

        if (string.IsNullOrWhiteSpace(body.Target))
        {
            return Result<Request, Error>.Failure(
                new Error(ErrorCodes.McpServers.InvalidTarget, "Target is required."));
        }

        var validationError = ValidateParametersForAction(action, body.Target, body.Parameters);
        if (validationError != null)
        {
            return Result<Request, Error>.Failure(validationError.Value);
        }

        var requestId = RequestId.Create();
        var request = new Request(requestId, action, body.Target, body.Parameters);
        return Result<Request, Error>.Success(request);
    }

    /// <summary>
    /// Maps a paged domain result to a paged API response.
    /// </summary>
    public static PagedResponse<RequestResponse> ToPagedResponse(
        PagedResult<Request> source,
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

    private static Error? ValidateParametersForAction(RequestAction action, string target, JsonElement? parameters)
    {
        if (action == RequestActions.McpServer.Start)
        {
            if (!TargetUri.TryParseMcpServer(target, out _) && !TargetUri.TryParseMcpServerInstance(target, out _, out _))
            {
                return new Error(ErrorCodes.McpServers.InvalidTarget, $"Target '{target}' is not a valid MCP server target. Expected format: mcp-servers/{{name}}");
            }
        }
        else if (action == RequestActions.McpServer.Instance.Stop
            || action == RequestActions.McpServer.Instance.RefreshMetadata)
        {
            if (!TargetUri.TryParseMcpServerInstance(target, out _, out _))
            {
                return new Error(ErrorCodes.McpServers.InvalidTarget, $"Target '{target}' is not a valid MCP server instance target. Expected format: mcp-servers/{{name}}/instances/{{instanceId}}");
            }
        }
        else if (action == RequestActions.McpServer.Instance.InvokeTool)
        {
            if (!TargetUri.TryParseMcpServerInstance(target, out _, out _))
            {
                return new Error(ErrorCodes.McpServers.InvalidTarget, $"Target '{target}' is not a valid MCP server instance target. Expected format: mcp-servers/{{name}}/instances/{{instanceId}}");
            }

            if (!parameters.HasValue)
            {
                return Errors.ToolNameRequired;
            }

            var toolParams = DeserializeParameters<InvokeToolParameters>(parameters.Value);
            if (toolParams == null || string.IsNullOrEmpty(toolParams.ToolName))
            {
                return Errors.ToolNameRequired;
            }
        }
        else if (action == RequestActions.McpServer.Instance.GetPrompt)
        {
            if (!TargetUri.TryParseMcpServerInstance(target, out _, out _))
            {
                return new Error(ErrorCodes.McpServers.InvalidTarget, $"Target '{target}' is not a valid MCP server instance target. Expected format: mcp-servers/{{name}}/instances/{{instanceId}}");
            }

            if (!parameters.HasValue)
            {
                return Errors.PromptNameRequired;
            }

            var promptParams = DeserializeParameters<GetPromptParameters>(parameters.Value);
            if (promptParams == null || string.IsNullOrEmpty(promptParams.PromptName))
            {
                return Errors.PromptNameRequired;
            }
        }
        else if (action == RequestActions.McpServer.Instance.ReadResource)
        {
            if (!TargetUri.TryParseMcpServerInstance(target, out _, out _))
            {
                return new Error(ErrorCodes.McpServers.InvalidTarget, $"Target '{target}' is not a valid MCP server instance target. Expected format: mcp-servers/{{name}}/instances/{{instanceId}}");
            }

            if (!parameters.HasValue)
            {
                return Errors.ResourceUriRequired;
            }

            var resourceParams = DeserializeParameters<ReadResourceParameters>(parameters.Value);
            if (resourceParams == null || string.IsNullOrEmpty(resourceParams.ResourceUri))
            {
                return Errors.ResourceUriRequired;
            }
        }

        return null;
    }

    private static T? DeserializeParameters<T>(JsonElement parameters) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(parameters.GetRawText(), SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
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
