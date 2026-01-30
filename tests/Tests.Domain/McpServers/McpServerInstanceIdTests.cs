using Core.Domain.McpServers;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.McpServers;

public class McpServerInstanceIdTests
{
    [Fact(DisplayName = "MSIID-001: Create should generate valid GUID")]
    public void MSIID001()
    {
        var id = McpServerInstanceId.Create();

        id.Should().NotBeNull();
        Guid.TryParse(id.Value, out _).Should().BeTrue();
    }

    [Fact(DisplayName = "MSIID-002: Create should generate unique IDs")]
    public void MSIID002()
    {
        var id1 = McpServerInstanceId.Create();
        var id2 = McpServerInstanceId.Create();

        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact(DisplayName = "MSIID-003: From should create from existing string")]
    public void MSIID003()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";

        var id = McpServerInstanceId.From(guidString);

        id.Value.Should().Be(guidString);
    }

    [Fact(DisplayName = "MSIID-004: ToString should return value")]
    public void MSIID004()
    {
        var id = McpServerInstanceId.Create();

        id.ToString().Should().Be(id.Value);
    }

    [Fact(DisplayName = "MSIID-005: Equal McpServerInstanceIds should have same hash code")]
    public void MSIID005()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        var id1 = McpServerInstanceId.From(guidString);
        var id2 = McpServerInstanceId.From(guidString);

        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact(DisplayName = "MSIID-006: Equal McpServerInstanceIds should be equal")]
    public void MSIID006()
    {
        var guidString = "550e8400-e29b-41d4-a716-446655440000";
        var id1 = McpServerInstanceId.From(guidString);
        var id2 = McpServerInstanceId.From(guidString);

        id1.Should().Be(id2);
    }
}
