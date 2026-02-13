using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.Events;
using Core.Domain.McpServers;
using Core.Domain.Paging;
using Core.Infrastructure.WebApp.Models.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.WebApp.Models.McpServers;

public class MapperTests
{
    [Fact(DisplayName = "MAP-001: ToEventResponse should map event type")]
    public void MAP001()
    {
        var evt = new Event(
            EventTypes.McpServer.Instance.Starting,
            "mcp-servers/test-server",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));

        var result = Mapper.ToEventResponse(evt, TimeZoneInfo.Utc);

        result.EventType.Should().Be(EventTypes.McpServer.Instance.Starting.Value);
    }

    [Fact(DisplayName = "MAP-002: ToEventResponse should format UTC timestamp with Z suffix")]
    public void MAP002()
    {
        var evt = new Event(
            EventTypes.McpServer.Instance.Started,
            "mcp-servers/test-server",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));

        var result = Mapper.ToEventResponse(evt, TimeZoneInfo.Utc);

        result.Timestamp.Should().EndWith("Z");
        result.Timestamp.Should().StartWith("2024-01-15T10:30:00");
    }

    [Fact(DisplayName = "MAP-003: ToEventResponse should convert to specified timezone")]
    public void MAP003()
    {
        var evt = new Event(
            EventTypes.McpServer.Instance.Started,
            "mcp-servers/test-server",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        var amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        var result = Mapper.ToEventResponse(evt, amsterdamTz);

        // In January, Amsterdam is UTC+1
        result.Timestamp.Should().StartWith("2024-01-15T11:30:00");
        result.Timestamp.Should().EndWith("+01:00");
    }

    [Fact(DisplayName = "MAP-004: ToEventResponse should include payload when present")]
    public void MAP004()
    {
        var payload = System.Text.Json.JsonSerializer.SerializeToElement(new { code = "ConnectionError", message = "Connection refused" });
        var evt = new Event(
            EventTypes.McpServer.Instance.StartFailed,
            "mcp-servers/test-server",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            payload);

        var result = Mapper.ToEventResponse(evt, TimeZoneInfo.Utc);

        result.Payload.Should().NotBeNull();
    }

    [Fact(DisplayName = "MAP-005: ToEventResponse should have null payload when not present")]
    public void MAP005()
    {
        var evt = new Event(
            EventTypes.McpServer.Instance.Started,
            "mcp-servers/test-server",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));

        var result = Mapper.ToEventResponse(evt, TimeZoneInfo.Utc);

        result.Payload.Should().BeNull();
    }

    [Fact(DisplayName = "MAP-008: ToDetailsResponse should map configuration correctly")]
    public void MAP008()
    {
        var serverName = McpServerName.Create("github").Value;
        var definition = new McpServerDefinition(
            serverName,
            "npx",
            new List<string> { "-y", "@modelcontextprotocol/server-github" }.AsReadOnly(),
            new Dictionary<string, string> { ["GITHUB_TOKEN"] = "secret" }.AsReadOnly());
        var statusEntry = new McpServerStatusCacheEntry(McpServerConnectionStatus.Verified, DateTime.UtcNow);

        var result = Mapper.ToDetailsResponse(serverName, statusEntry, TimeZoneInfo.Utc, definition);

        result.Name.Should().Be("github");
        result.Configuration.Should().NotBeNull();
        result.Configuration!.Command.Should().Be("npx");
        result.Configuration.Args.Should().HaveCount(2);
        result.Configuration.Env.Should().ContainKey("GITHUB_TOKEN");
    }

    [Fact(DisplayName = "MAP-009: ToListResponse should map server info correctly")]
    public void MAP009()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var info = new McpServerInfo(
            serverName,
            "docker",
            McpServerConnectionStatus.Verified,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));

        var result = Mapper.ToListResponse(info, TimeZoneInfo.Utc);

        result.Name.Should().Be("chronos");
        result.Status.Should().Be("verified");
        result.UpdatedAt.Should().StartWith("2024-01-15T10:30:00");
    }

    [Fact(DisplayName = "MAP-010: ToListResponse should handle null UpdatedAtUtc")]
    public void MAP010()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var info = new McpServerInfo(serverName, "docker");

        var result = Mapper.ToListResponse(info, TimeZoneInfo.Utc);

        result.UpdatedAt.Should().BeNull();
    }

    [Fact(DisplayName = "MAP-011: ToEventResponse should include target")]
    public void MAP011()
    {
        var evt = new Event(
            EventTypes.McpServer.Instance.Starting,
            "mcp-servers/test-server/instances/inst-123",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));

        var result = Mapper.ToEventResponse(evt, TimeZoneInfo.Utc);

        result.Target.Should().Be("mcp-servers/test-server/instances/inst-123");
    }

    [Fact(DisplayName = "MAP-013: ToEventResponse should include request ID when present")]
    public void MAP013()
    {
        var requestId = Guid.NewGuid().ToString();
        var evt = new Event(
            EventTypes.McpServer.Instance.Starting,
            "mcp-servers/test-server",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            null,
            requestId);

        var result = Mapper.ToEventResponse(evt, TimeZoneInfo.Utc);

        result.RequestId.Should().Be(requestId);
    }

    [Fact(DisplayName = "MAP-014: ToEventResponse should have null request ID when not present")]
    public void MAP014()
    {
        var evt = new Event(
            EventTypes.McpServer.Instance.Started,
            "mcp-servers/test-server",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));

        var result = Mapper.ToEventResponse(evt, TimeZoneInfo.Utc);

        result.RequestId.Should().BeNull();
    }

    [Fact(DisplayName = "MAP-015: ToPagedResponse should map paged events correctly")]
    public void MAP015()
    {
        var events = new List<Event>
        {
            new(EventTypes.McpServer.Instance.Starting, "mcp-servers/test-server", new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc)),
            new(EventTypes.McpServer.Instance.Started, "mcp-servers/test-server", new DateTime(2024, 1, 15, 10, 0, 1, DateTimeKind.Utc))
        };
        var pagedResult = new PagedResult<Event>(events, 1, 10, 50);

        var result = Mapper.ToPagedResponse(pagedResult, TimeZoneInfo.Utc);

        result.Items.Should().HaveCount(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(50);
        result.TotalPages.Should().Be(5);
    }

    [Fact(DisplayName = "MAP-016: ToPagedResponse should map event details correctly")]
    public void MAP016()
    {
        var events = new List<Event>
        {
            new(EventTypes.McpServer.Instance.Starting, "mcp-servers/test-server", new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc))
        };
        var pagedResult = new PagedResult<Event>(events, 1, 10, 1);

        var result = Mapper.ToPagedResponse(pagedResult, TimeZoneInfo.Utc);

        result.Items[0].EventType.Should().Be(EventTypes.McpServer.Instance.Starting.Value);
        result.Items[0].Timestamp.Should().StartWith("2024-01-15T10:00:00");
    }

    [Fact(DisplayName = "MAP-017: ToPagedResponse should handle empty paged result")]
    public void MAP017()
    {
        var pagedResult = new PagedResult<Event>([], 1, 10, 0);

        var result = Mapper.ToPagedResponse(pagedResult, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
        result.TotalItems.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    [Fact(DisplayName = "MAP-018: ToConfigurationResponse should map definition to configuration")]
    public void MAP018()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var definition = new McpServerDefinition(
            serverName,
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());

        var result = Mapper.ToConfigurationResponse(definition);

        result.Command.Should().Be("docker");
        result.Args.Should().BeEquivalentTo(new[] { "run", "--rm" });
        result.Env.Should().ContainKey("TZ");
    }

    [Fact(DisplayName = "MAP-019: ToDetailsResponse should include status and updatedOn")]
    public void MAP019()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var updatedOn = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var statusEntry = new McpServerStatusCacheEntry(McpServerConnectionStatus.Verified, updatedOn);

        var result = Mapper.ToDetailsResponse(serverName, statusEntry, TimeZoneInfo.Utc);

        result.Name.Should().Be("chronos");
        result.Status.Should().Be("verified");
        result.UpdatedAt.Should().StartWith("2024-01-15T10:30:00");
    }

    [Fact(DisplayName = "MAP-020: ToDetailsResponse without definition should have null configuration")]
    public void MAP020()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var statusEntry = new McpServerStatusCacheEntry(McpServerConnectionStatus.Unknown, null);

        var result = Mapper.ToDetailsResponse(serverName, statusEntry, TimeZoneInfo.Utc);

        result.Configuration.Should().BeNull();
    }

    [Fact(DisplayName = "MAP-021: ToDetailsResponseAfterCreate should return details with configuration")]
    public void MAP021()
    {
        var serverName = McpServerName.Create("new-server").Value;
        var definition = new McpServerDefinition(
            serverName,
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());

        var result = Mapper.ToDetailsResponseAfterCreate(definition, TimeZoneInfo.Utc);

        result.Name.Should().Be("new-server");
        result.Status.Should().Be("unknown");
        result.Configuration.Should().NotBeNull();
        result.Configuration!.Command.Should().Be("docker");
    }

    [Fact(DisplayName = "MAP-022: ToDomain should create definition from CreateRequest")]
    public void MAP022()
    {
        var request = new CreateRequest("new-server", new ConfigurationRequest("docker", ["run", "--rm"], new Dictionary<string, string> { ["TZ"] = "UTC" }));

        var result = Mapper.ToDomain(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Value.Should().Be("new-server");
        result.Value.Command.Should().Be("docker");
        result.Value.Args.Should().BeEquivalentTo(new[] { "run", "--rm" });
        result.Value.Env.Should().ContainKey("TZ");
    }

    [Fact(DisplayName = "MAP-023: ToDomain should create definition without configuration when null")]
    public void MAP023()
    {
        var request = new CreateRequest("new-server", null);

        var result = Mapper.ToDomain(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Value.Should().Be("new-server");
        result.Value.HasConfiguration.Should().BeFalse();
        result.Value.Command.Should().BeNull();
        result.Value.Args.Should().BeNull();
        result.Value.Env.Should().BeNull();
    }

    [Fact(DisplayName = "MAP-024: ToDomain should create definition from ConfigurationRequest")]
    public void MAP024()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var request = new ConfigurationRequest("npx", ["-y", "package"], new Dictionary<string, string> { ["API_KEY"] = "xxx" });

        var result = Mapper.ToDomain(serverName, request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(serverName);
        result.Value.Command.Should().Be("npx");
        result.Value.Args.Should().BeEquivalentTo(new[] { "-y", "package" });
        result.Value.Env.Should().ContainKey("API_KEY");
    }

    [Fact(DisplayName = "MAP-025: ToEventResponse should include event type")]
    public void MAP025()
    {
        var evt = new Event(
            EventTypes.McpServer.Instance.Starting,
            "mcp-servers/chronos",
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc));

        var result = Mapper.ToEventResponse(evt, TimeZoneInfo.Utc);

        result.EventType.Should().Be("mcp-server.instance.starting");
    }
}
