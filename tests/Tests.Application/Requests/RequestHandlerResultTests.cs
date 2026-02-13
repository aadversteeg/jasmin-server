using System.Text.Json;
using Ave.Extensions.ErrorPaths;
using Core.Application.Requests;
using FluentAssertions;
using Xunit;

namespace Tests.Application.Requests;

public class RequestHandlerResultTests
{
    [Fact(DisplayName = "RHR-001: Success without output should have IsSuccess true and null output")]
    public void RHR001()
    {
        var result = RequestHandlerResult.Success();

        result.IsSuccess.Should().BeTrue();
        result.Output.Should().BeNull();
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "RHR-002: Success with output should have IsSuccess true and output set")]
    public void RHR002()
    {
        var output = JsonSerializer.SerializeToElement(new { value = 42 });

        var result = RequestHandlerResult.Success(output);

        result.IsSuccess.Should().BeTrue();
        result.Output.Should().NotBeNull();
        result.Errors.Should().BeNull();
    }

    [Fact(DisplayName = "RHR-003: Failure should have IsSuccess false and errors set")]
    public void RHR003()
    {
        var errors = new List<Error>
        {
            new(new ErrorCode("ERR_001"), "Something went wrong"),
            new(new ErrorCode("ERR_002"), "Another issue")
        }.AsReadOnly();

        var result = RequestHandlerResult.Failure(errors);

        result.IsSuccess.Should().BeFalse();
        result.Output.Should().BeNull();
        result.Errors.Should().NotBeNull();
        result.Errors.Should().HaveCount(2);
        result.Errors![0].Code.Should().Be(new ErrorCode("ERR_001"));
        result.Errors[1].Message.Should().Be("Another issue");
    }
}
