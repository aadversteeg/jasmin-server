using Core.Domain.Requests;

namespace Core.Application.Requests;

/// <summary>
/// Handles a specific type of request action.
/// </summary>
public interface IRequestHandler
{
    /// <summary>
    /// Handles the request and returns a result.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of handling the request.</returns>
    Task<RequestHandlerResult> HandleAsync(Request request, CancellationToken cancellationToken);
}
