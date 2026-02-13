using System.Text.Json;
using Ave.Extensions.ErrorPaths;
using Core.Domain.Paging;
using Core.Domain.Requests;
using Core.Infrastructure.WebApp.Models.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.WebApp.Models.Requests;

public class RequestMapperTests
{
    [Fact(DisplayName = "GRM-001: ToResponse should map pending request")]
    public void GRM001()
    {
        var requestId = RequestId.Create();
        var action = RequestActions.McpServer.Start;
        var target = "mcp-servers/chronos";
        var request = new Request(requestId, action, target);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Id.Should().Be(requestId.Value);
        result.Action.Should().Be("mcp-server.start");
        result.Target.Should().Be("mcp-servers/chronos");
        result.Status.Should().Be("pending");
        result.Parameters.Should().BeNull();
        result.Output.Should().BeNull();
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "GRM-002: ToResponse should map completed request with output")]
    public void GRM002()
    {
        var requestId = RequestId.Create();
        var action = RequestActions.McpServer.Instance.InvokeTool;
        var target = "mcp-servers/chronos/instances/abc123";
        var parameters = JsonSerializer.SerializeToElement(new { toolName = "get_time", input = (object?)null });
        var request = new Request(requestId, action, target, parameters);

        var output = JsonSerializer.SerializeToElement(new { result = "2024-01-15T10:30:00Z" });
        request.MarkCompleted(output);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Status.Should().Be("completed");
        result.CompletedAt.Should().NotBeNull();
        result.Output.Should().NotBeNull();
        result.Parameters.Should().NotBeNull();
    }

    [Fact(DisplayName = "GRM-003: ToResponse should map failed request with errors")]
    public void GRM003()
    {
        var requestId = RequestId.Create();
        var action = RequestActions.McpServer.Start;
        var target = "mcp-servers/chronos";
        var request = new Request(requestId, action, target);

        var errors = new List<Error>
        {
            new(new ErrorCode("TIMEOUT_ERROR"), "Connection timeout")
        }.AsReadOnly();
        request.MarkFailed(errors);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Status.Should().Be("failed");
        result.CompletedAt.Should().NotBeNull();
        result.Errors.Should().NotBeNull();
        result.Errors.Should().HaveCount(1);
        result.Errors![0].Code.Should().Be("TIMEOUT_ERROR");
        result.Errors[0].Message.Should().Be("Connection timeout");
    }

    [Fact(DisplayName = "GRM-004: ToResponse should format UTC timestamp with Z suffix")]
    public void GRM004()
    {
        var request = new Request(RequestId.Create(), RequestActions.McpServer.Start, "mcp-servers/chronos");

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.CreatedAt.Should().EndWith("Z");
    }

    [Fact(DisplayName = "GRM-005: ToResponse should convert to specified timezone")]
    public void GRM005()
    {
        var request = new Request(RequestId.Create(), RequestActions.McpServer.Start, "mcp-servers/chronos");
        var amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        var result = RequestMapper.ToResponse(request, amsterdamTz);

        result.CreatedAt.Should().Contain("+");
    }

    [Fact(DisplayName = "GRM-006: ToDomain should create start request")]
    public void GRM006()
    {
        var body = new CreateRequestBody("mcp-server.start", "mcp-servers/chronos");

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(RequestActions.McpServer.Start);
        result.Value.Target.Should().Be("mcp-servers/chronos");
    }

    [Fact(DisplayName = "GRM-007: ToDomain should fail for unknown action")]
    public void GRM007()
    {
        var body = new CreateRequestBody("unknown.action", "mcp-servers/chronos");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Invalid request action");
    }

    [Fact(DisplayName = "GRM-008: ToDomain should fail for empty action")]
    public void GRM008()
    {
        var body = new CreateRequestBody("", "mcp-servers/chronos");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "GRM-009: ToDomain should fail for start with empty target")]
    public void GRM009()
    {
        var body = new CreateRequestBody("mcp-server.start", "");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("not a valid MCP server target");
    }

    [Fact(DisplayName = "GRM-010: ToDomain should create invoke-tool request with parameters")]
    public void GRM010()
    {
        var parameters = JsonSerializer.SerializeToElement(new { toolName = "get_time", input = new { timezone = "UTC" } });
        var body = new CreateRequestBody(
            "mcp-server.instance.invoke-tool",
            "mcp-servers/chronos/instances/abc123",
            parameters);

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(RequestActions.McpServer.Instance.InvokeTool);
        result.Value.Target.Should().Be("mcp-servers/chronos/instances/abc123");
        result.Value.Parameters.Should().NotBeNull();
    }

    [Fact(DisplayName = "GRM-011: ToDomain should fail for invoke-tool without parameters")]
    public void GRM011()
    {
        var body = new CreateRequestBody(
            "mcp-server.instance.invoke-tool",
            "mcp-servers/chronos/instances/abc123");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("ToolName is required");
    }

    [Fact(DisplayName = "GRM-012: ToDomain should fail for invoke-tool without toolName in parameters")]
    public void GRM012()
    {
        var parameters = JsonSerializer.SerializeToElement(new { input = new { timezone = "UTC" } });
        var body = new CreateRequestBody(
            "mcp-server.instance.invoke-tool",
            "mcp-servers/chronos/instances/abc123",
            parameters);

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("ToolName is required");
    }

    [Fact(DisplayName = "GRM-013: ToDomain should fail for invoke-tool with invalid target")]
    public void GRM013()
    {
        var parameters = JsonSerializer.SerializeToElement(new { toolName = "get_time" });
        var body = new CreateRequestBody(
            "mcp-server.instance.invoke-tool",
            "mcp-servers/chronos",
            parameters);

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("not a valid MCP server instance target");
    }

    [Fact(DisplayName = "GRM-014: ToDomain should create get-prompt request")]
    public void GRM014()
    {
        var parameters = JsonSerializer.SerializeToElement(new { promptName = "greeting", arguments = new { name = "World" } });
        var body = new CreateRequestBody(
            "mcp-server.instance.get-prompt",
            "mcp-servers/chronos/instances/abc123",
            parameters);

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(RequestActions.McpServer.Instance.GetPrompt);
    }

    [Fact(DisplayName = "GRM-015: ToDomain should fail for get-prompt without parameters")]
    public void GRM015()
    {
        var body = new CreateRequestBody(
            "mcp-server.instance.get-prompt",
            "mcp-servers/chronos/instances/abc123");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("PromptName is required");
    }

    [Fact(DisplayName = "GRM-016: ToDomain should create read-resource request")]
    public void GRM016()
    {
        var parameters = JsonSerializer.SerializeToElement(new { resourceUri = "demo://resource/test" });
        var body = new CreateRequestBody(
            "mcp-server.instance.read-resource",
            "mcp-servers/everything/instances/abc123",
            parameters);

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(RequestActions.McpServer.Instance.ReadResource);
    }

    [Fact(DisplayName = "GRM-017: ToDomain should fail for read-resource without parameters")]
    public void GRM017()
    {
        var body = new CreateRequestBody(
            "mcp-server.instance.read-resource",
            "mcp-servers/everything/instances/abc123");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("ResourceUri is required");
    }

    [Fact(DisplayName = "GRM-018: ToDomain should create stop request")]
    public void GRM018()
    {
        var body = new CreateRequestBody(
            "mcp-server.instance.stop",
            "mcp-servers/chronos/instances/abc123");

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(RequestActions.McpServer.Instance.Stop);
    }

    [Fact(DisplayName = "GRM-019: ToDomain should fail for stop with invalid target")]
    public void GRM019()
    {
        var body = new CreateRequestBody(
            "mcp-server.instance.stop",
            "mcp-servers/chronos");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("not a valid MCP server instance target");
    }

    [Fact(DisplayName = "GRM-020: ToDomain should create refresh-metadata request")]
    public void GRM020()
    {
        var body = new CreateRequestBody(
            "mcp-server.instance.refresh-metadata",
            "mcp-servers/chronos/instances/abc123");

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(RequestActions.McpServer.Instance.RefreshMetadata);
    }

    [Fact(DisplayName = "GRM-021: ToDomain should be case-insensitive for action")]
    public void GRM021()
    {
        var body = new CreateRequestBody("MCP-SERVER.START", "mcp-servers/chronos");

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(RequestActions.McpServer.Start);
    }

    [Fact(DisplayName = "GRM-022: ToPagedResponse should map paged requests correctly")]
    public void GRM022()
    {
        var requests = new List<Request>
        {
            new(RequestId.Create(), RequestActions.McpServer.Start, "mcp-servers/chronos"),
            new(RequestId.Create(), RequestActions.McpServer.Instance.Stop, "mcp-servers/chronos/instances/abc123")
        };
        var pagedResult = new PagedResult<Request>(requests, 2, 10, 25);

        var result = RequestMapper.ToPagedResponse(pagedResult, TimeZoneInfo.Utc);

        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(25);
        result.TotalPages.Should().Be(3);
    }

    [Fact(DisplayName = "GRM-023: ToPagedResponse should handle empty paged result")]
    public void GRM023()
    {
        var pagedResult = new PagedResult<Request>([], 1, 10, 0);

        var result = RequestMapper.ToPagedResponse(pagedResult, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact(DisplayName = "GRM-024: ToResponse should map running request")]
    public void GRM024()
    {
        var request = new Request(RequestId.Create(), RequestActions.McpServer.Start, "mcp-servers/chronos");
        request.MarkRunning();

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Status.Should().Be("running");
        result.CompletedAt.Should().BeNull();
    }

    [Fact(DisplayName = "GRM-025: ToResponse should map completed request without output")]
    public void GRM025()
    {
        var request = new Request(RequestId.Create(), RequestActions.McpServer.Start, "mcp-servers/chronos");
        request.MarkCompleted();

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Status.Should().Be("completed");
        result.CompletedAt.Should().NotBeNull();
        result.Output.Should().BeNull();
    }

    [Fact(DisplayName = "GRM-026: ToDomain should fail for start with invalid target")]
    public void GRM026()
    {
        var body = new CreateRequestBody("mcp-server.start", "invalid/target/format/too/many");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("not a valid MCP server target");
    }

    [Fact(DisplayName = "GRM-027: ToDomain should preserve parameters as JsonElement")]
    public void GRM027()
    {
        var parameters = JsonSerializer.SerializeToElement(new { toolName = "test_tool", input = new { key = "value" } });
        var body = new CreateRequestBody(
            "mcp-server.instance.invoke-tool",
            "mcp-servers/chronos/instances/abc123",
            parameters);

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Parameters.Should().NotBeNull();
        result.Value.Parameters!.Value.GetRawText().Should().Contain("test_tool");
    }

    [Fact(DisplayName = "GRM-028: ToDomain should create test-configuration request without target")]
    public void GRM028()
    {
        var parameters = JsonSerializer.SerializeToElement(new { command = "npx", args = new[] { "-y", "server" } });
        var body = new CreateRequestBody("mcp-server.test-configuration", null, parameters);

        var result = RequestMapper.ToDomain(body);

        result.IsSuccess.Should().BeTrue();
        result.Value.Action.Should().Be(RequestActions.McpServer.TestConfiguration);
        result.Value.Target.Should().BeNull();
    }

    [Fact(DisplayName = "GRM-029: ToDomain should fail for test-configuration without parameters")]
    public void GRM029()
    {
        var body = new CreateRequestBody("mcp-server.test-configuration");

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Command is required");
    }

    [Fact(DisplayName = "GRM-030: ToDomain should fail for test-configuration with empty command")]
    public void GRM030()
    {
        var parameters = JsonSerializer.SerializeToElement(new { command = "" });
        var body = new CreateRequestBody("mcp-server.test-configuration", null, parameters);

        var result = RequestMapper.ToDomain(body);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Command is required");
    }

    [Fact(DisplayName = "GRM-031: ToResponse should map null target")]
    public void GRM031()
    {
        var request = new Request(RequestId.Create(), RequestActions.McpServer.TestConfiguration);

        var result = RequestMapper.ToResponse(request, TimeZoneInfo.Utc);

        result.Target.Should().BeNull();
        result.Action.Should().Be("mcp-server.test-configuration");
    }
}
