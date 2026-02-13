using Core.Application.McpServers;
using Core.Application.Requests;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Requests;
using Microsoft.Extensions.Logging;
using Error = Ave.Extensions.ErrorPaths.Error;

namespace Core.Infrastructure.ModelContextProtocol.Hosting.RequestHandlers;

/// <summary>
/// Handles <c>mcp-server.instance.stop</c> requests by stopping an MCP server instance.
/// </summary>
public class McpServerInstanceStopHandler : IRequestHandler
{
    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly ILogger<McpServerInstanceStopHandler> _logger;

    public McpServerInstanceStopHandler(
        IMcpServerInstanceManager instanceManager,
        ILogger<McpServerInstanceStopHandler> logger)
    {
        _instanceManager = instanceManager;
        _logger = logger;
    }

    public async Task<RequestHandlerResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (!TargetUri.TryParseMcpServerInstance(request.Target, out _, out var instanceId))
        {
            return RequestHandlerResult.Failure(
                [new Error(ErrorCodes.McpServers.InvalidTarget, $"Target '{request.Target}' is not a valid mcp-server instance target.")]);
        }

        var mcpInstanceId = McpServerInstanceId.From(instanceId);
        var requestId = request.Id.Value;

        _logger.LogDebug("Stopping MCP server instance {InstanceId} for request {RequestId}", instanceId, request.Id.Value);

        var result = await _instanceManager.StopInstanceAsync(mcpInstanceId, requestId, cancellationToken);

        if (result.IsSuccess)
        {
            return RequestHandlerResult.Success();
        }

        return RequestHandlerResult.Failure(
            [result.Error]);
    }
}
