using Core.Application.Requests;
using Core.Domain.Requests;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// In-memory registry that maps request actions to their handlers using exact match lookup.
/// </summary>
public class RequestHandlerRegistry : IRequestHandlerRegistry
{
    private readonly Dictionary<RequestAction, IRequestHandler> _handlers = new();

    /// <summary>
    /// Registers a handler for the specified action.
    /// </summary>
    /// <param name="action">The request action.</param>
    /// <param name="handler">The handler to register.</param>
    public void Register(RequestAction action, IRequestHandler handler)
    {
        _handlers[action] = handler;
    }

    /// <inheritdoc />
    public IRequestHandler? GetHandler(RequestAction action)
    {
        return _handlers.TryGetValue(action, out var handler) ? handler : null;
    }
}
