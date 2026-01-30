using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerRequestTests
{
    [Fact(DisplayName = "REQ-001: New request should have Pending status")]
    public void REQ001()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;

        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);

        request.Status.Should().Be(McpServerRequestStatus.Pending);
    }

    [Fact(DisplayName = "REQ-002: New request should have CreatedAtUtc set")]
    public void REQ002()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var beforeCreation = DateTime.UtcNow;

        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);

        request.CreatedAtUtc.Should().BeOnOrAfter(beforeCreation);
        request.CreatedAtUtc.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact(DisplayName = "REQ-003: MarkRunning should change status to Running")]
    public void REQ003()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);

        request.MarkRunning();

        request.Status.Should().Be(McpServerRequestStatus.Running);
    }

    [Fact(DisplayName = "REQ-004: MarkCompleted should set status and timestamp")]
    public void REQ004()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);
        var beforeCompletion = DateTime.UtcNow;

        request.MarkCompleted();

        request.Status.Should().Be(McpServerRequestStatus.Completed);
        request.CompletedAtUtc.Should().NotBeNull();
        request.CompletedAtUtc!.Value.Should().BeOnOrAfter(beforeCompletion);
    }

    [Fact(DisplayName = "REQ-005: MarkCompleted with result should set ResultInstanceId")]
    public void REQ005()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var resultInstanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);

        request.MarkCompleted(resultInstanceId);

        request.ResultInstanceId.Should().Be(resultInstanceId);
    }

    [Fact(DisplayName = "REQ-006: MarkFailed should set status, timestamp, and errors")]
    public void REQ006()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);
        var errors = new List<McpServerRequestError>
        {
            new("CONNECTION_ERROR", "Connection failed")
        }.AsReadOnly();

        request.MarkFailed(errors);

        request.Status.Should().Be(McpServerRequestStatus.Failed);
        request.CompletedAtUtc.Should().NotBeNull();
        request.Errors.Should().NotBeNull();
        request.Errors.Should().HaveCount(1);
        request.Errors![0].Code.Should().Be("CONNECTION_ERROR");
        request.Errors[0].Message.Should().Be("Connection failed");
    }

    [Fact(DisplayName = "REQ-007: Stop action should have TargetInstanceId")]
    public void REQ007()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var targetInstanceId = McpServerInstanceId.Create();

        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Stop, targetInstanceId);

        request.Action.Should().Be(McpServerRequestAction.Stop);
        request.TargetInstanceId.Should().Be(targetInstanceId);
    }

    [Fact(DisplayName = "REQ-008: New request should have null optional fields")]
    public void REQ008()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;

        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);

        request.CompletedAtUtc.Should().BeNull();
        request.ResultInstanceId.Should().BeNull();
        request.Errors.Should().BeNull();
        request.TargetInstanceId.Should().BeNull();
    }

    [Fact(DisplayName = "REQ-009: InvokeTool action should have ToolName and Input")]
    public void REQ009()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var targetInstanceId = McpServerInstanceId.Create();
        var input = System.Text.Json.JsonSerializer.SerializeToElement(new { timezoneId = "Europe/Amsterdam" });

        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            targetInstanceId,
            "get_current_date_and_time",
            input);

        request.Action.Should().Be(McpServerRequestAction.InvokeTool);
        request.TargetInstanceId.Should().Be(targetInstanceId);
        request.ToolName.Should().Be("get_current_date_and_time");
        request.Input.Should().NotBeNull();
    }

    [Fact(DisplayName = "REQ-010: MarkCompletedWithOutput should set status, timestamp, and output")]
    public void REQ010()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var targetInstanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            targetInstanceId,
            "get_current_date_and_time");
        var beforeCompletion = DateTime.UtcNow;
        var output = System.Text.Json.JsonSerializer.SerializeToElement(new { result = "2024-01-15T10:30:00+01:00" });

        request.MarkCompletedWithOutput(output);

        request.Status.Should().Be(McpServerRequestStatus.Completed);
        request.CompletedAtUtc.Should().NotBeNull();
        request.CompletedAtUtc!.Value.Should().BeOnOrAfter(beforeCompletion);
        request.Output.Should().NotBeNull();
    }

    [Fact(DisplayName = "REQ-011: New InvokeTool request should have null Output")]
    public void REQ011()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var targetInstanceId = McpServerInstanceId.Create();

        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            targetInstanceId,
            "test-tool");

        request.Output.Should().BeNull();
    }

    [Fact(DisplayName = "REQ-012: InvokeTool action enum value should be 2")]
    public void REQ012()
    {
        ((int)McpServerRequestAction.InvokeTool).Should().Be(2);
    }
}
