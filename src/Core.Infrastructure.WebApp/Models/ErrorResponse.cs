namespace Core.Infrastructure.WebApp.Models;

/// <summary>
/// Standard error response model for API errors.
/// </summary>
/// <param name="Errors">The list of errors.</param>
public record ErrorResponse(IReadOnlyList<ErrorDetail> Errors)
{
    /// <summary>
    /// Creates an error response with a single error.
    /// </summary>
    public static ErrorResponse Single(string code, string message) =>
        new([new ErrorDetail(code, message)]);

    /// <summary>
    /// Creates an error response from a domain error.
    /// </summary>
    public static ErrorResponse FromError(Core.Domain.Models.Error error) =>
        new([new ErrorDetail(error.Code.Value, error.Message)]);
}

/// <summary>
/// Details of a single error.
/// </summary>
/// <param name="Code">The error code.</param>
/// <param name="Message">The error message.</param>
public record ErrorDetail(string Code, string Message);
