using Ave.Extensions.Functional;
using Core.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace Core.Infrastructure.WebApp.Extensions;

/// <summary>
/// Extension methods for converting Result types to IActionResult.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a Result containing a Maybe to an IActionResult.
    /// Returns 404 if Maybe has no value, 400 if failure, 200 with mapped data otherwise.
    /// </summary>
    public static IActionResult ToActionResult<T, TResponse>(
        this Result<Maybe<T>, Error> source,
        Func<T, TResponse> map)
    {
        if (source.IsFailure)
        {
            return new BadRequestObjectResult(new { error = source.Error.Message });
        }

        if (source.Value.HasNoValue)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(map(source.Value.Value));
    }

    /// <summary>
    /// Converts a Result containing a list to an IActionResult.
    /// Returns 400 if failure, 200 with mapped data otherwise.
    /// </summary>
    public static IActionResult ToActionResult<T, TResponse>(
        this Result<IReadOnlyList<T>, Error> source,
        Func<T, TResponse> map)
    {
        if (source.IsFailure)
        {
            return new BadRequestObjectResult(new { error = source.Error.Message });
        }

        return new OkObjectResult(source.Value.Select(map).ToList());
    }
}
