using Core.Domain.Paging;
using FluentAssertions;
using Xunit;

namespace Tests.Domain.Paging;

public class PagingParametersTests
{
    [Fact(DisplayName = "PP-001: Create should succeed with valid parameters")]
    public void PP001()
    {
        var result = PagingParameters.Create(1, 20);

        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact(DisplayName = "PP-002: Create should fail when page is less than 1")]
    public void PP002()
    {
        var result = PagingParameters.Create(0, 20);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("Page must be greater than or equal to 1");
    }

    [Fact(DisplayName = "PP-003: Create should fail when page is negative")]
    public void PP003()
    {
        var result = PagingParameters.Create(-1, 20);

        result.IsFailure.Should().BeTrue();
    }

    [Fact(DisplayName = "PP-004: Create should fail when pageSize is less than 1")]
    public void PP004()
    {
        var result = PagingParameters.Create(1, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("PageSize must be between 1 and 100");
    }

    [Fact(DisplayName = "PP-005: Create should fail when pageSize exceeds 100")]
    public void PP005()
    {
        var result = PagingParameters.Create(1, 101);

        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain("PageSize must be between 1 and 100");
    }

    [Fact(DisplayName = "PP-006: Create should succeed with pageSize of 100")]
    public void PP006()
    {
        var result = PagingParameters.Create(1, 100);

        result.IsSuccess.Should().BeTrue();
        result.Value.PageSize.Should().Be(100);
    }

    [Fact(DisplayName = "PP-007: Skip should calculate correct offset for page 1")]
    public void PP007()
    {
        var result = PagingParameters.Create(1, 20);

        result.Value.Skip.Should().Be(0);
    }

    [Fact(DisplayName = "PP-008: Skip should calculate correct offset for page 2")]
    public void PP008()
    {
        var result = PagingParameters.Create(2, 20);

        result.Value.Skip.Should().Be(20);
    }

    [Fact(DisplayName = "PP-009: Skip should calculate correct offset for page 5 with pageSize 10")]
    public void PP009()
    {
        var result = PagingParameters.Create(5, 10);

        result.Value.Skip.Should().Be(40);
    }
}
