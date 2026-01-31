using Core.Application.Events;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Messaging;

/// <summary>
/// Publisher that fans out events to all registered handler queues.
/// </summary>
/// <typeparam name="T">The type of event to publish.</typeparam>
public class EventPublisher<T> : IEventPublisher<T>
{
    private readonly IReadOnlyList<HandlerRunner<T>> _handlerRunners;
    private readonly EventPublisherSettings _settings;
    private readonly ILogger<EventPublisher<T>> _logger;

    public EventPublisher(
        IEnumerable<HandlerRunner<T>> handlerRunners,
        EventPublisherSettings settings,
        ILogger<EventPublisher<T>> logger)
    {
        _handlerRunners = handlerRunners.ToList().AsReadOnly();
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public void Publish(T @event)
    {
        foreach (var runner in _handlerRunners)
        {
            if (_settings.OverflowPolicy == OverflowPolicy.Wait)
            {
                runner.EnqueueBlocking(@event);
            }
            else
            {
                if (!runner.TryEnqueue(@event))
                {
                    _logger.LogWarning("Event dropped for handler due to full queue: {EventType}",
                        typeof(T).Name);
                }
            }
        }
    }
}
