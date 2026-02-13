using System.Collections.Concurrent;
using Core.Application.Requests;
using Core.Domain.Models;
using Core.Domain.Requests;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Error = Ave.Extensions.ErrorPaths.Error;
using ErrorCode = Ave.Extensions.ErrorPaths.ErrorCode;

namespace Core.Infrastructure.ModelContextProtocol.InMemory.Requests;

/// <summary>
/// Background service that processes generic requests from the queue.
/// Requests are processed in parallel across different targets, but serialized per target.
/// </summary>
public class RequestProcessorService : BackgroundService, IRequestCancellation
{
    private readonly IRequestQueue _queue;
    private readonly IRequestStore _store;
    private readonly IRequestHandlerRegistry _registry;
    private readonly ILogger<RequestProcessorService> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _targetSemaphores = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _requestCancellationTokens = new();

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

    public bool Cancel(RequestId requestId)
    {
        var maybeRequest = _store.GetById(requestId);
        if (maybeRequest.HasNoValue)
        {
            return false;
        }

        var request = maybeRequest.Value;

        if (request.Status == RequestStatus.Completed ||
            request.Status == RequestStatus.Failed ||
            request.Status == RequestStatus.Cancelled)
        {
            return false;
        }

        if (_requestCancellationTokens.TryGetValue(requestId.Value, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Cancellation requested for running request {RequestId}", requestId.Value);
        }
        else
        {
            request.MarkCancelled();
            _store.Update(request);
            _logger.LogInformation("Pending request {RequestId} marked as cancelled", requestId.Value);
        }

        return true;
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
        var semaphoreKey = request.Target ?? request.Id.Value;
        var semaphore = _targetSemaphores.GetOrAdd(semaphoreKey, _ => new SemaphoreSlim(1, 1));

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
        if (request.Status == RequestStatus.Cancelled)
        {
            _logger.LogDebug("Skipping already cancelled request {RequestId}", request.Id.Value);
            return;
        }

        _logger.LogDebug("Processing request {RequestId}, action: {Action}, target: {Target}",
            request.Id.Value, request.Action.Value, request.Target ?? "(none)");

        using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _requestCancellationTokens[request.Id.Value] = requestCts;

        request.MarkRunning();
        _store.Update(request);

        try
        {
            var handler = _registry.GetHandler(request.Action);
            if (handler == null)
            {
                request.MarkFailed([new Error(ErrorCodes.Request.UnknownAction, $"No handler registered for action '{request.Action.Value}'.")]);
                _store.Update(request);
                return;
            }

            var result = await handler.HandleAsync(request, requestCts.Token);

            if (result.IsSuccess)
            {
                request.MarkCompleted(result.Output);
                _logger.LogInformation("Request {RequestId} completed successfully", request.Id.Value);
            }
            else
            {
                request.MarkFailed(result.Errors!, result.Output);
                _logger.LogWarning("Request {RequestId} failed: {Errors}",
                    request.Id.Value, string.Join(", ", result.Errors!.Select(e => $"{e.Code.Value}: {e.Message}")));
            }

            _store.Update(request);
        }
        catch (OperationCanceledException) when (requestCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            request.MarkCancelled();
            _store.Update(request);
            _logger.LogInformation("Request {RequestId} was cancelled", request.Id.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            request.MarkFailed([new Error(ErrorCodes.Request.Cancelled, "Request cancelled due to shutdown.")]);
            _store.Update(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request {RequestId}", request.Id.Value);
            request.MarkFailed([new Error(new ErrorCode(ex.GetType().Name), ex.Message)]);
            _store.Update(request);
        }
        finally
        {
            _requestCancellationTokens.TryRemove(request.Id.Value, out _);
        }
    }
}
