using Core.Domain.McpServers;
using Core.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerIdTests
{
    [Fact(DisplayName = "MSID-001: Create with valid value should return success")]
    public void MSID001()
    {
        var result = McpServerId.Create("chronos");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("chronos");
    }

    [Fact(DisplayName = "MSID-002: Create with null should return failure")]
    public void MSID002()
    {
        var result = McpServerId.Create(null!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMcpServerId);
    }

    [Fact(DisplayName = "MSID-003: Create with empty string should return failure")]
    public void MSID003()
    {
        var result = McpServerId.Create("");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.InvalidMcpServerId);
    }

    [Fact(DisplayName = "MSID-004: ToString should return value")]
    public void MSID004()
    {
        var result = McpServerId.Create("test-server");

        result.Value.ToString().Should().Be("test-server");
    }

    [Fact(DisplayName = "MSID-005: Equal McpServerIds should have same hash code")]
    public void MSID005()
    {
        var id1 = McpServerId.Create("server").Value;
        var id2 = McpServerId.Create("server").Value;

        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }
}
