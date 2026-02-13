using System.Text.Json;
using Core.Application.McpServers;
using Core.Application.Requests;
using Core.Domain.McpServers;
using Core.Domain.Requests;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.ModelContextProtocol.Hosting.RequestHandlers;

/// <summary>
/// Handles <c>mcp-server.start</c> requests by starting an MCP server instance.
/// </summary>
public class McpServerStartHandler : IRequestHandler
{
    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly ILogger<McpServerStartHandler> _logger;

    public McpServerStartHandler(
        IMcpServerInstanceManager instanceManager,
        ILogger<McpServerStartHandler> logger)
    {
        _instanceManager = instanceManager;
        _logger = logger;
    }

    public async Task<RequestHandlerResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (!TargetUri.TryParseMcpServer(request.Target, out var name))
        {
            return RequestHandlerResult.Failure(
                [new RequestError("INVALID_TARGET", $"Target '{request.Target}' is not a valid mcp-server target.")]);
        }

        var serverName = McpServerName.Create(name);
        if (serverName.IsFailure)
        {
            return RequestHandlerResult.Failure(
                [new RequestError(serverName.Error.Code.Value, serverName.Error.Message)]);
        }

        var requestId = request.Id.Value;

        _logger.LogDebug("Starting MCP server {ServerName} for request {RequestId}", name, request.Id.Value);

        var result = await _instanceManager.StartInstanceAsync(serverName.Value, requestId, cancellationToken);

        if (result.IsSuccess)
        {
            var output = JsonSerializer.SerializeToElement(new { instanceId = result.Value.Value });
            return RequestHandlerResult.Success(output);
        }

        return RequestHandlerResult.Failure(
            [new RequestError(result.Error.Code.Value, result.Error.Message)]);
    }
}
