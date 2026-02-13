using Core.Domain.Events;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Events;

public class EventTypeTests
{
    [Fact(DisplayName = "EVT-001: Constructor should set value")]
    public void EVT001()
    {
        var eventType = new EventType("mcp-server.instance.started");

        eventType.Value.Should().Be("mcp-server.instance.started");
    }

    [Fact(DisplayName = "EVT-002: Constructor should throw on null")]
    public void EVT002()
    {
        var act = () => new EventType(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "EVT-003: Constructor should throw on empty string")]
    public void EVT003()
    {
        var act = () => new EventType("");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "EVT-004: Constructor should throw on whitespace")]
    public void EVT004()
    {
        var act = () => new EventType("  ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "EVT-005: Default struct should have empty value")]
    public void EVT005()
    {
        var eventType = default(EventType);

        eventType.Value.Should().Be(string.Empty);
    }

    [Fact(DisplayName = "EVT-006: Depth should return segment count")]
    public void EVT006()
    {
        new EventType("mcp-server").Depth.Should().Be(1);
        new EventType("mcp-server.instance").Depth.Should().Be(2);
        new EventType("mcp-server.instance.started").Depth.Should().Be(3);
    }

    [Fact(DisplayName = "EVT-007: Depth of default struct should be zero")]
    public void EVT007()
    {
        default(EventType).Depth.Should().Be(0);
    }

    [Fact(DisplayName = "EVT-008: Leaf should return last segment")]
    public void EVT008()
    {
        new EventType("mcp-server").Leaf.Should().Be("mcp-server");
        new EventType("mcp-server.instance").Leaf.Should().Be("instance");
        new EventType("mcp-server.instance.started").Leaf.Should().Be("started");
    }

    [Fact(DisplayName = "EVT-009: Leaf of default struct should be empty")]
    public void EVT009()
    {
        default(EventType).Leaf.Should().Be(string.Empty);
    }

    [Fact(DisplayName = "EVT-010: Parent should return parent event type")]
    public void EVT010()
    {
        var eventType = new EventType("mcp-server.instance.started");

        eventType.Parent.Should().NotBeNull();
        eventType.Parent!.Value.Value.Should().Be("mcp-server.instance");
    }

    [Fact(DisplayName = "EVT-011: Parent of root event type should be null")]
    public void EVT011()
    {
        var eventType = new EventType("mcp-server");

        eventType.Parent.Should().BeNull();
    }

    [Fact(DisplayName = "EVT-012: Parent of default struct should be null")]
    public void EVT012()
    {
        default(EventType).Parent.Should().BeNull();
    }

    [Fact(DisplayName = "EVT-013: IsChildOf should return true for exact match")]
    public void EVT013()
    {
        var eventType = new EventType("mcp-server.instance");

        eventType.IsChildOf(new EventType("mcp-server.instance")).Should().BeTrue();
    }

    [Fact(DisplayName = "EVT-014: IsChildOf should return true for descendant")]
    public void EVT014()
    {
        var eventType = new EventType("mcp-server.instance.started");

        eventType.IsChildOf(new EventType("mcp-server")).Should().BeTrue();
        eventType.IsChildOf(new EventType("mcp-server.instance")).Should().BeTrue();
    }

    [Fact(DisplayName = "EVT-015: IsChildOf should return false for non-ancestor")]
    public void EVT015()
    {
        var eventType = new EventType("mcp-server.instance");

        eventType.IsChildOf(new EventType("mcp-server.instance.started")).Should().BeFalse();
        eventType.IsChildOf(new EventType("other")).Should().BeFalse();
    }

    [Fact(DisplayName = "EVT-016: IsChildOf should not match partial segment prefix")]
    public void EVT016()
    {
        var eventType = new EventType("mcp-server-extra.created");

        eventType.IsChildOf(new EventType("mcp-server")).Should().BeFalse();
    }

    [Fact(DisplayName = "EVT-017: IsChildOf with default struct should return false")]
    public void EVT017()
    {
        var eventType = new EventType("mcp-server");

        eventType.IsChildOf(default).Should().BeFalse();
        default(EventType).IsChildOf(eventType).Should().BeFalse();
    }

    [Fact(DisplayName = "EVT-018: Slash operator should compose segments")]
    public void EVT018()
    {
        var parent = new EventType("mcp-server");

        var child = parent / "instance";

        child.Value.Should().Be("mcp-server.instance");
    }

    [Fact(DisplayName = "EVT-019: Slash operator should chain multiple segments")]
    public void EVT019()
    {
        var eventType = new EventType("mcp-server") / "instance" / "started";

        eventType.Value.Should().Be("mcp-server.instance.started");
    }

    [Fact(DisplayName = "EVT-020: Slash operator should throw on null child")]
    public void EVT020()
    {
        var parent = new EventType("mcp-server");

        var act = () => parent / null!;

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "EVT-021: Slash operator should throw on empty child")]
    public void EVT021()
    {
        var parent = new EventType("mcp-server");

        var act = () => parent / "";

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "EVT-022: Slash operator should throw when child contains separator")]
    public void EVT022()
    {
        var parent = new EventType("mcp-server");

        var act = () => parent / "instance.started";

        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "EVT-023: Slash operator on default struct should create root type")]
    public void EVT023()
    {
        var eventType = default(EventType) / "mcp-server";

        eventType.Value.Should().Be("mcp-server");
    }

    [Fact(DisplayName = "EVT-024: Implicit string conversion should return value")]
    public void EVT024()
    {
        var eventType = new EventType("mcp-server.instance.started");

        string value = eventType;

        value.Should().Be("mcp-server.instance.started");
    }

    [Fact(DisplayName = "EVT-025: ToString should return value")]
    public void EVT025()
    {
        var eventType = new EventType("mcp-server.instance.started");

        eventType.ToString().Should().Be("mcp-server.instance.started");
    }

    [Fact(DisplayName = "EVT-026: Equal event types should be equal")]
    public void EVT026()
    {
        var a = new EventType("mcp-server.instance.started");
        var b = new EventType("mcp-server.instance.started");

        a.Equals(b).Should().BeTrue();
        (a == b).Should().BeTrue();
        (a != b).Should().BeFalse();
    }

    [Fact(DisplayName = "EVT-027: Different event types should not be equal")]
    public void EVT027()
    {
        var a = new EventType("mcp-server.instance.started");
        var b = new EventType("mcp-server.instance.stopped");

        a.Equals(b).Should().BeFalse();
        (a == b).Should().BeFalse();
        (a != b).Should().BeTrue();
    }

    [Fact(DisplayName = "EVT-028: Equal event types should have same hash code")]
    public void EVT028()
    {
        var a = new EventType("mcp-server.instance.started");
        var b = new EventType("mcp-server.instance.started");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact(DisplayName = "EVT-029: CompareTo should order lexicographically")]
    public void EVT029()
    {
        var a = new EventType("a.b");
        var b = new EventType("a.c");

        a.CompareTo(b).Should().BeNegative();
        b.CompareTo(a).Should().BePositive();
        a.CompareTo(a).Should().Be(0);
    }

    [Fact(DisplayName = "EVT-030: Comparison operators should work correctly")]
    public void EVT030()
    {
        var a = new EventType("a.b");
        var b = new EventType("a.c");

        (a < b).Should().BeTrue();
        (a <= b).Should().BeTrue();
        (b > a).Should().BeTrue();
        (b >= a).Should().BeTrue();
        (a <= a).Should().BeTrue();
        (a >= a).Should().BeTrue();
    }

    [Fact(DisplayName = "EVT-031: Equals with object should work for matching EventType")]
    public void EVT031()
    {
        var a = new EventType("mcp-server.instance.started");
        object b = new EventType("mcp-server.instance.started");

        a.Equals(b).Should().BeTrue();
    }

    [Fact(DisplayName = "EVT-032: Equals with object should return false for non-EventType")]
    public void EVT032()
    {
        var a = new EventType("mcp-server.instance.started");

        a.Equals("mcp-server.instance.started").Should().BeFalse();
        a.Equals(null).Should().BeFalse();
    }
}
