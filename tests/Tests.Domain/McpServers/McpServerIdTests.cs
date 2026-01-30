using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerIdTests
{
    [Fact(DisplayName = "MSID-001: Create should generate valid GUID")]
    public void MSID001()
    {
        var id = McpServerId.Create();

        id.Should().NotBeNull();
        Guid.TryParse(id.Value, out _).Should().BeTrue();
    }

    [Fact(DisplayName = "MSID-002: Create should generate unique IDs")]
    public void MSID002()
    {
        var id1 = McpServerId.Create();
        var id2 = McpServerId.Create();

        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact(DisplayName = "MSID-003: From should create from existing string")]
    public void MSID003()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";

        var id = McpServerId.From(guidString);

        id.Value.Should().Be(guidString);
    }

    [Fact(DisplayName = "MSID-004: ToString should return value")]
    public void MSID004()
    {
        var id = McpServerId.Create();

        id.ToString().Should().Be(id.Value);
    }

    [Fact(DisplayName = "MSID-005: Equal McpServerIds should have same hash code")]
    public void MSID005()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        var id1 = McpServerId.From(guidString);
        var id2 = McpServerId.From(guidString);

        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact(DisplayName = "MSID-006: Equal McpServerIds should be equal")]
    public void MSID006()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        var id1 = McpServerId.From(guidString);
        var id2 = McpServerId.From(guidString);

        id1.Should().Be(id2);
    }
}
