using System.Text.Json;
using Core.Application.Events;
using Core.Domain.Events;
using Core.Domain.Events.Payloads;
using FluentAssertions;
using Xunit;

namespace Tests.Application.Events;

public class EventFactoryTests
{
    [Fact(DisplayName = "EFC-001: Create without payload should return event with null payload")]
    public void EFC001()
    {
        var evt = EventFactory.Create(EventTypes.McpServer.Created, "mcp-servers/my-server");

        evt.Payload.Should().BeNull();
    }

    [Fact(DisplayName = "EFC-002: Create without payload should set type correctly")]
    public void EFC002()
    {
        var evt = EventFactory.Create(EventTypes.McpServer.Instance.Started, "mcp-servers/s/instances/i");

        evt.Type.Should().Be(EventTypes.McpServer.Instance.Started);
    }

    [Fact(DisplayName = "EFC-003: Create without payload should set target correctly")]
    public void EFC003()
    {
        var evt = EventFactory.Create(EventTypes.McpServer.Created, "mcp-servers/my-server");

        evt.Target.Should().Be("mcp-servers/my-server");
    }

    [Fact(DisplayName = "EFC-004: Create without payload should set timestamp to approximately now")]
    public void EFC004()
    {
        var before = DateTime.UtcNow;

        var evt = EventFactory.Create(EventTypes.McpServer.Created, "mcp-servers/my-server");

        evt.TimestampUtc.Should().BeOnOrAfter(before);
        evt.TimestampUtc.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact(DisplayName = "EFC-005: Create without payload should set requestId when provided")]
    public void EFC005()
    {
        var evt = EventFactory.Create(EventTypes.McpServer.Instance.Starting, "mcp-servers/s/instances/i", "req-123");

        evt.RequestId.Should().Be("req-123");
    }

    [Fact(DisplayName = "EFC-006: Create without payload should have null requestId when not provided")]
    public void EFC006()
    {
        var evt = EventFactory.Create(EventTypes.McpServer.Created, "mcp-servers/my-server");

        evt.RequestId.Should().BeNull();
    }

    [Fact(DisplayName = "EFC-007: Create with typed payload should serialize payload to JsonElement")]
    public void EFC007()
    {
        var payload = new ErrorPayload(new List<EventError>
        {
            new("CONN_ERR", "Connection refused")
        }.AsReadOnly());

        var evt = EventFactory.Create(EventTypes.McpServer.Instance.StartFailed, "mcp-servers/s/instances/i", payload);

        evt.Payload.Should().NotBeNull();
        evt.Payload!.Value.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact(DisplayName = "EFC-008: Create with typed payload should produce deserializable JSON")]
    public void EFC008()
    {
        var payload = new ToolInvocationPayload("get_time", null, null);

        var evt = EventFactory.Create(EventTypes.McpServer.ToolInvocation.Invoking, "mcp-servers/s/instances/i", payload);

        var deserialized = JsonSerializer.Deserialize<JsonElement>(evt.Payload!.Value.GetRawText());
        deserialized.GetProperty("toolName").GetString().Should().Be("get_time");
    }

    [Fact(DisplayName = "EFC-009: Create with typed payload should use camelCase property names")]
    public void EFC009()
    {
        var config = new EventConfiguration("node", new List<string> { "server.js" }.AsReadOnly(), new Dictionary<string, string>().AsReadOnly());
        var payload = new InstanceStartedPayload(config);

        var evt = EventFactory.Create(EventTypes.McpServer.Instance.Started, "mcp-servers/s/instances/i", payload);

        var json = evt.Payload!.Value.GetRawText();
        json.Should().Contain("\"configuration\"");
        json.Should().Contain("\"command\"");
        json.Should().NotContain("\"Configuration\"");
        json.Should().NotContain("\"Command\"");
    }

    [Fact(DisplayName = "EFC-010: Create with typed payload should set type and target")]
    public void EFC010()
    {
        var payload = new ErrorPayload(new List<EventError>().AsReadOnly());

        var evt = EventFactory.Create(EventTypes.McpServer.Instance.StopFailed, "mcp-servers/s/instances/i", payload);

        evt.Type.Should().Be(EventTypes.McpServer.Instance.StopFailed);
        evt.Target.Should().Be("mcp-servers/s/instances/i");
    }

    [Fact(DisplayName = "EFC-011: Create with typed payload should pass requestId through")]
    public void EFC011()
    {
        var payload = new ToolInvocationPayload("test-tool", null, null);

        var evt = EventFactory.Create(EventTypes.McpServer.ToolInvocation.Invoking, "mcp-servers/s/instances/i", payload, "req-456");

        evt.RequestId.Should().Be("req-456");
    }

    [Fact(DisplayName = "EFC-012: Create with ConfigurationPayload should serialize both old and new")]
    public void EFC012()
    {
        var oldConfig = new EventConfiguration("node", new List<string> { "old.js" }.AsReadOnly(), new Dictionary<string, string>().AsReadOnly());
        var newConfig = new EventConfiguration("node", new List<string> { "new.js" }.AsReadOnly(), new Dictionary<string, string>().AsReadOnly());
        var payload = new ConfigurationPayload(oldConfig, newConfig);

        var evt = EventFactory.Create(EventTypes.McpServer.Configuration.Updated, "mcp-servers/my-server", payload);

        var json = evt.Payload!.Value.GetRawText();
        json.Should().Contain("\"oldConfiguration\"");
        json.Should().Contain("\"newConfiguration\"");
        json.Should().Contain("old.js");
        json.Should().Contain("new.js");
    }
}
