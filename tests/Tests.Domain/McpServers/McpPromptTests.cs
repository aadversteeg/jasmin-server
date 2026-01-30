using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpPromptTests
{
    [Fact(DisplayName = "MCP-001: Prompt should store all properties")]
    public void MCP001()
    {
        var arguments = new List<McpPromptArgument>
        {
            new("topic", "The topic to discuss", true),
            new("style", "Writing style", false)
        };

        var prompt = new McpPrompt("generate_text", "Generate Text", "Generates text based on a topic", arguments);

        prompt.Name.Should().Be("generate_text");
        prompt.Title.Should().Be("Generate Text");
        prompt.Description.Should().Be("Generates text based on a topic");
        prompt.Arguments.Should().HaveCount(2);
    }

    [Fact(DisplayName = "MCP-002: Prompt should allow null optional properties")]
    public void MCP002()
    {
        var prompt = new McpPrompt("simple_prompt", null, null, null);

        prompt.Name.Should().Be("simple_prompt");
        prompt.Title.Should().BeNull();
        prompt.Description.Should().BeNull();
        prompt.Arguments.Should().BeNull();
    }

    [Fact(DisplayName = "MCP-003: Prompts with same values should be equal")]
    public void MCP003()
    {
        var prompt1 = new McpPrompt("test", "Test", "Description", null);
        var prompt2 = new McpPrompt("test", "Test", "Description", null);

        prompt1.Should().Be(prompt2);
    }

    [Fact(DisplayName = "MCP-004: Prompts with different names should not be equal")]
    public void MCP004()
    {
        var prompt1 = new McpPrompt("prompt1", "Test", null, null);
        var prompt2 = new McpPrompt("prompt2", "Test", null, null);

        prompt1.Should().NotBe(prompt2);
    }

    [Fact(DisplayName = "MCP-005: Prompt arguments should store all properties")]
    public void MCP005()
    {
        var argument = new McpPromptArgument("topic", "The topic to discuss", true);

        argument.Name.Should().Be("topic");
        argument.Description.Should().Be("The topic to discuss");
        argument.Required.Should().BeTrue();
    }

    [Fact(DisplayName = "MCP-006: Prompt argument should allow null description")]
    public void MCP006()
    {
        var argument = new McpPromptArgument("param", null, false);

        argument.Name.Should().Be("param");
        argument.Description.Should().BeNull();
        argument.Required.Should().BeFalse();
    }
}
