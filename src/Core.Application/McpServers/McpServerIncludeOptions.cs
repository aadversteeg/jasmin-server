using Ave.Extensions.Functional;
using Core.Domain.Models;

namespace Core.Application.McpServers;

/// <summary>
/// Options for including additional data in MCP server responses.
/// </summary>
public record McpServerIncludeOptions
{
    private const string EventsOption = "events";
    private const string AllOption = "all";

    private McpServerIncludeOptions()
    {
    }

    /// <summary>
    /// Gets the default options with no additional data included.
    /// </summary>
    public static McpServerIncludeOptions Default => new();

    /// <summary>
    /// Gets options with all additional data included.
    /// </summary>
    public static McpServerIncludeOptions All => new() { IncludeEvents = true };

    /// <summary>
    /// Creates include options from a query parameter string.
    /// </summary>
    /// <param name="include">Comma-separated list of options or "all".</param>
    /// <returns>A result containing the parsed options or an error.</returns>
    public static Result<McpServerIncludeOptions, Error> Create(string? include)
    {
        if (string.IsNullOrEmpty(include))
        {
            return Result<McpServerIncludeOptions, Error>.Success(Default);
        }

        if (include.Trim().ToLowerInvariant() == AllOption)
        {
            return Result<McpServerIncludeOptions, Error>.Success(All);
        }

        var options = include.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
        var result = new McpServerIncludeOptions();

        foreach (var option in options)
        {
            switch (option)
            {
                case EventsOption:
                    result = result with { IncludeEvents = true };
                    break;
                default:
                    return Result<McpServerIncludeOptions, Error>.Failure(
                        Errors.InvalidIncludeOption(option));
            }
        }

        return Result<McpServerIncludeOptions, Error>.Success(result);
    }

    /// <summary>
    /// Gets whether to include events in the response.
    /// </summary>
    public bool IncludeEvents { get; private init; }
}
