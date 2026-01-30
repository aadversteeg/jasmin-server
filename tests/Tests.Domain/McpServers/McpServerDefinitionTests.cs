using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerDefinitionTests
{
    [Fact(DisplayName = "MSD-001: Constructor should set all properties")]
    public void MSD001()
    {
        var id = McpServerName.Create("test-server").Value;
        var args = new List<string> { "-y", "test-package" }.AsReadOnly();
        var env = new Dictionary<string, string> { ["KEY"] = "value" }.AsReadOnly();

        var definition = new McpServerDefinition(id, "npx", args, env);

        definition.Id.Should().Be(id);
        definition.Command.Should().Be("npx");
        definition.Args.Should().HaveCount(2);
        definition.Env.Should().ContainKey("KEY");
    }

    [Fact(DisplayName = "MSD-002: Record equality should work correctly")]
    public void MSD002()
    {
        var id = McpServerName.Create("server").Value;
        var args = new List<string>().AsReadOnly();
        var env = new Dictionary<string, string>().AsReadOnly();

        var def1 = new McpServerDefinition(id, "docker", args, env);
        var def2 = new McpServerDefinition(id, "docker", args, env);

        def1.Should().Be(def2);
    }
}
