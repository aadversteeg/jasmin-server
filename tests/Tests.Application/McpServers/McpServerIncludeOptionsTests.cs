using Core.Application.McpServers;
using Core.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Tests.Application.McpServers;

public class McpServerIncludeOptionsTests
{
    [Fact(DisplayName = "MIO-001: Create with null should return Default options")]
    public void MIO001()
    {
        var result = McpServerIncludeOptions.Create(null);

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeFalse();
        result.Value.IncludeRequests.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-002: Create with empty string should return Default options")]
    public void MIO002()
    {
        var result = McpServerIncludeOptions.Create("");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeFalse();
        result.Value.IncludeRequests.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-004: Create with 'all' should set all options to true")]
    public void MIO004()
    {
        var result = McpServerIncludeOptions.Create("all");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeTrue();
        result.Value.IncludeRequests.Should().BeTrue();
        result.Value.IncludeInstances.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeResources.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-005: Create with 'ALL' (uppercase) should set all options to true")]
    public void MIO005()
    {
        var result = McpServerIncludeOptions.Create("ALL");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeTrue();
        result.Value.IncludeRequests.Should().BeTrue();
        result.Value.IncludeInstances.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeResources.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-008: Create with invalid option should return error")]
    public void MIO008()
    {
        var result = McpServerIncludeOptions.Create("invalid");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidIncludeOption);
        result.Error.Message.Should().Contain("invalid");
    }

    [Fact(DisplayName = "MIO-009: Default property should have all options as false")]
    public void MIO009()
    {
        var options = McpServerIncludeOptions.Default;

        options.IncludeConfiguration.Should().BeFalse();
        options.IncludeRequests.Should().BeFalse();
        options.IncludeInstances.Should().BeFalse();
        options.IncludeTools.Should().BeFalse();
        options.IncludePrompts.Should().BeFalse();
        options.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-010: All property should have all options as true")]
    public void MIO010()
    {
        var options = McpServerIncludeOptions.All;

        options.IncludeConfiguration.Should().BeTrue();
        options.IncludeRequests.Should().BeTrue();
        options.IncludeInstances.Should().BeTrue();
        options.IncludeTools.Should().BeTrue();
        options.IncludePrompts.Should().BeTrue();
        options.IncludeResources.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-011: Create with 'configuration' should set IncludeConfiguration to true")]
    public void MIO011()
    {
        var result = McpServerIncludeOptions.Create("configuration");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeTrue();
        result.Value.IncludeRequests.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-012: Create with 'requests' should set IncludeRequests to true")]
    public void MIO012()
    {
        var result = McpServerIncludeOptions.Create("requests");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeRequests.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-013: Create with comma-separated options should set multiple options")]
    public void MIO013()
    {
        var result = McpServerIncludeOptions.Create("configuration,requests");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeTrue();
        result.Value.IncludeRequests.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-015: Create with 'instances' should set IncludeInstances to true")]
    public void MIO015()
    {
        var result = McpServerIncludeOptions.Create("instances");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeInstances.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeFalse();
        result.Value.IncludeRequests.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-016: Create with 'tools' should set IncludeTools to true")]
    public void MIO016()
    {
        var result = McpServerIncludeOptions.Create("tools");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeFalse();
        result.Value.IncludePrompts.Should().BeFalse();
        result.Value.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-017: Create with 'prompts' should set IncludePrompts to true")]
    public void MIO017()
    {
        var result = McpServerIncludeOptions.Create("prompts");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeTools.Should().BeFalse();
        result.Value.IncludeResources.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-018: Create with 'resources' should set IncludeResources to true")]
    public void MIO018()
    {
        var result = McpServerIncludeOptions.Create("resources");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeResources.Should().BeTrue();
        result.Value.IncludeTools.Should().BeFalse();
        result.Value.IncludePrompts.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-019: Create with 'tools,prompts,resources' should set all metadata options")]
    public void MIO019()
    {
        var result = McpServerIncludeOptions.Create("tools,prompts,resources");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeTools.Should().BeTrue();
        result.Value.IncludePrompts.Should().BeTrue();
        result.Value.IncludeResources.Should().BeTrue();
        result.Value.IncludeConfiguration.Should().BeFalse();
    }
}
