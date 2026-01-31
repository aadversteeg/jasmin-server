using Core.Application.Events;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Messaging;

/// <summary>
/// A typed wrapper around HandlerRunner that includes the handler type in its generic signature.
/// This ensures each handler gets a distinct service registration in the DI container.
/// </summary>
/// <typeparam name="T">The type of event to handle.</typeparam>
/// <typeparam name="THandler">The handler implementation type.</typeparam>
public class TypedHandlerRunner<T, THandler> : HandlerRunner<T>
    where THandler : class, IEventHandler<T>
{
    public TypedHandlerRunner(
        THandler handler,
        EventPublisherSettings settings,
        ILogger<HandlerRunner<T>> logger)
        : base(handler, settings, logger)
    {
    }
}
