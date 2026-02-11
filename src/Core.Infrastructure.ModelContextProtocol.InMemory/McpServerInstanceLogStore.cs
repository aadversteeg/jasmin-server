using System.Collections.Concurrent;
using System.Threading.Channels;
using Core.Application.McpServers;
using Core.Domain.McpServers;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// In-memory implementation of the instance log store.
/// Captures stderr output per instance with thread-safe append, pagination, and live subscriptions.
/// </summary>
public class McpServerInstanceLogStore : IMcpServerInstanceLogStore
{
    private readonly ConcurrentDictionary<string, InstanceLogData> _instances = new();

    /// <inheritdoc />
    public void Append(McpServerInstanceId instanceId, string text)
    {
        var data = _instances.GetOrAdd(instanceId.Value, _ => new InstanceLogData());
        var entry = data.Append(text);
        data.BroadcastToSubscribers(entry);
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerInstanceLogEntry> GetEntries(
        McpServerInstanceId instanceId,
        long afterLine = 0,
        int limit = 100)
    {
        if (!_instances.TryGetValue(instanceId.Value, out var data))
        {
            return [];
        }

        return data.GetEntries(afterLine, limit);
    }

    /// <inheritdoc />
    public int GetCount(McpServerInstanceId instanceId)
    {
        if (!_instances.TryGetValue(instanceId.Value, out var data))
        {
            return 0;
        }

        return data.Count;
    }

    /// <inheritdoc />
    public void Remove(McpServerInstanceId instanceId)
    {
        if (_instances.TryRemove(instanceId.Value, out var data))
        {
            data.CompleteAllSubscribers();
        }
    }

    /// <inheritdoc />
    public (Guid subscriptionId, ChannelReader<McpServerInstanceLogEntry> reader) Subscribe(
        McpServerInstanceId instanceId,
        int channelCapacity = 1000)
    {
        var data = _instances.GetOrAdd(instanceId.Value, _ => new InstanceLogData());
        return data.Subscribe(channelCapacity);
    }

    /// <inheritdoc />
    public void Unsubscribe(McpServerInstanceId instanceId, Guid subscriptionId)
    {
        if (_instances.TryGetValue(instanceId.Value, out var data))
        {
            data.Unsubscribe(subscriptionId);
        }
    }

    private sealed class InstanceLogData
    {
        private readonly List<McpServerInstanceLogEntry> _entries = new();
        private readonly ReaderWriterLockSlim _lock = new();
        private readonly ConcurrentDictionary<Guid, Channel<McpServerInstanceLogEntry>> _subscribers = new();
        private long _nextLineNumber;

        public McpServerInstanceLogEntry Append(string text)
        {
            var lineNumber = Interlocked.Increment(ref _nextLineNumber);
            var entry = new McpServerInstanceLogEntry(lineNumber, DateTime.UtcNow, text);

            _lock.EnterWriteLock();
            try
            {
                _entries.Add(entry);
            }
            finally
            {
                _lock.ExitWriteLock();
            }

            return entry;
        }

        public IReadOnlyList<McpServerInstanceLogEntry> GetEntries(long afterLine, int limit)
        {
            _lock.EnterReadLock();
            try
            {
                // Entries are ordered by line number (1-based, sequential).
                // afterLine=0 means from the start, afterLine=N means skip first N entries.
                var startIndex = (int)Math.Min(afterLine, _entries.Count);
                var count = Math.Min(limit, _entries.Count - startIndex);

                if (count <= 0)
                {
                    return [];
                }

                return _entries.GetRange(startIndex, count).AsReadOnly();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _entries.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        public (Guid subscriptionId, ChannelReader<McpServerInstanceLogEntry> reader) Subscribe(int channelCapacity)
        {
            var subscriptionId = Guid.NewGuid();
            var channel = Channel.CreateBounded<McpServerInstanceLogEntry>(
                new BoundedChannelOptions(channelCapacity)
                {
                    FullMode = BoundedChannelFullMode.DropOldest,
                    SingleReader = true,
                    SingleWriter = false
                });

            _subscribers[subscriptionId] = channel;
            return (subscriptionId, channel.Reader);
        }

        public void Unsubscribe(Guid subscriptionId)
        {
            if (_subscribers.TryRemove(subscriptionId, out var channel))
            {
                channel.Writer.TryComplete();
            }
        }

        public void BroadcastToSubscribers(McpServerInstanceLogEntry entry)
        {
            foreach (var (_, channel) in _subscribers)
            {
                channel.Writer.TryWrite(entry);
            }
        }

        public void CompleteAllSubscribers()
        {
            foreach (var (_, channel) in _subscribers)
            {
                channel.Writer.TryComplete();
            }

            _subscribers.Clear();
        }
    }
}
