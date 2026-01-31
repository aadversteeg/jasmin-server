using Core.Domain.McpServers;
using Core.Infrastructure.Messaging.SSE;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests.Infrastructure.Messaging.SSE;

public class SseClientManagerTests
{
    private readonly Mock<ILogger<SseClientManager>> _loggerMock;
    private readonly SseClientManager _sseClientManager;

    public SseClientManagerTests()
    {
        _loggerMock = new Mock<ILogger<SseClientManager>>();
        _sseClientManager = new SseClientManager(_loggerMock.Object);
    }

    [Fact(DisplayName = "SCM-001: RegisterClient should return unique client ID")]
    public void SCM001()
    {
        var clientId1 = _sseClientManager.RegisterClient();
        var clientId2 = _sseClientManager.RegisterClient();

        clientId1.Should().NotBe(Guid.Empty);
        clientId2.Should().NotBe(Guid.Empty);
        clientId1.Should().NotBe(clientId2);
    }

    [Fact(DisplayName = "SCM-002: RegisterClient should increment client count")]
    public void SCM002()
    {
        _sseClientManager.ClientCount.Should().Be(0);

        _sseClientManager.RegisterClient();
        _sseClientManager.ClientCount.Should().Be(1);

        _sseClientManager.RegisterClient();
        _sseClientManager.ClientCount.Should().Be(2);
    }

    [Fact(DisplayName = "SCM-003: UnregisterClient should decrement client count")]
    public void SCM003()
    {
        var clientId1 = _sseClientManager.RegisterClient();
        var clientId2 = _sseClientManager.RegisterClient();
        _sseClientManager.ClientCount.Should().Be(2);

        _sseClientManager.UnregisterClient(clientId1);
        _sseClientManager.ClientCount.Should().Be(1);

        _sseClientManager.UnregisterClient(clientId2);
        _sseClientManager.ClientCount.Should().Be(0);
    }

    [Fact(DisplayName = "SCM-004: UnregisterClient with unknown ID should not throw")]
    public void SCM004()
    {
        var unknownClientId = Guid.NewGuid();

        var action = () => _sseClientManager.UnregisterClient(unknownClientId);

        action.Should().NotThrow();
    }

    [Fact(DisplayName = "SCM-005: GetEventsAsync with unknown ID should throw")]
    public void SCM005()
    {
        var unknownClientId = Guid.NewGuid();

        var action = () => _sseClientManager.GetEventsAsync(unknownClientId, CancellationToken.None);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage($"Client {unknownClientId} not found");
    }

    [Fact(DisplayName = "SCM-006: GetEventsAsync with valid ID should return async enumerable")]
    public void SCM006()
    {
        var clientId = _sseClientManager.RegisterClient();

        var result = _sseClientManager.GetEventsAsync(clientId, CancellationToken.None);

        result.Should().NotBeNull();
    }

    [Fact(DisplayName = "SCM-007: Broadcast should write to all client channels")]
    public async Task SCM007()
    {
        var clientId1 = _sseClientManager.RegisterClient();
        var clientId2 = _sseClientManager.RegisterClient();
        var serverName = McpServerName.Create("test-server").Value;
        var @event = new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow);

        _sseClientManager.Broadcast(@event);

        using var cts1 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        using var cts2 = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var events1 = new List<McpServerEvent>();
        var events2 = new List<McpServerEvent>();

        try
        {
            await foreach (var e in _sseClientManager.GetEventsAsync(clientId1, cts1.Token))
            {
                events1.Add(e);
                break;
            }
        }
        catch (OperationCanceledException) { }

        try
        {
            await foreach (var e in _sseClientManager.GetEventsAsync(clientId2, cts2.Token))
            {
                events2.Add(e);
                break;
            }
        }
        catch (OperationCanceledException) { }

        events1.Should().HaveCount(1);
        events1[0].ServerName.Should().Be(serverName);
        events2.Should().HaveCount(1);
        events2[0].ServerName.Should().Be(serverName);
    }

    [Fact(DisplayName = "SCM-008: Broadcast with no clients should not throw")]
    public void SCM008()
    {
        var serverName = McpServerName.Create("test-server").Value;
        var @event = new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow);

        var action = () => _sseClientManager.Broadcast(@event);

        action.Should().NotThrow();
    }

    [Fact(DisplayName = "SCM-009: RegisterClient should use custom channel capacity")]
    public void SCM009()
    {
        var clientId = _sseClientManager.RegisterClient(channelCapacity: 5);

        clientId.Should().NotBe(Guid.Empty);
        _sseClientManager.ClientCount.Should().Be(1);
    }

    [Fact(DisplayName = "SCM-010: Client channel should drop oldest when full")]
    public async Task SCM010()
    {
        var clientId = _sseClientManager.RegisterClient(channelCapacity: 2);
        var serverName = McpServerName.Create("test-server").Value;

        // Broadcast 3 events but capacity is only 2
        _sseClientManager.Broadcast(new McpServerEvent(serverName, McpServerEventType.Starting, DateTime.UtcNow));
        _sseClientManager.Broadcast(new McpServerEvent(serverName, McpServerEventType.Started, DateTime.UtcNow));
        _sseClientManager.Broadcast(new McpServerEvent(serverName, McpServerEventType.Stopping, DateTime.UtcNow));

        var events = new List<McpServerEvent>();
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        try
        {
            await foreach (var e in _sseClientManager.GetEventsAsync(clientId, cts.Token))
            {
                events.Add(e);
            }
        }
        catch (OperationCanceledException) { }

        // Should have 2 events (oldest was dropped)
        events.Should().HaveCount(2);
        events[0].EventType.Should().Be(McpServerEventType.Started);
        events[1].EventType.Should().Be(McpServerEventType.Stopping);
    }
}
