namespace Core.Application.Events;

/// <summary>
/// Interface for publishing events to registered handlers.
/// </summary>
/// <typeparam name="T">The type of event to publish.</typeparam>
public interface IEventPublisher<in T>
{
    /// <summary>
    /// Publishes an event to all registered handlers.
    /// This method is non-blocking and returns immediately.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    void Publish(T @event);
}
