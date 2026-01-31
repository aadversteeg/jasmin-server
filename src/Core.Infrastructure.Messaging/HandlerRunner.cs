using System.Threading.Channels;
using Core.Application.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.Messaging;

/// <summary>
/// Wraps an IEventHandler with its own channel and background processing loop.
/// Each handler processes events sequentially from its own queue.
/// </summary>
/// <typeparam name="T">The type of event to handle.</typeparam>
public class HandlerRunner<T> : BackgroundService
{
    private readonly Channel<T> _channel;
    private readonly IEventHandler<T> _handler;
    private readonly ILogger _logger;

    public HandlerRunner(
        IEventHandler<T> handler,
        EventPublisherSettings settings,
        ILogger logger)
    {
        _handler = handler;
        _logger = logger;

        var fullMode = settings.OverflowPolicy switch
        {
            OverflowPolicy.DropNewest => BoundedChannelFullMode.DropWrite,
            OverflowPolicy.Wait => BoundedChannelFullMode.Wait,
            _ => BoundedChannelFullMode.DropOldest
        };

        _channel = Channel.CreateBounded<T>(new BoundedChannelOptions(settings.ChannelCapacity)
        {
            FullMode = fullMode,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <summary>
    /// Attempts to enqueue an event without blocking.
    /// </summary>
    /// <param name="event">The event to enqueue.</param>
    /// <returns>True if the event was enqueued; false if the channel is full.</returns>
    public bool TryEnqueue(T @event) => _channel.Writer.TryWrite(@event);

    /// <summary>
    /// Enqueues an event, blocking until space is available.
    /// </summary>
    /// <param name="event">The event to enqueue.</param>
    public void EnqueueBlocking(T @event) =>
        _channel.Writer.WriteAsync(@event).AsTask().GetAwaiter().GetResult();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var @event in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _handler.HandleAsync(@event, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler {Handler} failed for event {EventType}",
                    _handler.GetType().Name, typeof(T).Name);
            }
        }
    }
}
