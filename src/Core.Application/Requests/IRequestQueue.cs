using Core.Domain.Requests;

namespace Core.Application.Requests;

/// <summary>
/// Queue for requests to be processed by the background processor.
/// </summary>
public interface IRequestQueue
{
    /// <summary>
    /// Enqueues a request for background processing.
    /// </summary>
    /// <param name="request">The request to enqueue.</param>
    void Enqueue(Request request);

    /// <summary>
    /// Dequeues the next request for processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next request to process.</returns>
    ValueTask<Request> DequeueAsync(CancellationToken cancellationToken);
}
