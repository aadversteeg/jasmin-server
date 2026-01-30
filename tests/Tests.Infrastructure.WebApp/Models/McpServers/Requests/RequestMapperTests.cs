using Core.Domain.McpServers;
using Core.Infrastructure.WebApp.Models.McpServers.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.WebApp.Models.McpServers.Requests;

public class RequestMapperTests
{
    [Fact(DisplayName = "RMAP-001: ToResponse should map pending request")]
    public void RMAP001()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.RequestId.Should().Be(requestId.Value);
        result.ServerName.Should().Be("chronos");
        result.Action.Should().Be("start");
        result.Status.Should().Be("pending");
    }

    [Fact(DisplayName = "RMAP-002: ToResponse should map completed start request")]
    public void RMAP002()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var resultInstanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);
        request.MarkCompleted(resultInstanceId);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Status.Should().Be("completed");
        result.ResultInstanceId.Should().Be(resultInstanceId.Value);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact(DisplayName = "RMAP-003: ToResponse should map failed request")]
    public void RMAP003()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);
        request.MarkFailed("Connection timeout");

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Status.Should().Be("failed");
        result.ErrorMessage.Should().Be("Connection timeout");
    }

    [Fact(DisplayName = "RMAP-004: ToResponse should map stop request with target")]
    public void RMAP004()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var targetInstanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Stop, targetInstanceId);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Action.Should().Be("stop");
        result.TargetInstanceId.Should().Be(targetInstanceId.Value);
    }

    [Fact(DisplayName = "RMAP-005: ToDomain should create start request")]
    public void RMAP005()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var createRequest = new CreateRequestRequest("start", null);

        var result = RequestMapper.ToDomain(serverName, createRequest);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(McpServerRequestAction.Start);
        result.Value.ServerName.Should().Be(serverName);
    }

    [Fact(DisplayName = "RMAP-006: ToDomain should create stop request with instance ID")]
    public void RMAP006()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId = "550e8400-e29b-41d4-a716-446655440000";
        var createRequest = new CreateRequestRequest("stop", instanceId);

        var result = RequestMapper.ToDomain(serverName, createRequest);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(McpServerRequestAction.Stop);
        result.Value.TargetInstanceId!.Value.Should().Be(instanceId);
    }

    [Fact(DisplayName = "RMAP-007: ToDomain should fail for invalid action")]
    public void RMAP007()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var createRequest = new CreateRequestRequest("invalid", null);

        var result = RequestMapper.ToDomain(serverName, createRequest);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Invalid request action");
    }

    [Fact(DisplayName = "RMAP-008: ToDomain should fail for stop without instance ID")]
    public void RMAP008()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var createRequest = new CreateRequestRequest("stop", null);

        var result = RequestMapper.ToDomain(serverName, createRequest);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("InstanceId is required");
    }

    [Fact(DisplayName = "RMAP-009: ToResponse should format UTC timestamp with Z suffix")]
    public void RMAP009()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.CreatedAt.Should().EndWith("Z");
    }

    [Fact(DisplayName = "RMAP-010: ToResponse should convert to specified timezone")]
    public void RMAP010()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var request = new McpServerRequest(requestId, serverName, McpServerRequestAction.Start);
        var amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        var result = RequestMapper.ToResponse(request, amsterdamTz);

        result.CreatedAt.Should().Contain("+");
    }
}
