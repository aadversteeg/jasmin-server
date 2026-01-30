using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerInfoTests
{
    [Fact(DisplayName = "MSI-001: Constructor should set all properties")]
    public void MSI001()
    {
        var id = McpServerName.Create("test-server").Value;

        var info = new McpServerInfo(id, "docker");

        info.Id.Should().Be(id);
        info.Command.Should().Be("docker");
    }

    [Fact(DisplayName = "MSI-002: Record equality should work correctly")]
    public void MSI002()
    {
        var id = McpServerName.Create("server").Value;

        var info1 = new McpServerInfo(id, "npx");
        var info2 = new McpServerInfo(id, "npx");

        info1.Should().Be(info2);
    }
}
