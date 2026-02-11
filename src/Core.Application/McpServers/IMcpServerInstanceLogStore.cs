using System.Threading.Channels;
using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Per-instance log store for stderr output from MCP server instances.
/// </summary>
public interface IMcpServerInstanceLogStore
{
    /// <summary>
    /// Appends a line of stderr output for the specified instance.
    /// Thread-safe. Called from the StandardErrorLines callback.
    /// </summary>
    void Append(McpServerInstanceId instanceId, string text);

    /// <summary>
    /// Gets log entries for an instance with cursor-based pagination.
    /// Returns entries with LineNumber greater than afterLine, up to limit entries.
    /// </summary>
    /// <param name="instanceId">The instance ID.</param>
    /// <param name="afterLine">Return lines after this line number (0 means from the start).</param>
    /// <param name="limit">Maximum number of entries to return.</param>
    /// <returns>Log entries in ascending line number order.</returns>
    IReadOnlyList<McpServerInstanceLogEntry> GetEntries(
        McpServerInstanceId instanceId,
        long afterLine = 0,
        int limit = 100);

    /// <summary>
    /// Gets the total number of log entries for an instance.
    /// </summary>
    int GetCount(McpServerInstanceId instanceId);

    /// <summary>
    /// Removes all log entries for the specified instance.
    /// Completes all subscriber channels for this instance.
    /// Called when an instance is stopped.
    /// </summary>
    void Remove(McpServerInstanceId instanceId);

    /// <summary>
    /// Creates a subscription for new log entries for a specific instance.
    /// Returns a subscription ID and a channel reader for consuming new entries.
    /// The caller must call Unsubscribe when done.
    /// </summary>
    /// <param name="instanceId">The instance ID to subscribe to.</param>
    /// <param name="channelCapacity">Bounded channel capacity. Oldest entries are dropped when full.</param>
    /// <returns>A subscription ID and channel reader.</returns>
    (Guid subscriptionId, ChannelReader<McpServerInstanceLogEntry> reader) Subscribe(
        McpServerInstanceId instanceId,
        int channelCapacity = 1000);

    /// <summary>
    /// Removes a subscription created by Subscribe.
    /// </summary>
    void Unsubscribe(McpServerInstanceId instanceId, Guid subscriptionId);
}
