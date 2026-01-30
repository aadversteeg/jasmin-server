using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpToolTests
{
    [Fact(DisplayName = "MCT-001: Tool should store all properties")]
    public void MCT001()
    {
        var tool = new McpTool("read_file", "Read File", "Reads content from a file", "{\"type\":\"object\"}");

        tool.Name.Should().Be("read_file");
        tool.Title.Should().Be("Read File");
        tool.Description.Should().Be("Reads content from a file");
        tool.InputSchema.Should().Be("{\"type\":\"object\"}");
    }

    [Fact(DisplayName = "MCT-002: Tool should allow null optional properties")]
    public void MCT002()
    {
        var tool = new McpTool("simple_tool", null, null, null);

        tool.Name.Should().Be("simple_tool");
        tool.Title.Should().BeNull();
        tool.Description.Should().BeNull();
        tool.InputSchema.Should().BeNull();
    }

    [Fact(DisplayName = "MCT-003: Tools with same values should be equal")]
    public void MCT003()
    {
        var tool1 = new McpTool("test", "Test", "Description", "{}");
        var tool2 = new McpTool("test", "Test", "Description", "{}");

        tool1.Should().Be(tool2);
    }

    [Fact(DisplayName = "MCT-004: Tools with different names should not be equal")]
    public void MCT004()
    {
        var tool1 = new McpTool("tool1", "Test", null, null);
        var tool2 = new McpTool("tool2", "Test", null, null);

        tool1.Should().NotBe(tool2);
    }
}
