using Core.Domain.Paging;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Paging;

public class DateRangeFilterTests
{
    [Fact(DisplayName = "DRF-001: IsInRange should return true when no filters are set")]
    public void DRF001()
    {
        var filter = new DateRangeFilter(null, null);
        var timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        filter.IsInRange(timestamp).Should().BeTrue();
    }

    [Fact(DisplayName = "DRF-002: IsInRange should return true when timestamp equals From")]
    public void DRF002()
    {
        var from = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var filter = new DateRangeFilter(from, null);

        filter.IsInRange(from).Should().BeTrue();
    }

    [Fact(DisplayName = "DRF-003: IsInRange should return true when timestamp equals To")]
    public void DRF003()
    {
        var to = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var filter = new DateRangeFilter(null, to);

        filter.IsInRange(to).Should().BeTrue();
    }

    [Fact(DisplayName = "DRF-004: IsInRange should return false when timestamp is before From")]
    public void DRF004()
    {
        var from = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var filter = new DateRangeFilter(from, null);
        var timestamp = new DateTime(2024, 6, 15, 11, 59, 59, DateTimeKind.Utc);

        filter.IsInRange(timestamp).Should().BeFalse();
    }

    [Fact(DisplayName = "DRF-005: IsInRange should return false when timestamp is after To")]
    public void DRF005()
    {
        var to = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var filter = new DateRangeFilter(null, to);
        var timestamp = new DateTime(2024, 6, 15, 12, 0, 1, DateTimeKind.Utc);

        filter.IsInRange(timestamp).Should().BeFalse();
    }

    [Fact(DisplayName = "DRF-006: IsInRange should return true when timestamp is between From and To")]
    public void DRF006()
    {
        var from = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2024, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var filter = new DateRangeFilter(from, to);
        var timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        filter.IsInRange(timestamp).Should().BeTrue();
    }

    [Fact(DisplayName = "DRF-007: IsInRange should return false when timestamp is outside range")]
    public void DRF007()
    {
        var from = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2024, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var filter = new DateRangeFilter(from, to);
        var timestamp = new DateTime(2024, 6, 15, 15, 0, 0, DateTimeKind.Utc);

        filter.IsInRange(timestamp).Should().BeFalse();
    }
}
