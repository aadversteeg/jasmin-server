using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerEventTests
{
    [Fact(DisplayName = "MSE-001: Event should store event type and timestamp")]
    public void MSE001()
    {
        var timestamp = DateTime.UtcNow;

        var evt = new McpServerEvent(McpServerEventType.Starting, timestamp);

        evt.EventType.Should().Be(McpServerEventType.Starting);
        evt.TimestampUtc.Should().Be(timestamp);
        evt.ErrorMessage.Should().BeNull();
    }

    [Fact(DisplayName = "MSE-002: Event should store error message for failures")]
    public void MSE002()
    {
        var timestamp = DateTime.UtcNow;
        var errorMessage = "Connection refused";

        var evt = new McpServerEvent(McpServerEventType.StartFailed, timestamp, errorMessage);

        evt.EventType.Should().Be(McpServerEventType.StartFailed);
        evt.ErrorMessage.Should().Be(errorMessage);
    }

    [Fact(DisplayName = "MSE-003: All event types should be defined")]
    public void MSE003()
    {
        var eventTypes = Enum.GetValues<McpServerEventType>();

        eventTypes.Should().Contain(McpServerEventType.Starting);
        eventTypes.Should().Contain(McpServerEventType.Started);
        eventTypes.Should().Contain(McpServerEventType.StartFailed);
        eventTypes.Should().Contain(McpServerEventType.Stopping);
        eventTypes.Should().Contain(McpServerEventType.Stopped);
        eventTypes.Should().Contain(McpServerEventType.StopFailed);
    }

    [Fact(DisplayName = "MSE-004: Events with same values should be equal")]
    public void MSE004()
    {
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var evt1 = new McpServerEvent(McpServerEventType.Started, timestamp);
        var evt2 = new McpServerEvent(McpServerEventType.Started, timestamp);

        evt1.Should().Be(evt2);
    }
}
