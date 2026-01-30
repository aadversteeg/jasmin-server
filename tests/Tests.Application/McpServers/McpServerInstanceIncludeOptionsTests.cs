using Core.Application.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Application.McpServers;

public class McpServerInstanceIncludeOptionsTests
{
    [Fact(DisplayName = "MIIO-001: Create with null should return default options")]
    public void MIIO001()
    {
        var result = McpServerInstanceIncludeOptions.Create(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeFalse();
        result.Value.IncludePrompts.Should().BeFalse();
        result.Value.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIIO-002: Create with empty string should return default options")]
    public void MIIO002()
    {
        var result = McpServerInstanceIncludeOptions.Create("");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeFalse();
        result.Value.IncludePrompts.Should().BeFalse();
        result.Value.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIIO-003: Create with all should return all options enabled")]
    public void MIIO003()
    {
        var result = McpServerInstanceIncludeOptions.Create("all");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeResources.Should().BeTrue();
    }

    [Fact(DisplayName = "MIIO-004: Create with tools should only enable tools")]
    public void MIIO004()
    {
        var result = McpServerInstanceIncludeOptions.Create("tools");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeFalse();
        result.Value.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIIO-005: Create with prompts should only enable prompts")]
    public void MIIO005()
    {
        var result = McpServerInstanceIncludeOptions.Create("prompts");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeFalse();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIIO-006: Create with resources should only enable resources")]
    public void MIIO006()
    {
        var result = McpServerInstanceIncludeOptions.Create("resources");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeFalse();
        result.Value.IncludePrompts.Should().BeFalse();
        result.Value.IncludeResources.Should().BeTrue();
    }

    [Fact(DisplayName = "MIIO-007: Create with multiple options should enable all specified")]
    public void MIIO007()
    {
        var result = McpServerInstanceIncludeOptions.Create("tools,prompts,resources");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeResources.Should().BeTrue();
    }

    [Fact(DisplayName = "MIIO-008: Create with invalid option should return error")]
    public void MIIO008()
    {
        var result = McpServerInstanceIncludeOptions.Create("invalid");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Value.Should().Be("INVALID_INSTANCE_INCLUDE_OPTION");
    }

    [Fact(DisplayName = "MIIO-009: Create should be case insensitive")]
    public void MIIO009()
    {
        var result = McpServerInstanceIncludeOptions.Create("TOOLS,Prompts,RESOURCES");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeResources.Should().BeTrue();
    }

    [Fact(DisplayName = "MIIO-010: Create should handle whitespace in options")]
    public void MIIO010()
    {
        var result = McpServerInstanceIncludeOptions.Create(" tools , prompts ");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIIO-011: Default should have no options enabled")]
    public void MIIO011()
    {
        var options = McpServerInstanceIncludeOptions.Default;

        options.IncludeTools.Should().BeFalse();
        options.IncludePrompts.Should().BeFalse();
        options.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIIO-012: All should have all options enabled")]
    public void MIIO012()
    {
        var options = McpServerInstanceIncludeOptions.All;

        options.IncludeTools.Should().BeTrue();
        options.IncludePrompts.Should().BeTrue();
        options.IncludeResources.Should().BeTrue();
    }
}
