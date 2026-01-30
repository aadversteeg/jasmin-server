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

    [Fact(DisplayName = "REQ-006: MarkFailed should set status, timestamp, and error")]
    public void REQ006()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("test-server").Value;
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);
        var errorMessage = "Connection failed";

        request.MarkFailed(errorMessage);

        request.Status.Should().Be(McpServerRequestStatus.Failed);
        request.CompletedAtUtc.Should().NotBeNull();
        request.ErrorMessage.Should().Be(errorMessage);
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
        request.ErrorMessage.Should().BeNull();
        request.TargetInstanceId.Should().BeNull();
    }
}
