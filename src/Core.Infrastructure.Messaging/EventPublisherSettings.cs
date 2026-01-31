namespace Core.Infrastructure.Messaging;

/// <summary>
/// Settings for the event publisher.
/// </summary>
public class EventPublisherSettings
{
    /// <summary>
    /// Gets or sets the maximum number of events that can be queued per handler.
    /// Default is 10000.
    /// </summary>
    public int ChannelCapacity { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the policy to apply when the channel is full.
    /// Default is DropOldest.
    /// </summary>
    public OverflowPolicy OverflowPolicy { get; set; } = OverflowPolicy.DropOldest;
}
