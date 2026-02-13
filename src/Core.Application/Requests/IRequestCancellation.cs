using Core.Domain.Requests;

namespace Core.Application.Requests;

/// <summary>
/// Provides the ability to cancel pending or running requests.
/// </summary>
public interface IRequestCancellation
{
    /// <summary>
    /// Cancels a request by its identifier.
    /// For pending requests, marks them as cancelled immediately.
    /// For running requests, signals cancellation to the handler.
    /// </summary>
    /// <param name="requestId">The identifier of the request to cancel.</param>
    /// <returns>True if the request was found and cancellation was initiated; false if the request was not found or is already in a terminal state.</returns>
    bool Cancel(RequestId requestId);
}
