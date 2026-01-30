using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Infrastructure.WebApp.Models.McpServers.Instances;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.WebApp.Models.McpServers.Instances;

public class InstanceMapperTests
{
    [Fact(DisplayName = "IMP-001: ToResponse should map instance info correctly")]
    public void IMP001()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instanceId = McpServerInstanceId.Create();
        var config = new McpServerEventConfiguration(
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        var instance = new McpServerInstanceInfo(
            instanceId,
            serverName,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            config);

        var result = InstanceMapper.ToResponse(instance, TimeZoneInfo.Utc);

        result.InstanceId.Should().Be(instanceId.Value);
        result.ServerName.Should().Be("chronos");
        result.StartedAt.Should().StartWith("2024-01-15T10:30:00");
        result.StartedAt.Should().EndWith("Z");
    }

    [Fact(DisplayName = "IMP-002: ToResponse should map configuration when present")]
    public void IMP002()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var config = new McpServerEventConfiguration(
            "docker",
            new List<string> { "run", "--rm" }.AsReadOnly(),
            new Dictionary<string, string> { ["TZ"] = "UTC" }.AsReadOnly());
        var instance = new McpServerInstanceInfo(
            McpServerInstanceId.Create(),
            serverName,
            DateTime.UtcNow,
            config);

        var result = InstanceMapper.ToResponse(instance, TimeZoneInfo.Utc);

        result.Configuration.Should().NotBeNull();
        result.Configuration!.Command.Should().Be("docker");
        result.Configuration.Args.Should().BeEquivalentTo(new[] { "run", "--rm" });
        result.Configuration.Env.Should().ContainKey("TZ");
    }

    [Fact(DisplayName = "IMP-003: ToResponse should have null configuration when not present")]
    public void IMP003()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instance = new McpServerInstanceInfo(
            McpServerInstanceId.Create(),
            serverName,
            DateTime.UtcNow,
            null);

        var result = InstanceMapper.ToResponse(instance, TimeZoneInfo.Utc);

        result.Configuration.Should().BeNull();
    }

    [Fact(DisplayName = "IMP-004: ToResponse should convert to specified timezone")]
    public void IMP004()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instance = new McpServerInstanceInfo(
            McpServerInstanceId.Create(),
            serverName,
            new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
            null);
        var amsterdamTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Amsterdam");

        var result = InstanceMapper.ToResponse(instance, amsterdamTz);

        // In January, Amsterdam is UTC+1
        result.StartedAt.Should().StartWith("2024-01-15T11:30:00");
        result.StartedAt.Should().EndWith("+01:00");
    }

    [Fact(DisplayName = "IMP-005: ToListResponse should map list of instances")]
    public void IMP005()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var instances = new List<McpServerInstanceInfo>
        {
            new(McpServerInstanceId.Create(), serverName, new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc), null),
            new(McpServerInstanceId.Create(), serverName, new DateTime(2024, 1, 15, 11, 0, 0, DateTimeKind.Utc), null)
        };

        var result = InstanceMapper.ToListResponse(instances, TimeZoneInfo.Utc);

        result.Items.Should().HaveCount(2);
        result.Items[0].ServerName.Should().Be("chronos");
        result.Items[1].ServerName.Should().Be("chronos");
    }

    [Fact(DisplayName = "IMP-006: ToListResponse should handle empty list")]
    public void IMP006()
    {
        var instances = new List<McpServerInstanceInfo>();

        var result = InstanceMapper.ToListResponse(instances, TimeZoneInfo.Utc);

        result.Items.Should().BeEmpty();
    }

    [Fact(DisplayName = "IMP-007: ToListResponse should include configuration for each instance")]
    public void IMP007()
    {
        var serverName = McpServerName.Create("chronos").Value;
        var config = new McpServerEventConfiguration(
            "docker",
            new List<string> { "run" }.AsReadOnly(),
            new Dictionary<string, string>().AsReadOnly());
        var instances = new List<McpServerInstanceInfo>
        {
            new(McpServerInstanceId.Create(), serverName, DateTime.UtcNow, config),
            new(McpServerInstanceId.Create(), serverName, DateTime.UtcNow, null)
        };

        var result = InstanceMapper.ToListResponse(instances, TimeZoneInfo.Utc);

        result.Items[0].Configuration.Should().NotBeNull();
        result.Items[0].Configuration!.Command.Should().Be("docker");
        result.Items[1].Configuration.Should().BeNull();
    }
}
