using System.Text.Json;
using Core.Application.McpServers;
using Core.Application.Requests;
using Core.Domain.McpServers;
using Core.Domain.Requests;
using Core.Domain.Requests.Parameters;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.ModelContextProtocol.Hosting.RequestHandlers;

/// <summary>
/// Handles <c>mcp-server.instance.read-resource</c> requests by reading a resource from an MCP server instance.
/// </summary>
public class McpServerReadResourceHandler : IRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly ILogger<McpServerReadResourceHandler> _logger;

    public McpServerReadResourceHandler(
        IMcpServerInstanceManager instanceManager,
        ILogger<McpServerReadResourceHandler> logger)
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
                [new RequestError("PARAMETERS_REQUIRED", "Parameters are required for read-resource action.")]);
        }

        var parameters = JsonSerializer.Deserialize<ReadResourceParameters>(request.Parameters.Value, JsonOptions);
        if (parameters == null || string.IsNullOrEmpty(parameters.ResourceUri))
        {
            return RequestHandlerResult.Failure(
                [new RequestError("RESOURCE_URI_REQUIRED", "ResourceUri is required for read-resource action.")]);
        }

        var serverName = McpServerName.Create(name);
        if (serverName.IsFailure)
        {
            return RequestHandlerResult.Failure(
                [new RequestError(serverName.Error.Code.Value, serverName.Error.Message)]);
        }

        var mcpInstanceId = McpServerInstanceId.From(instanceId);
        var requestId = request.Id.Value;

        _logger.LogDebug("Reading resource {ResourceUri} from instance {InstanceId} for request {RequestId}",
            parameters.ResourceUri, instanceId, request.Id.Value);

        var result = await _instanceManager.ReadResourceAsync(
            serverName.Value, mcpInstanceId, parameters.ResourceUri, requestId, cancellationToken);

        if (result.IsSuccess)
        {
            var output = JsonSerializer.SerializeToElement(result.Value);
            return RequestHandlerResult.Success(output);
        }

        return RequestHandlerResult.Failure(
            [new RequestError(result.Error.Code.Value, result.Error.Message)]);
    }
}
