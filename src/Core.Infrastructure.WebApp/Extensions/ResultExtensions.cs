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

    /// <summary>
    /// Converts a Result to a Created IActionResult.
    /// Returns 409 Conflict if duplicate, 400 if other failure, 201 Created with mapped data otherwise.
    /// </summary>
    public static IActionResult ToCreatedResult<T, TResponse>(
        this Result<T, Error> source,
        string routeName,
        Func<T, object> routeValuesSelector,
        Func<T, TResponse> map)
    {
        if (source.IsFailure)
        {
            if (source.Error.Code == ErrorCodes.DuplicateMcpServerName)
            {
                return new ConflictObjectResult(new { error = source.Error.Message });
            }
            return new BadRequestObjectResult(new { error = source.Error.Message });
        }

        return new CreatedAtRouteResult(routeName, routeValuesSelector(source.Value), map(source.Value));
    }

    /// <summary>
    /// Converts a Result to an OK IActionResult.
    /// Returns 404 if not found, 400 if other failure, 200 OK with mapped data otherwise.
    /// </summary>
    public static IActionResult ToOkResult<T, TResponse>(
        this Result<T, Error> source,
        Func<T, TResponse> map)
    {
        if (source.IsFailure)
        {
            if (source.Error.Code == ErrorCodes.McpServerNotFound)
            {
                return new NotFoundObjectResult(new { error = source.Error.Message });
            }
            return new BadRequestObjectResult(new { error = source.Error.Message });
        }

        return new OkObjectResult(map(source.Value));
    }

    /// <summary>
    /// Converts a Result containing Unit to a NoContent IActionResult.
    /// Returns 404 if not found, 400 if other failure, 204 No Content otherwise.
    /// </summary>
    public static IActionResult ToNoContentResult(this Result<Unit, Error> source)
    {
        if (source.IsFailure)
        {
            if (source.Error.Code == ErrorCodes.McpServerNotFound)
            {
                return new NotFoundObjectResult(new { error = source.Error.Message });
            }
            return new BadRequestObjectResult(new { error = source.Error.Message });
        }

        return new NoContentResult();
    }
}
