using Core.Application.McpServers;
using Core.Application.Requests;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Requests;
using Microsoft.Extensions.Logging;
using Error = Ave.Extensions.ErrorPaths.Error;

namespace Core.Infrastructure.ModelContextProtocol.Hosting.RequestHandlers;

/// <summary>
/// Handles <c>mcp-server.instance.refresh-metadata</c> requests by refreshing metadata from an MCP server instance.
/// </summary>
public class McpServerRefreshMetadataHandler : IRequestHandler
{
    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly ILogger<McpServerRefreshMetadataHandler> _logger;

    public McpServerRefreshMetadataHandler(
        IMcpServerInstanceManager instanceManager,
        ILogger<McpServerRefreshMetadataHandler> logger)
    {
        _instanceManager = instanceManager;
        _logger = logger;
    }

    public async Task<RequestHandlerResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (!TargetUri.TryParseMcpServerInstance(request.Target, out var name, out var instanceId))
        {
            return RequestHandlerResult.Failure(
                [new Error(ErrorCodes.McpServers.InvalidTarget, $"Target '{request.Target}' is not a valid mcp-server instance target.")]);
        }

        var serverName = McpServerName.Create(name);
        if (serverName.IsFailure)
        {
            return RequestHandlerResult.Failure(
                [serverName.Error]);
        }

        var mcpInstanceId = McpServerInstanceId.From(instanceId);
        var requestId = request.Id.Value;

        _logger.LogDebug("Refreshing metadata for instance {InstanceId} for request {RequestId}",
            instanceId, request.Id.Value);

        var result = await _instanceManager.RefreshMetadataAsync(
            serverName.Value, mcpInstanceId, requestId, cancellationToken);

        if (result.IsSuccess)
        {
            return RequestHandlerResult.Success();
        }

        return RequestHandlerResult.Failure(
            [result.Error]);
    }
}
