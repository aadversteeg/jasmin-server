using Core.Domain.Requests;

namespace Core.Application.Requests;

/// <summary>
/// Registry that maps request actions to their handlers.
/// </summary>
public interface IRequestHandlerRegistry
{
    /// <summary>
    /// Gets the handler for the specified action.
    /// </summary>
    /// <param name="action">The request action.</param>
    /// <returns>The handler, or null if no handler is registered for the action.</returns>
    IRequestHandler? GetHandler(RequestAction action);
}
