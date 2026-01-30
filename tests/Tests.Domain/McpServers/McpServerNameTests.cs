using Core.Domain.McpServers;
using Core.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerNameTests
{
    [Fact(DisplayName = "MSN-001: Create with valid value should return success")]
    public void MSN001()
    {
        var result = McpServerName.Create("chronos");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("chronos");
    }

    [Fact(DisplayName = "MSN-002: Create with null should return failure")]
    public void MSN002()
    {
        var result = McpServerName.Create(null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMcpServerName);
    }

    [Fact(DisplayName = "MSN-003: Create with empty string should return failure")]
    public void MSN003()
    {
        var result = McpServerName.Create("");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMcpServerName);
    }

    [Fact(DisplayName = "MSN-004: ToString should return value")]
    public void MSN004()
    {
        var result = McpServerName.Create("test-server");

        result.Value.ToString().Should().Be("test-server");
    }

    [Fact(DisplayName = "MSN-005: Equal McpServerNames should have same hash code")]
    public void MSN005()
    {
        var name1 = McpServerName.Create("server").Value;
        var name2 = McpServerName.Create("server").Value;

        name1.GetHashCode().Should().Be(name2.GetHashCode());
    }
}
