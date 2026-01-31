using Core.Domain.McpServers;
using Core.Infrastructure.Messaging.SSE;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Infrastructure.Messaging.SSE;

public class SseEventHandlerTests
{
    private readonly Mock<ILogger<SseClientManager>> _loggerMock;
    private readonly SseClientManager _clientManager;
    private readonly SseEventHandler _handler;

    public SseEventHandlerTests()
    {
        _loggerMock = new Mock<ILogger<SseClientManager>>();
        _clientManager = new SseClientManager(_loggerMock.Object);
        _handler = new SseEventHandler(_clientManager);
    }

    [Fact(DisplayName = "SEH-001: HandleAsync should broadcast event to client manager")]
    public async Task SEH001()
    {
        var clientId = _clientManager.RegisterClient();
        var serverName = McpServerName.Create("test-server").Value;
        var @event = new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow);

        await _handler.HandleAsync(@event, CancellationToken.None);

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var events = new List<McpServerEvent>();
        try
        {
            await foreach (var e in _clientManager.GetEventsAsync(clientId, cts.Token))
            {
                events.Add(e);
                break;
            }
        }
        catch (OperationCanceledException) { }

        events.Should().HaveCount(1);
        events[0].Should().Be(@event);
    }

    [Fact(DisplayName = "SEH-002: HandleAsync should complete successfully without clients")]
    public async Task SEH002()
    {
        var serverName = McpServerName.Create("test-server").Value;
        var @event = new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow);

        var action = async () => await _handler.HandleAsync(@event, CancellationToken.None);

        await action.Should().NotThrowAsync();
    }

    [Fact(DisplayName = "SEH-003: HandleAsync should broadcast to multiple clients")]
    public async Task SEH003()
    {
        var clientId1 = _clientManager.RegisterClient();
        var clientId2 = _clientManager.RegisterClient();
        var serverName = McpServerName.Create("test-server").Value;
        var @event = new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow);

        await _handler.HandleAsync(@event, CancellationToken.None);

        using var cts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var events1 = new List<McpServerEvent>();
        var events2 = new List<McpServerEvent>();

        try
        {
            await foreach (var e in _clientManager.GetEventsAsync(clientId1, cts1.Token))
            {
                events1.Add(e);
                break;
            }
        }
        catch (OperationCanceledException) { }

        try
        {
            await foreach (var e in _clientManager.GetEventsAsync(clientId2, cts2.Token))
            {
                events2.Add(e);
                break;
            }
        }
        catch (OperationCanceledException) { }

        events1.Should().HaveCount(1);
        events2.Should().HaveCount(1);
        events1[0].Should().Be(@event);
        events2[0].Should().Be(@event);
    }

    [Fact(DisplayName = "SEH-004: HandleAsync should return completed task")]
    public async Task SEH004()
    {
        var serverName = McpServerName.Create("test-server").Value;
        var @event = new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow);

        var task = _handler.HandleAsync(@event, CancellationToken.None);

        task.IsCompleted.Should().BeTrue();
        await task;
    }
}
