using System.Text.Json;
using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerEventTests
{
    private static readonly McpServerName TestServerName = McpServerName.Create("test-server").Value;

    [Fact(DisplayName = "MSE-001: Event should store server name, event type and timestamp")]
    public void MSE001()
    {
        var timestamp = DateTime.UtcNow;

        var evt = new McpServerEvent(TestServerName, McpServerEventType.Starting, timestamp);

        evt.ServerName.Should().Be(TestServerName);
        evt.EventType.Should().Be(McpServerEventType.Starting);
        evt.TimestampUtc.Should().Be(timestamp);
        evt.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "MSE-002: Event should store errors for failures")]
    public void MSE002()
    {
        var timestamp = DateTime.UtcNow;
        var errors = new List<McpServerEventError>
        {
            new("ConnectionError", "Connection refused")
        }.AsReadOnly();

        var evt = new McpServerEvent(TestServerName, McpServerEventType.StartFailed, timestamp, errors);

        evt.EventType.Should().Be(McpServerEventType.StartFailed);
        evt.Errors.Should().NotBeNull();
        evt.Errors.Should().HaveCount(1);
        evt.Errors![0].Code.Should().Be("ConnectionError");
        evt.Errors[0].Message.Should().Be("Connection refused");
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
        eventTypes.Should().Contain(McpServerEventType.ServerCreated);
        eventTypes.Should().Contain(McpServerEventType.ServerDeleted);
    }

    [Fact(DisplayName = "MSE-004: Events with same values should be equal")]
    public void MSE004()
    {
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);

        var evt1 = new McpServerEvent(TestServerName, McpServerEventType.Started, timestamp);
        var evt2 = new McpServerEvent(TestServerName, McpServerEventType.Started, timestamp);

        evt1.Should().Be(evt2);
    }

    [Fact(DisplayName = "MSE-005: Event should store instance ID")]
    public void MSE005()
    {
        var timestamp = DateTime.UtcNow;
        var instanceId = McpServerInstanceId.Create();

        var evt = new McpServerEvent(TestServerName, McpServerEventType.Starting, timestamp, null, instanceId);

        evt.InstanceId.Should().Be(instanceId);
    }

    [Fact(DisplayName = "MSE-006: Event without instance ID should have null InstanceId")]
    public void MSE006()
    {
        var timestamp = DateTime.UtcNow;

        var evt = new McpServerEvent(TestServerName, McpServerEventType.Starting, timestamp);

        evt.InstanceId.Should().BeNull();
    }

    [Fact(DisplayName = "MSE-007: Events with different instance IDs should not be equal")]
    public void MSE007()
    {
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var instanceId1 = McpServerInstanceId.Create();
        var instanceId2 = McpServerInstanceId.Create();

        var evt1 = new McpServerEvent(TestServerName, McpServerEventType.Started, timestamp, null, instanceId1);
        var evt2 = new McpServerEvent(TestServerName, McpServerEventType.Started, timestamp, null, instanceId2);

        evt1.Should().NotBe(evt2);
    }

    [Fact(DisplayName = "MSE-008: Event should store request ID")]
    public void MSE008()
    {
        var timestamp = DateTime.UtcNow;
        var requestId = McpServerRequestId.Create();

        var evt = new McpServerEvent(TestServerName, McpServerEventType.Starting, timestamp, null, null, requestId);

        evt.RequestId.Should().Be(requestId);
    }

    [Fact(DisplayName = "MSE-009: Event without request ID should have null RequestId")]
    public void MSE009()
    {
        var timestamp = DateTime.UtcNow;

        var evt = new McpServerEvent(TestServerName, McpServerEventType.Starting, timestamp);

        evt.RequestId.Should().BeNull();
    }

    [Fact(DisplayName = "MSE-010: Events with different request IDs should not be equal")]
    public void MSE010()
    {
        var timestamp = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var requestId1 = McpServerRequestId.Create();
        var requestId2 = McpServerRequestId.Create();

        var evt1 = new McpServerEvent(TestServerName, McpServerEventType.Started, timestamp, null, null, requestId1);
        var evt2 = new McpServerEvent(TestServerName, McpServerEventType.Started, timestamp, null, null, requestId2);

        evt1.Should().NotBe(evt2);
    }

    [Fact(DisplayName = "MSE-011: Metadata retrieval event types should be defined")]
    public void MSE011()
    {
        var eventTypes = Enum.GetValues<McpServerEventType>();

        eventTypes.Should().Contain(McpServerEventType.ToolsRetrieving);
        eventTypes.Should().Contain(McpServerEventType.ToolsRetrieved);
        eventTypes.Should().Contain(McpServerEventType.ToolsRetrievalFailed);
        eventTypes.Should().Contain(McpServerEventType.PromptsRetrieving);
        eventTypes.Should().Contain(McpServerEventType.PromptsRetrieved);
        eventTypes.Should().Contain(McpServerEventType.PromptsRetrievalFailed);
        eventTypes.Should().Contain(McpServerEventType.ResourcesRetrieving);
        eventTypes.Should().Contain(McpServerEventType.ResourcesRetrieved);
        eventTypes.Should().Contain(McpServerEventType.ResourcesRetrievalFailed);
    }

    [Fact(DisplayName = "MSE-012: Metadata retrieval event types should have correct values")]
    public void MSE012()
    {
        ((int)McpServerEventType.ToolsRetrieving).Should().Be(9);
        ((int)McpServerEventType.ToolsRetrieved).Should().Be(10);
        ((int)McpServerEventType.ToolsRetrievalFailed).Should().Be(11);
        ((int)McpServerEventType.PromptsRetrieving).Should().Be(12);
        ((int)McpServerEventType.PromptsRetrieved).Should().Be(13);
        ((int)McpServerEventType.PromptsRetrievalFailed).Should().Be(14);
        ((int)McpServerEventType.ResourcesRetrieving).Should().Be(15);
        ((int)McpServerEventType.ResourcesRetrieved).Should().Be(16);
        ((int)McpServerEventType.ResourcesRetrievalFailed).Should().Be(17);
    }

    [Fact(DisplayName = "MSE-017: Tool invocation event types should be defined")]
    public void MSE017()
    {
        var eventTypes = Enum.GetValues<McpServerEventType>();

        eventTypes.Should().Contain(McpServerEventType.ToolInvocationAccepted);
        eventTypes.Should().Contain(McpServerEventType.ToolInvoking);
        eventTypes.Should().Contain(McpServerEventType.ToolInvoked);
        eventTypes.Should().Contain(McpServerEventType.ToolInvocationFailed);
    }

    [Fact(DisplayName = "MSE-018: Tool invocation event types should have correct values")]
    public void MSE018()
    {
        ((int)McpServerEventType.ToolInvocationAccepted).Should().Be(18);
        ((int)McpServerEventType.ToolInvoking).Should().Be(19);
        ((int)McpServerEventType.ToolInvoked).Should().Be(20);
        ((int)McpServerEventType.ToolInvocationFailed).Should().Be(21);
    }

    [Fact(DisplayName = "MSE-019: ServerCreated and ServerDeleted event types should have correct values")]
    public void MSE019()
    {
        ((int)McpServerEventType.ServerCreated).Should().Be(22);
        ((int)McpServerEventType.ServerDeleted).Should().Be(23);
    }

    [Fact(DisplayName = "MSE-013: Event should store tool invocation data")]
    public void MSE013()
    {
        var timestamp = DateTime.UtcNow;
        var input = JsonSerializer.SerializeToElement(new { timezoneId = "Europe/Amsterdam" });
        var toolInvocationData = new McpServerToolInvocationEventData("get_time", input, null);

        var evt = new McpServerEvent(
            TestServerName,
            McpServerEventType.ToolInvoking,
            timestamp,
            ToolInvocationData: toolInvocationData);

        evt.ToolInvocationData.Should().NotBeNull();
        evt.ToolInvocationData!.ToolName.Should().Be("get_time");
        evt.ToolInvocationData.Input.Should().NotBeNull();
        evt.ToolInvocationData.Output.Should().BeNull();
    }

    [Fact(DisplayName = "MSE-014: Event without tool invocation data should have null ToolInvocationData")]
    public void MSE014()
    {
        var timestamp = DateTime.UtcNow;

        var evt = new McpServerEvent(TestServerName, McpServerEventType.Starting, timestamp);

        evt.ToolInvocationData.Should().BeNull();
    }

    [Fact(DisplayName = "MSE-015: Tool invocation completed event should store output")]
    public void MSE015()
    {
        var timestamp = DateTime.UtcNow;
        var input = JsonSerializer.SerializeToElement(new { param = "value" });
        var output = JsonSerializer.SerializeToElement(new { result = "success" });
        var toolInvocationData = new McpServerToolInvocationEventData("test_tool", input, output);

        var evt = new McpServerEvent(
            TestServerName,
            McpServerEventType.ToolInvoked,
            timestamp,
            ToolInvocationData: toolInvocationData);

        evt.ToolInvocationData.Should().NotBeNull();
        evt.ToolInvocationData!.Output.Should().NotBeNull();
    }

    [Fact(DisplayName = "MSE-016: McpServerToolInvocationEventData record should store all properties")]
    public void MSE016()
    {
        var input = JsonSerializer.SerializeToElement(new { arg1 = "value1" });
        var output = JsonSerializer.SerializeToElement(new { result = "done" });

        var data = new McpServerToolInvocationEventData("my_tool", input, output);

        data.ToolName.Should().Be("my_tool");
        data.Input.Should().NotBeNull();
        data.Output.Should().NotBeNull();
    }
}
