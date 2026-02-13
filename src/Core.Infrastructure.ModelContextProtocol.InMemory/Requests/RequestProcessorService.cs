using System.Collections.Concurrent;
using Core.Application.Requests;
using Core.Domain.Requests;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.ModelContextProtocol.InMemory.Requests;

/// <summary>
/// Background service that processes generic requests from the queue.
/// Requests are processed in parallel across different targets, but serialized per target.
/// </summary>
public class RequestProcessorService : BackgroundService
{
    private readonly IRequestQueue _queue;
    private readonly IRequestStore _store;
    private readonly IRequestHandlerRegistry _registry;
    private readonly ILogger<RequestProcessorService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _targetSemaphores = new();

    public RequestProcessorService(
        IRequestQueue queue,
        IRequestStore store,
        IRequestHandlerRegistry registry,
        ILogger<RequestProcessorService> logger)
    {
        _queue = queue;
        _store = store;
        _registry = registry;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Request processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var request = await _queue.DequeueAsync(stoppingToken);
                _ = ProcessRequestWithTargetSerializationAsync(request, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request processor loop");
            }
        }

        _logger.LogInformation("Request processor stopped");
    }

    private async Task ProcessRequestWithTargetSerializationAsync(Request request, CancellationToken cancellationToken)
    {
        var semaphore = _targetSemaphores.GetOrAdd(request.Target, _ => new SemaphoreSlim(1, 1));

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            await ProcessRequestAsync(request, cancellationToken);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task ProcessRequestAsync(Request request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Processing request {RequestId}, action: {Action}, target: {Target}",
            request.Id.Value, request.Action.Value, request.Target);

        request.MarkRunning();
        _store.Update(request);

        try
        {
            var handler = _registry.GetHandler(request.Action);
            if (handler == null)
            {
                request.MarkFailed([new RequestError("UNKNOWN_ACTION", $"No handler registered for action '{request.Action.Value}'.")]);
                _store.Update(request);
                return;
            }

            var result = await handler.HandleAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                request.MarkCompleted(result.Output);
                _logger.LogInformation("Request {RequestId} completed successfully", request.Id.Value);
            }
            else
            {
                request.MarkFailed(result.Errors!);
                _logger.LogWarning("Request {RequestId} failed: {Errors}",
                    request.Id.Value, string.Join(", ", result.Errors!.Select(e => $"{e.Code}: {e.Message}")));
            }

            _store.Update(request);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            request.MarkFailed([new RequestError("REQUEST_CANCELLED", "Request cancelled due to shutdown.")]);
            _store.Update(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request {RequestId}", request.Id.Value);
            request.MarkFailed([new RequestError(ex.GetType().Name, ex.Message)]);
            _store.Update(request);
        }
    }
}
