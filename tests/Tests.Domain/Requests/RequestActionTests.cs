using Core.Domain.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Requests;

public class RequestActionTests
{
    [Fact(DisplayName = "RAC-001: Constructor should set value")]
    public void RAC001()
    {
        var action = new RequestAction("mcp-server.start");

        action.Value.Should().Be("mcp-server.start");
    }

    [Fact(DisplayName = "RAC-002: Constructor should throw on null")]
    public void RAC002()
    {
        var act = () => new RequestAction(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "RAC-003: Constructor should throw on empty string")]
    public void RAC003()
    {
        var act = () => new RequestAction("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "RAC-004: Constructor should throw on whitespace")]
    public void RAC004()
    {
        var act = () => new RequestAction("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "RAC-005: Default struct should have empty value")]
    public void RAC005()
    {
        var action = default(RequestAction);

        action.Value.Should().Be(string.Empty);
    }

    [Fact(DisplayName = "RAC-006: Depth should return segment count")]
    public void RAC006()
    {
        new RequestAction("mcp-server").Depth.Should().Be(1);
        new RequestAction("mcp-server.instance").Depth.Should().Be(2);
        new RequestAction("mcp-server.instance.invoke-tool").Depth.Should().Be(3);
    }

    [Fact(DisplayName = "RAC-007: Depth of default struct should be zero")]
    public void RAC007()
    {
        default(RequestAction).Depth.Should().Be(0);
    }

    [Fact(DisplayName = "RAC-008: Leaf should return last segment")]
    public void RAC008()
    {
        new RequestAction("mcp-server").Leaf.Should().Be("mcp-server");
        new RequestAction("mcp-server.instance").Leaf.Should().Be("instance");
        new RequestAction("mcp-server.instance.invoke-tool").Leaf.Should().Be("invoke-tool");
    }

    [Fact(DisplayName = "RAC-009: Leaf of default struct should be empty")]
    public void RAC009()
    {
        default(RequestAction).Leaf.Should().Be(string.Empty);
    }

    [Fact(DisplayName = "RAC-010: Parent should return parent action")]
    public void RAC010()
    {
        var action = new RequestAction("mcp-server.instance.invoke-tool");

        action.Parent.Should().NotBeNull();
        action.Parent!.Value.Value.Should().Be("mcp-server.instance");
    }

    [Fact(DisplayName = "RAC-011: Parent of root action should be null")]
    public void RAC011()
    {
        var action = new RequestAction("mcp-server");

        action.Parent.Should().BeNull();
    }

    [Fact(DisplayName = "RAC-012: Parent of default struct should be null")]
    public void RAC012()
    {
        default(RequestAction).Parent.Should().BeNull();
    }

    [Fact(DisplayName = "RAC-013: IsChildOf should return true for exact match")]
    public void RAC013()
    {
        var action = new RequestAction("mcp-server.instance");

        action.IsChildOf(new RequestAction("mcp-server.instance")).Should().BeTrue();
    }

    [Fact(DisplayName = "RAC-014: IsChildOf should return true for descendant")]
    public void RAC014()
    {
        var action = new RequestAction("mcp-server.instance.invoke-tool");

        action.IsChildOf(new RequestAction("mcp-server")).Should().BeTrue();
        action.IsChildOf(new RequestAction("mcp-server.instance")).Should().BeTrue();
    }

    [Fact(DisplayName = "RAC-015: IsChildOf should return false for non-ancestor")]
    public void RAC015()
    {
        var action = new RequestAction("mcp-server.instance");

        action.IsChildOf(new RequestAction("mcp-server.instance.invoke-tool")).Should().BeFalse();
        action.IsChildOf(new RequestAction("other")).Should().BeFalse();
    }

    [Fact(DisplayName = "RAC-016: IsChildOf should not match partial segment prefix")]
    public void RAC016()
    {
        var action = new RequestAction("mcp-server-extra.start");

        action.IsChildOf(new RequestAction("mcp-server")).Should().BeFalse();
    }

    [Fact(DisplayName = "RAC-017: IsChildOf with default struct should return false")]
    public void RAC017()
    {
        var action = new RequestAction("mcp-server");

        action.IsChildOf(default).Should().BeFalse();
        default(RequestAction).IsChildOf(action).Should().BeFalse();
    }

    [Fact(DisplayName = "RAC-018: Slash operator should compose segments")]
    public void RAC018()
    {
        var parent = new RequestAction("mcp-server");

        var child = parent / "instance";

        child.Value.Should().Be("mcp-server.instance");
    }

    [Fact(DisplayName = "RAC-019: Slash operator should chain multiple segments")]
    public void RAC019()
    {
        var action = new RequestAction("mcp-server") / "instance" / "invoke-tool";

        action.Value.Should().Be("mcp-server.instance.invoke-tool");
    }

    [Fact(DisplayName = "RAC-020: Slash operator should throw on null child")]
    public void RAC020()
    {
        var parent = new RequestAction("mcp-server");

        var act = () => parent / null!;

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "RAC-021: Slash operator should throw on empty child")]
    public void RAC021()
    {
        var parent = new RequestAction("mcp-server");

        var act = () => parent / "";

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "RAC-022: Slash operator should throw when child contains separator")]
    public void RAC022()
    {
        var parent = new RequestAction("mcp-server");

        var act = () => parent / "instance.start";

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "RAC-023: Slash operator on default struct should create root action")]
    public void RAC023()
    {
        var action = default(RequestAction) / "mcp-server";

        action.Value.Should().Be("mcp-server");
    }

    [Fact(DisplayName = "RAC-024: Implicit string conversion should return value")]
    public void RAC024()
    {
        var action = new RequestAction("mcp-server.start");

        string value = action;

        value.Should().Be("mcp-server.start");
    }

    [Fact(DisplayName = "RAC-025: ToString should return value")]
    public void RAC025()
    {
        var action = new RequestAction("mcp-server.start");

        action.ToString().Should().Be("mcp-server.start");
    }

    [Fact(DisplayName = "RAC-026: Equal actions should be equal")]
    public void RAC026()
    {
        var a = new RequestAction("mcp-server.start");
        var b = new RequestAction("mcp-server.start");

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact(DisplayName = "RAC-027: Different actions should not be equal")]
    public void RAC027()
    {
        var a = new RequestAction("mcp-server.start");
        var b = new RequestAction("mcp-server.stop");

        a.Equals(b).Should().BeFalse();
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact(DisplayName = "RAC-028: Equal actions should have same hash code")]
    public void RAC028()
    {
        var a = new RequestAction("mcp-server.start");
        var b = new RequestAction("mcp-server.start");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact(DisplayName = "RAC-029: CompareTo should order lexicographically")]
    public void RAC029()
    {
        var a = new RequestAction("a.b");
        var b = new RequestAction("a.c");

        a.CompareTo(b).Should().BeNegative();
        b.CompareTo(a).Should().BePositive();
        a.CompareTo(a).Should().Be(0);
    }

    [Fact(DisplayName = "RAC-030: Comparison operators should work correctly")]
    public void RAC030()
    {
        var a = new RequestAction("a.b");
        var b = new RequestAction("a.c");

        (a < b).Should().BeTrue();
        (a <= b).Should().BeTrue();
        (b > a).Should().BeTrue();
        (b >= a).Should().BeTrue();
        (a <= a).Should().BeTrue();
        (a >= a).Should().BeTrue();
    }

    [Fact(DisplayName = "RAC-031: Equals with object should work for matching RequestAction")]
    public void RAC031()
    {
        var a = new RequestAction("mcp-server.start");
        object b = new RequestAction("mcp-server.start");

        a.Equals(b).Should().BeTrue();
    }

    [Fact(DisplayName = "RAC-032: Equals with object should return false for non-RequestAction")]
    public void RAC032()
    {
        var a = new RequestAction("mcp-server.start");

        a.Equals("mcp-server.start").Should().BeFalse();
        a.Equals(null).Should().BeFalse();
    }
}
