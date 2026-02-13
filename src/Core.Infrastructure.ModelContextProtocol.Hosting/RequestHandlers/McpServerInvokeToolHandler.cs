using System.Text.Json;
using Core.Application.McpServers;
using Core.Application.Requests;
using Core.Domain.McpServers;
using Core.Domain.Requests;
using Core.Domain.Requests.Parameters;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.ModelContextProtocol.Hosting.RequestHandlers;

/// <summary>
/// Handles <c>mcp-server.instance.invoke-tool</c> requests by invoking a tool on an MCP server instance.
/// </summary>
public class McpServerInvokeToolHandler : IRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly ILogger<McpServerInvokeToolHandler> _logger;

    public McpServerInvokeToolHandler(
        IMcpServerInstanceManager instanceManager,
        ILogger<McpServerInvokeToolHandler> logger)
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
                [new RequestError("PARAMETERS_REQUIRED", "Parameters are required for invoke-tool action.")]);
        }

        var parameters = JsonSerializer.Deserialize<InvokeToolParameters>(request.Parameters.Value, JsonOptions);
        if (parameters == null || string.IsNullOrEmpty(parameters.ToolName))
        {
            return RequestHandlerResult.Failure(
                [new RequestError("TOOL_NAME_REQUIRED", "ToolName is required for invoke-tool action.")]);
        }

        var serverName = McpServerName.Create(name);
        if (serverName.IsFailure)
        {
            return RequestHandlerResult.Failure(
                [new RequestError(serverName.Error.Code.Value, serverName.Error.Message)]);
        }

        var mcpInstanceId = McpServerInstanceId.From(instanceId);
        var requestId = request.Id.Value;

        // Convert JsonElement input to dictionary
        IReadOnlyDictionary<string, object?>? arguments = null;
        if (parameters.Input.HasValue)
        {
            arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(parameters.Input.Value);
        }

        _logger.LogDebug("Invoking tool {ToolName} on instance {InstanceId} for request {RequestId}",
            parameters.ToolName, instanceId, request.Id.Value);

        var result = await _instanceManager.InvokeToolAsync(
            serverName.Value, mcpInstanceId, parameters.ToolName, arguments, requestId, cancellationToken);

        if (result.IsSuccess)
        {
            var output = JsonSerializer.SerializeToElement(result.Value);
            return RequestHandlerResult.Success(output);
        }

        return RequestHandlerResult.Failure(
            [new RequestError(result.Error.Code.Value, result.Error.Message)]);
    }
}
