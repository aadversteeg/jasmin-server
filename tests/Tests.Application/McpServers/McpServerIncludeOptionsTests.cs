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
        result.Value.IncludeEvents.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-002: Create with empty string should return Default options")]
    public void MIO002()
    {
        var result = McpServerIncludeOptions.Create("");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeEvents.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-003: Create with 'events' should set IncludeEvents to true")]
    public void MIO003()
    {
        var result = McpServerIncludeOptions.Create("events");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeEvents.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-004: Create with 'all' should set all options to true")]
    public void MIO004()
    {
        var result = McpServerIncludeOptions.Create("all");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeEvents.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-005: Create with 'ALL' (uppercase) should set all options to true")]
    public void MIO005()
    {
        var result = McpServerIncludeOptions.Create("ALL");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeEvents.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-006: Create with 'Events' (mixed case) should set IncludeEvents to true")]
    public void MIO006()
    {
        var result = McpServerIncludeOptions.Create("Events");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeEvents.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-007: Create with whitespace around option should work")]
    public void MIO007()
    {
        var result = McpServerIncludeOptions.Create("  events  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.IncludeEvents.Should().BeTrue();
    }

    [Fact(DisplayName = "MIO-008: Create with invalid option should return error")]
    public void MIO008()
    {
        var result = McpServerIncludeOptions.Create("invalid");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidIncludeOption);
        result.Error.Message.Should().Contain("invalid");
    }

    [Fact(DisplayName = "MIO-009: Default property should have IncludeEvents as false")]
    public void MIO009()
    {
        var options = McpServerIncludeOptions.Default;

        options.IncludeEvents.Should().BeFalse();
    }

    [Fact(DisplayName = "MIO-010: All property should have IncludeEvents as true")]
    public void MIO010()
    {
        var options = McpServerIncludeOptions.All;

        options.IncludeEvents.Should().BeTrue();
    }
}
