namespace Core.Infrastructure.Messaging;

/// <summary>
/// Defines the behavior when the event channel is full.
/// </summary>
public enum OverflowPolicy
{
    /// <summary>
    /// Discard oldest events to make room for new ones.
    /// </summary>
    DropOldest,

    /// <summary>
    /// Discard new events when the channel is full.
    /// </summary>
    DropNewest,

    /// <summary>
    /// Block the caller until space is available.
    /// </summary>
    Wait
}
