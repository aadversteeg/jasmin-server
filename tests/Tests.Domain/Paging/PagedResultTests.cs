using Core.Domain.Paging;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Paging;

public class PagedResultTests
{
    [Fact(DisplayName = "PR-001: Constructor should set all properties")]
    public void PR001()
    {
        var items = new List<string> { "a", "b", "c" };

        var result = new PagedResult<string>(items, 1, 10, 100);

        result.Items.Should().BeEquivalentTo(items);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalItems.Should().Be(100);
    }

    [Fact(DisplayName = "PR-002: TotalPages should calculate correct value")]
    public void PR002()
    {
        var result = new PagedResult<string>([], 1, 10, 100);

        result.TotalPages.Should().Be(10);
    }

    [Fact(DisplayName = "PR-003: TotalPages should round up for partial pages")]
    public void PR003()
    {
        var result = new PagedResult<string>([], 1, 10, 105);

        result.TotalPages.Should().Be(11);
    }

    [Fact(DisplayName = "PR-004: TotalPages should be 0 when pageSize is 0")]
    public void PR004()
    {
        var result = new PagedResult<string>([], 1, 0, 100);

        result.TotalPages.Should().Be(0);
    }

    [Fact(DisplayName = "PR-005: TotalPages should be 0 when totalItems is 0")]
    public void PR005()
    {
        var result = new PagedResult<string>([], 1, 10, 0);

        result.TotalPages.Should().Be(0);
    }

    [Fact(DisplayName = "PR-006: TotalPages should be 1 when items fit on one page")]
    public void PR006()
    {
        var result = new PagedResult<string>([], 1, 10, 5);

        result.TotalPages.Should().Be(1);
    }

    [Fact(DisplayName = "PR-007: TotalPages should be 1 when items exactly fill one page")]
    public void PR007()
    {
        var result = new PagedResult<string>([], 1, 10, 10);

        result.TotalPages.Should().Be(1);
    }
}
