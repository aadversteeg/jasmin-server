using Core.Infrastructure.Messaging;
using FluentAssertions;
using Xunit;

namespace Tests.Infrastructure.Messaging;

public class EventPublisherSettingsTests
{
    [Fact(DisplayName = "EPS-001: Default ChannelCapacity should be 10000")]
    public void EPS001()
    {
        var settings = new EventPublisherSettings();

        settings.ChannelCapacity.Should().Be(10000);
    }

    [Fact(DisplayName = "EPS-002: Default OverflowPolicy should be DropOldest")]
    public void EPS002()
    {
        var settings = new EventPublisherSettings();

        settings.OverflowPolicy.Should().Be(OverflowPolicy.DropOldest);
    }

    [Fact(DisplayName = "EPS-003: ChannelCapacity should be settable")]
    public void EPS003()
    {
        var settings = new EventPublisherSettings { ChannelCapacity = 5000 };

        settings.ChannelCapacity.Should().Be(5000);
    }

    [Fact(DisplayName = "EPS-004: OverflowPolicy should be settable to DropNewest")]
    public void EPS004()
    {
        var settings = new EventPublisherSettings { OverflowPolicy = OverflowPolicy.DropNewest };

        settings.OverflowPolicy.Should().Be(OverflowPolicy.DropNewest);
    }

    [Fact(DisplayName = "EPS-005: OverflowPolicy should be settable to Wait")]
    public void EPS005()
    {
        var settings = new EventPublisherSettings { OverflowPolicy = OverflowPolicy.Wait };

        settings.OverflowPolicy.Should().Be(OverflowPolicy.Wait);
    }
}
