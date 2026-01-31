namespace Core.Application.Events;

/// <summary>
/// Interface for handling events.
/// </summary>
/// <typeparam name="T">The type of event to handle.</typeparam>
public interface IEventHandler<in T>
{
    /// <summary>
    /// Handles an event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HandleAsync(T @event, CancellationToken cancellationToken);
}
