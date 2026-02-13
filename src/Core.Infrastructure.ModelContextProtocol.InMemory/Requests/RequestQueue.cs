using System.Threading.Channels;
using Core.Application.Requests;
using Core.Domain.Requests;

namespace Core.Infrastructure.ModelContextProtocol.InMemory.Requests;

/// <summary>
/// Channel-based queue for generic requests.
/// </summary>
public class RequestQueue : IRequestQueue
{
    private readonly Channel<Request> _channel =
        Channel.CreateUnbounded<Request>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    /// <inheritdoc />
    public void Enqueue(Request request)
    {
        _channel.Writer.TryWrite(request);
    }

    /// <inheritdoc />
    public ValueTask<Request> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
