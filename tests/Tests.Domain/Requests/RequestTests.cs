using System.Text.Json;
using Core.Domain.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Requests;

public class RequestTests
{
    [Fact(DisplayName = "GRQ-001: New request should have Pending status")]
    public void GRQ001()
    {
        var request = CreateRequest();

        request.Status.Should().Be(RequestStatus.Pending);
    }

    [Fact(DisplayName = "GRQ-002: New request should have CreatedAtUtc set")]
    public void GRQ002()
    {
        var before = DateTime.UtcNow;

        var request = CreateRequest();

        request.CreatedAtUtc.Should().BeOnOrAfter(before);
        request.CreatedAtUtc.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact(DisplayName = "GRQ-003: New request should have null optional fields")]
    public void GRQ003()
    {
        var request = CreateRequest();

        request.CompletedAtUtc.Should().BeNull();
        request.Output.Should().BeNull();
        request.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "GRQ-004: New request should store action and target")]
    public void GRQ004()
    {
        var action = RequestActions.McpServer.Instance.InvokeTool;
        var target = "mcp-servers/my-server/instances/abc-123";
        var request = new Request(RequestId.Create(), action, target);

        request.Action.Should().Be(action);
        request.Target.Should().Be(target);
    }

    [Fact(DisplayName = "GRQ-005: New request should store parameters")]
    public void GRQ005()
    {
        var parameters = JsonSerializer.SerializeToElement(new { toolName = "get_time" });
        var request = new Request(RequestId.Create(), RequestActions.McpServer.Instance.InvokeTool, "mcp-servers/s/instances/i", parameters);

        request.Parameters.Should().NotBeNull();
    }

    [Fact(DisplayName = "GRQ-006: New request without parameters should have null parameters")]
    public void GRQ006()
    {
        var request = CreateRequest();

        request.Parameters.Should().BeNull();
    }

    [Fact(DisplayName = "GRQ-007: MarkRunning should change status to Running")]
    public void GRQ007()
    {
        var request = CreateRequest();

        request.MarkRunning();

        request.Status.Should().Be(RequestStatus.Running);
    }

    [Fact(DisplayName = "GRQ-008: MarkCompleted without output should set status and timestamp")]
    public void GRQ008()
    {
        var request = CreateRequest();
        var before = DateTime.UtcNow;

        request.MarkCompleted();

        request.Status.Should().Be(RequestStatus.Completed);
        request.CompletedAtUtc.Should().NotBeNull();
        request.CompletedAtUtc!.Value.Should().BeOnOrAfter(before);
        request.Output.Should().BeNull();
    }

    [Fact(DisplayName = "GRQ-009: MarkCompleted with output should set status, timestamp, and output")]
    public void GRQ009()
    {
        var request = CreateRequest();
        var output = JsonSerializer.SerializeToElement(new { result = "success" });

        request.MarkCompleted(output);

        request.Status.Should().Be(RequestStatus.Completed);
        request.CompletedAtUtc.Should().NotBeNull();
        request.Output.Should().NotBeNull();
    }

    [Fact(DisplayName = "GRQ-010: MarkFailed should set status, timestamp, and errors")]
    public void GRQ010()
    {
        var request = CreateRequest();
        var errors = new List<RequestError>
        {
            new("CONN_ERR", "Connection failed"),
            new("TIMEOUT", "Operation timed out")
        }.AsReadOnly();
        var before = DateTime.UtcNow;

        request.MarkFailed(errors);

        request.Status.Should().Be(RequestStatus.Failed);
        request.CompletedAtUtc.Should().NotBeNull();
        request.CompletedAtUtc!.Value.Should().BeOnOrAfter(before);
        request.Errors.Should().NotBeNull();
        request.Errors.Should().HaveCount(2);
        request.Errors![0].Code.Should().Be("CONN_ERR");
        request.Errors[1].Message.Should().Be("Operation timed out");
    }

    [Fact(DisplayName = "GRQ-011: MarkFailed should not set output")]
    public void GRQ011()
    {
        var request = CreateRequest();

        request.MarkFailed(new List<RequestError> { new("ERR", "Error") }.AsReadOnly());

        request.Output.Should().BeNull();
    }

    [Fact(DisplayName = "GRQ-012: RequestId Create should generate unique IDs")]
    public void GRQ012()
    {
        var id1 = RequestId.Create();
        var id2 = RequestId.Create();

        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact(DisplayName = "GRQ-013: RequestId From should preserve value")]
    public void GRQ013()
    {
        var id = RequestId.From("test-id-123");

        id.Value.Should().Be("test-id-123");
        id.ToString().Should().Be("test-id-123");
    }

    private static Request CreateRequest()
    {
        return new Request(RequestId.Create(), RequestActions.McpServer.Start, "mcp-servers/test-server");
    }
}
