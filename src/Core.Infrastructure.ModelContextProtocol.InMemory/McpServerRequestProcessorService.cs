using Core.Application.McpServers;
using Core.Domain.McpServers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Background service that processes MCP server requests sequentially from the queue.
/// </summary>
public class McpServerRequestProcessorService : BackgroundService
{
    private readonly IMcpServerRequestQueue _queue;
    private readonly IMcpServerRequestStore _store;
    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly ILogger<McpServerRequestProcessorService> _logger;

    public McpServerRequestProcessorService(
        IMcpServerRequestQueue queue,
        IMcpServerRequestStore store,
        IMcpServerInstanceManager instanceManager,
        ILogger<McpServerRequestProcessorService> logger)
    {
        _queue = queue;
        _store = store;
        _instanceManager = instanceManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MCP server request processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await _queue.DequeueAsync(stoppingToken);
                await ProcessRequestAsync(request, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request processor loop");
            }
        }

        _logger.LogInformation("MCP server request processor stopped");
    }

    private async Task ProcessRequestAsync(McpServerRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing request {RequestId} for server {ServerName}, action: {Action}",
            request.Id.Value, request.ServerName.Value, request.Action);

        request.MarkRunning();
        _store.Update(request);

        try
        {
            if (request.Action == McpServerRequestAction.Start)
            {
                await ProcessStartRequestAsync(request, cancellationToken);
            }
            else
            {
                await ProcessStopRequestAsync(request, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            request.MarkFailed("Request cancelled due to shutdown");
            _store.Update(request);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request {RequestId}", request.Id.Value);
            request.MarkFailed(ex.Message);
            _store.Update(request);
        }
    }

    private async Task ProcessStartRequestAsync(McpServerRequest request, CancellationToken cancellationToken)
    {
        var result = await _instanceManager.StartInstanceAsync(
            request.ServerName,
            request.Id,
            cancellationToken);

        if (result.IsSuccess)
        {
            request.MarkCompleted(result.Value);
            _logger.LogInformation("Request {RequestId} completed: started instance {InstanceId}",
                request.Id.Value, result.Value.Value);
        }
        else
        {
            request.MarkFailed(result.Error.Message);
            _logger.LogWarning("Request {RequestId} failed: {Error}",
                request.Id.Value, result.Error.Message);
        }

        _store.Update(request);
    }

    private async Task ProcessStopRequestAsync(McpServerRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetInstanceId == null)
        {
            request.MarkFailed("InstanceId is required for stop action");
            _store.Update(request);
            return;
        }

        var result = await _instanceManager.StopInstanceAsync(
            request.TargetInstanceId,
            request.Id,
            cancellationToken);

        if (result.IsSuccess)
        {
            request.MarkCompleted();
            _logger.LogInformation("Request {RequestId} completed: stopped instance {InstanceId}",
                request.Id.Value, request.TargetInstanceId.Value);
        }
        else
        {
            request.MarkFailed(result.Error.Message);
            _logger.LogWarning("Request {RequestId} failed: {Error}",
                request.Id.Value, result.Error.Message);
        }

        _store.Update(request);
    }
}
