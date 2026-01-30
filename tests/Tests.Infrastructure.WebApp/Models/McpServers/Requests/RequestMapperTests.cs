using System.Text.Json;
using Core.Domain.McpServers;
using Core.Domain.Paging;
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
        var errors = new List<McpServerRequestError>
        {
            new("TIMEOUT_ERROR", "Connection timeout")
        }.AsReadOnly();
        request.MarkFailed(errors);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Status.Should().Be("failed");
        result.Errors.Should().NotBeNull();
        result.Errors.Should().HaveCount(1);
        result.Errors![0].Code.Should().Be("TIMEOUT_ERROR");
        result.Errors[0].Message.Should().Be("Connection timeout");
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

    [Fact(DisplayName = "RMAP-011: ToPagedResponse should map paged requests correctly")]
    public void RMAP011()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var requests = new List<McpServerRequest>
        {
            new(McpServerRequestId.Create(), serverName, McpServerRequestAction.Start),
            new(McpServerRequestId.Create(), serverName, McpServerRequestAction.Stop, McpServerInstanceId.Create())
        };
        var pagedResult = new PagedResult<McpServerRequest>(requests, 2, 10, 25);

        var result = RequestMapper.ToPagedResponse(pagedResult, TimeZoneInfo.Utc);

        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Fact(DisplayName = "RMAP-012: ToPagedResponse should map request details correctly")]
    public void RMAP012()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var requests = new List<McpServerRequest>
        {
            new(McpServerRequestId.Create(), serverName, McpServerRequestAction.Start)
        };
        var pagedResult = new PagedResult<McpServerRequest>(requests, 1, 10, 1);

        var result = RequestMapper.ToPagedResponse(pagedResult, TimeZoneInfo.Utc);

        result.Items[0].ServerName.Should().Be("chronos");
        result.Items[0].Action.Should().Be("start");
        result.Items[0].Status.Should().Be("pending");
    }

    [Fact(DisplayName = "RMAP-013: ToPagedResponse should handle empty paged result")]
    public void RMAP013()
    {
        var pagedResult = new PagedResult<McpServerRequest>([], 1, 10, 0);

        var result = RequestMapper.ToPagedResponse(pagedResult, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact(DisplayName = "RMAP-014: ToDomain should create InvokeTool request with all fields")]
    public void RMAP014()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId = "550e8400-e29b-41d4-a716-446655440000";
        var input = JsonSerializer.SerializeToElement(new { timezoneId = "Europe/Amsterdam" });
        var createRequest = new CreateRequestRequest("invokeTool", instanceId, "get_current_date_and_time", input);

        var result = RequestMapper.ToDomain(serverName, createRequest);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(McpServerRequestAction.InvokeTool);
        result.Value.TargetInstanceId!.Value.Should().Be(instanceId);
        result.Value.ToolName.Should().Be("get_current_date_and_time");
        result.Value.Input.Should().NotBeNull();
    }

    [Fact(DisplayName = "RMAP-015: ToDomain should fail for InvokeTool without instance ID")]
    public void RMAP015()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var createRequest = new CreateRequestRequest("invokeTool", null, "get_current_date_and_time");

        var result = RequestMapper.ToDomain(serverName, createRequest);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("InstanceId is required");
    }

    [Fact(DisplayName = "RMAP-016: ToDomain should fail for InvokeTool without tool name")]
    public void RMAP016()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId = "550e8400-e29b-41d4-a716-446655440000";
        var createRequest = new CreateRequestRequest("invokeTool", instanceId, null);

        var result = RequestMapper.ToDomain(serverName, createRequest);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("ToolName is required");
    }

    [Fact(DisplayName = "RMAP-017: ToResponse should map InvokeTool request with tool name and input")]
    public void RMAP017()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var targetInstanceId = McpServerInstanceId.Create();
        var input = JsonSerializer.SerializeToElement(new { timezoneId = "Europe/Amsterdam" });
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            targetInstanceId,
            "get_current_date_and_time",
            input);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Action.Should().Be("invoketool");
        result.TargetInstanceId.Should().Be(targetInstanceId.Value);
        result.ToolName.Should().Be("get_current_date_and_time");
        result.Input.Should().NotBeNull();
    }

    [Fact(DisplayName = "RMAP-018: ToResponse should map completed InvokeTool request with output")]
    public void RMAP018()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var targetInstanceId = McpServerInstanceId.Create();
        var input = JsonSerializer.SerializeToElement(new { timezoneId = "Europe/Amsterdam" });
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            targetInstanceId,
            "get_current_date_and_time",
            input);

        var outputContent = new
        {
            Content = new[]
            {
                new { Type = "text", Text = "Current time: 2024-01-15T10:30:00+01:00" }
            },
            IsError = false
        };
        var output = JsonSerializer.SerializeToElement(outputContent);
        request.MarkCompletedWithOutput(output);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Status.Should().Be("completed");
        result.Output.Should().NotBeNull();
        result.Output!.Content.Should().HaveCount(1);
        result.Output.Content[0].Type.Should().Be("text");
        result.Output.IsError.Should().BeFalse();
    }

    [Fact(DisplayName = "RMAP-019: ToResponse should handle InvokeTool request without output")]
    public void RMAP019()
    {
        var requestId = McpServerRequestId.Create();
        var serverName = McpServerName.Create("chronos").Value;
        var targetInstanceId = McpServerInstanceId.Create();
        var request = new McpServerRequest(
            requestId,
            serverName,
            McpServerRequestAction.InvokeTool,
            targetInstanceId,
            "get_current_date_and_time");

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Output.Should().BeNull();
    }

    [Fact(DisplayName = "RMAP-020: ToDomain should accept InvokeTool with case-insensitive action")]
    public void RMAP020()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId = "550e8400-e29b-41d4-a716-446655440000";
        var createRequest = new CreateRequestRequest("INVOKETOOL", instanceId, "test_tool");

        var result = RequestMapper.ToDomain(serverName, createRequest);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(McpServerRequestAction.InvokeTool);
    }
}
