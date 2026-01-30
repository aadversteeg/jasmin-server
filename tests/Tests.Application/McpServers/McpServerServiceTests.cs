using Core.Application.McpServers;
using Core.Domain.McpServers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Tests.Application.McpServers;

public class McpServerServiceTests
{
    private static IOptions<McpServerOptions> CreateOptions(Dictionary<string, McpServerConfigEntry>? servers = null)
    {
        var options = new McpServerOptions
        {
            Servers = servers ?? []
        };
        var mock = new Mock<IOptions<McpServerOptions>>();
        mock.Setup(x => x.Value).Returns(options);
        return mock.Object;
    }

    [Fact(DisplayName = "MSS-001: GetAll should return empty list when no servers configured")]
    public void MSS001()
    {
        var service = new McpServerService(CreateOptions());

        var result = service.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact(DisplayName = "MSS-002: GetAll should return all configured servers")]
    public void MSS002()
    {
        var servers = new Dictionary<string, McpServerConfigEntry>
        {
            ["chronos"] = new McpServerConfigEntry { Command = "docker" },
            ["github"] = new McpServerConfigEntry { Command = "docker" }
        };
        var service = new McpServerService(CreateOptions(servers));

        var result = service.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(s => s.Id.Value == "chronos");
        result.Value.Should().Contain(s => s.Id.Value == "github");
    }

    [Fact(DisplayName = "MSS-003: GetAll should return servers sorted by id")]
    public void MSS003()
    {
        var servers = new Dictionary<string, McpServerConfigEntry>
        {
            ["zebra"] = new McpServerConfigEntry { Command = "npx" },
            ["alpha"] = new McpServerConfigEntry { Command = "docker" }
        };
        var service = new McpServerService(CreateOptions(servers));

        var result = service.GetAll();

        result.IsSuccess.Should().BeTrue();
        result.Value[0].Id.Value.Should().Be("alpha");
        result.Value[1].Id.Value.Should().Be("zebra");
    }

    [Fact(DisplayName = "MSS-004: GetById should return None for non-existent server")]
    public void MSS004()
    {
        var service = new McpServerService(CreateOptions());
        var id = McpServerId.Create("non-existent").Value;

        var result = service.GetById(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeFalse();
    }

    [Fact(DisplayName = "MSS-005: GetById should return server definition when found")]
    public void MSS005()
    {
        var servers = new Dictionary<string, McpServerConfigEntry>
        {
            ["chronos"] = new McpServerConfigEntry
            {
                Command = "docker",
                Args = ["run", "--rm", "-i"],
                Env = new Dictionary<string, string> { ["TZ"] = "Europe/Amsterdam" }
            }
        };
        var service = new McpServerService(CreateOptions(servers));
        var id = McpServerId.Create("chronos").Value;

        var result = service.GetById(id);

        result.IsSuccess.Should().BeTrue();
        result.Value.HasValue.Should().BeTrue();
        result.Value.Value.Id.Value.Should().Be("chronos");
        result.Value.Value.Command.Should().Be("docker");
        result.Value.Value.Args.Should().HaveCount(3);
        result.Value.Value.Env.Should().ContainKey("TZ");
    }
}
