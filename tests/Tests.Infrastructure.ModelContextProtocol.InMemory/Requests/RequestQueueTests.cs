using Core.Domain.Requests;
using Core.Infrastructure.ModelContextProtocol.InMemory.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.ModelContextProtocol.InMemory.Requests;

public class RequestQueueTests
{
    private readonly RequestQueue _queue;

    public RequestQueueTests()
    {
        _queue = new RequestQueue();
    }

    [Fact(DisplayName = "RQU-001: Enqueue and dequeue should preserve FIFO order")]
    public async Task RQU001()
    {
        var request1 = CreateRequest("mcp-servers/first");
        var request2 = CreateRequest("mcp-servers/second");
        var request3 = CreateRequest("mcp-servers/third");

        _queue.Enqueue(request1);
        _queue.Enqueue(request2);
        _queue.Enqueue(request3);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var result1 = await _queue.DequeueAsync(cts.Token);
        var result2 = await _queue.DequeueAsync(cts.Token);
        var result3 = await _queue.DequeueAsync(cts.Token);

        result1.Id.Should().Be(request1.Id);
        result2.Id.Should().Be(request2.Id);
        result3.Id.Should().Be(request3.Id);
    }

    [Fact(DisplayName = "RQU-002: Dequeue should block until item available")]
    public async Task RQU002()
    {
        var dequeueTask = _queue.DequeueAsync(CancellationToken.None);

        dequeueTask.IsCompleted.Should().BeFalse();

        var request = CreateRequest("mcp-servers/chronos");
        _queue.Enqueue(request);

        var result = await dequeueTask;
        result.Id.Should().Be(request.Id);
    }

    [Fact(DisplayName = "RQU-003: Dequeue should throw when cancelled")]
    public async Task RQU003()
    {
        using var cts = new CancellationTokenSource();
        var dequeueTask = _queue.DequeueAsync(cts.Token);

        cts.Cancel();

        var act = async () => await dequeueTask;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static Request CreateRequest(string target)
    {
        return new Request(RequestId.Create(), RequestActions.McpServer.Start, target);
    }
}
