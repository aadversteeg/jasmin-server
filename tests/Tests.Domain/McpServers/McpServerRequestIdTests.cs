using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerRequestIdTests
{
    [Fact(DisplayName = "RID-001: Create should generate valid GUID")]
    public void RID001()
    {
        var id = McpServerRequestId.Create();

        id.Should().NotBeNull();
        Guid.TryParse(id.Value, out _).Should().BeTrue();
    }

    [Fact(DisplayName = "RID-002: Create should generate unique IDs")]
    public void RID002()
    {
        var id1 = McpServerRequestId.Create();
        var id2 = McpServerRequestId.Create();

        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact(DisplayName = "RID-003: From should create from existing string")]
    public void RID003()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";

        var id = McpServerRequestId.From(guidString);

        id.Value.Should().Be(guidString);
    }

    [Fact(DisplayName = "RID-004: ToString should return value")]
    public void RID004()
    {
        var id = McpServerRequestId.Create();

        id.ToString().Should().Be(id.Value);
    }

    [Fact(DisplayName = "RID-005: Equal McpServerRequestIds should have same hash code")]
    public void RID005()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        var id1 = McpServerRequestId.From(guidString);
        var id2 = McpServerRequestId.From(guidString);

        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact(DisplayName = "RID-006: Equal McpServerRequestIds should be equal")]
    public void RID006()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        var id1 = McpServerRequestId.From(guidString);
        var id2 = McpServerRequestId.From(guidString);

        id1.Should().Be(id2);
    }
}
