using System.Text.Json;
using Ave.Extensions.ErrorPaths;

namespace Core.Application.Requests;

/// <summary>
/// Discriminated result from a request handler: success (with optional output) or failure (with errors).
/// </summary>
public class RequestHandlerResult
{
    public bool IsSuccess { get; }
    public JsonElement? Output { get; }
    public IReadOnlyList<Error>? Errors { get; }

    private RequestHandlerResult(bool isSuccess, JsonElement? output, IReadOnlyList<Error>? errors)
    {
        IsSuccess = isSuccess;
        Output = output;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result with no output.
    /// </summary>
    public static RequestHandlerResult Success() => new(true, null, null);

    /// <summary>
    /// Creates a successful result with output.
    /// </summary>
    /// <param name="output">The output from the handler.</param>
    public static RequestHandlerResult Success(JsonElement output) => new(true, output, null);

    /// <summary>
    /// Creates a failure result with errors.
    /// </summary>
    /// <param name="errors">The errors that occurred.</param>
    public static RequestHandlerResult Failure(IReadOnlyList<Error> errors) => new(false, null, errors);
}
