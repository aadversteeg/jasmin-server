using System.Text.Json;
using Core.Application.McpServers;
using Core.Application.Requests;
using Core.Domain.McpServers;
using Core.Domain.Requests;
using Core.Domain.Requests.Parameters;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.ModelContextProtocol.Hosting.RequestHandlers;

/// <summary>
/// Handles <c>mcp-server.instance.get-prompt</c> requests by getting a prompt from an MCP server instance.
/// </summary>
public class McpServerGetPromptHandler : IRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly ILogger<McpServerGetPromptHandler> _logger;

    public McpServerGetPromptHandler(
        IMcpServerInstanceManager instanceManager,
        ILogger<McpServerGetPromptHandler> logger)
    {
        _instanceManager = instanceManager;
        _logger = logger;
    }

    public async Task<RequestHandlerResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (!TargetUri.TryParseMcpServerInstance(request.Target, out var name, out var instanceId))
        {
            return RequestHandlerResult.Failure(
                [new RequestError("INVALID_TARGET", $"Target '{request.Target}' is not a valid mcp-server instance target.")]);
        }

        if (!request.Parameters.HasValue)
        {
            return RequestHandlerResult.Failure(
                [new RequestError("PARAMETERS_REQUIRED", "Parameters are required for get-prompt action.")]);
        }

        var parameters = JsonSerializer.Deserialize<GetPromptParameters>(request.Parameters.Value, JsonOptions);
        if (parameters == null || string.IsNullOrEmpty(parameters.PromptName))
        {
            return RequestHandlerResult.Failure(
                [new RequestError("PROMPT_NAME_REQUIRED", "PromptName is required for get-prompt action.")]);
        }

        var serverName = McpServerName.Create(name);
        if (serverName.IsFailure)
        {
            return RequestHandlerResult.Failure(
                [new RequestError(serverName.Error.Code.Value, serverName.Error.Message)]);
        }

        var mcpInstanceId = McpServerInstanceId.From(instanceId);
        var requestId = request.Id.Value;

        // Convert JsonElement arguments to dictionary
        IReadOnlyDictionary<string, string?>? arguments = null;
        if (parameters.Arguments.HasValue)
        {
            arguments = JsonSerializer.Deserialize<Dictionary<string, string?>>(parameters.Arguments.Value);
        }

        _logger.LogDebug("Getting prompt {PromptName} from instance {InstanceId} for request {RequestId}",
            parameters.PromptName, instanceId, request.Id.Value);

        var result = await _instanceManager.GetPromptAsync(
            serverName.Value, mcpInstanceId, parameters.PromptName, arguments, requestId, cancellationToken);

        if (result.IsSuccess)
        {
            var output = JsonSerializer.SerializeToElement(result.Value);
            return RequestHandlerResult.Success(output);
        }

        return RequestHandlerResult.Failure(
            [new RequestError(result.Error.Code.Value, result.Error.Message)]);
    }
}
