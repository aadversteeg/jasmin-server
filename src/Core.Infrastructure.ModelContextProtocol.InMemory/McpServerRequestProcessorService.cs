using System.Text.Json;
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
    private readonly IEventStore _eventStore;
    private readonly ILogger<McpServerRequestProcessorService> _logger;

    public McpServerRequestProcessorService(
        IMcpServerRequestQueue queue,
        IMcpServerRequestStore store,
        IMcpServerInstanceManager instanceManager,
        IEventStore eventStore,
        ILogger<McpServerRequestProcessorService> logger)
    {
        _queue = queue;
        _store = store;
        _instanceManager = instanceManager;
        _eventStore = eventStore;
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
            else if (request.Action == McpServerRequestAction.Stop)
            {
                await ProcessStopRequestAsync(request, cancellationToken);
            }
            else if (request.Action == McpServerRequestAction.InvokeTool)
            {
                await ProcessInvokeToolRequestAsync(request, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            request.MarkFailed(ToRequestErrors("REQUEST_CANCELLED", "Request cancelled due to shutdown"));
            _store.Update(request);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request {RequestId}", request.Id.Value);
            request.MarkFailed(ToRequestErrors(ex));
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
            request.MarkFailed(ToRequestErrors(result.Error));
            _logger.LogWarning("Request {RequestId} failed: {Error}",
                request.Id.Value, result.Error.Message);
        }

        _store.Update(request);
    }

    private async Task ProcessStopRequestAsync(McpServerRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetInstanceId == null)
        {
            request.MarkFailed(ToRequestErrors("INSTANCE_ID_REQUIRED_FOR_STOP", "InstanceId is required for stop action"));
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
            // Record StopFailed event since the instance manager couldn't record it
            // (instance was not found, so it didn't have access to the server)
            var eventErrors = new List<McpServerEventError>
            {
                new(result.Error.Code.Value, result.Error.Message)
            }.AsReadOnly();
            _eventStore.RecordEvent(request.ServerName, McpServerEventType.StopFailed, eventErrors, request.TargetInstanceId, request.Id);

            request.MarkFailed(ToRequestErrors(result.Error));
            _logger.LogWarning("Request {RequestId} failed: {Error}",
                request.Id.Value, result.Error.Message);
        }

        _store.Update(request);
    }

    private async Task ProcessInvokeToolRequestAsync(McpServerRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetInstanceId == null)
        {
            request.MarkFailed(ToRequestErrors("INSTANCE_ID_REQUIRED_FOR_INVOKE_TOOL", "InstanceId is required for invokeTool action"));
            _store.Update(request);
            return;
        }

        if (string.IsNullOrEmpty(request.ToolName))
        {
            request.MarkFailed(ToRequestErrors("TOOL_NAME_REQUIRED", "ToolName is required for invokeTool action"));
            _store.Update(request);
            return;
        }

        // Convert JsonElement input to dictionary for the instance manager
        IReadOnlyDictionary<string, object?>? arguments = null;
        if (request.Input.HasValue)
        {
            arguments = JsonSerializer.Deserialize<Dictionary<string, object?>>(request.Input.Value);
        }

        var result = await _instanceManager.InvokeToolAsync(
            request.ServerName,
            request.TargetInstanceId,
            request.ToolName,
            arguments,
            request.Id,
            cancellationToken);

        if (result.IsSuccess)
        {
            var outputJson = JsonSerializer.SerializeToElement(result.Value);
            request.MarkCompletedWithOutput(outputJson);
            _logger.LogInformation("Request {RequestId} completed: invoked tool {ToolName} on instance {InstanceId}",
                request.Id.Value, request.ToolName, request.TargetInstanceId.Value);
        }
        else
        {
            request.MarkFailed(ToRequestErrors(result.Error));
            _logger.LogWarning("Request {RequestId} failed: {Error}",
                request.Id.Value, result.Error.Message);
        }

        _store.Update(request);
    }

    private static IReadOnlyList<McpServerRequestError> ToRequestErrors(string code, string message)
    {
        return [new McpServerRequestError(code, message)];
    }

    private static IReadOnlyList<McpServerRequestError> ToRequestErrors(Core.Domain.Models.Error error)
    {
        return [new McpServerRequestError(error.Code.Value, error.Message)];
    }

    private static IReadOnlyList<McpServerRequestError> ToRequestErrors(Exception ex)
    {
        return [new McpServerRequestError(ex.GetType().Name, ex.Message)];
    }
}
