using Ave.Extensions.Functional;
using Core.Domain.Models;

namespace Core.Application.McpServers;

/// <summary>
/// Options for including additional data in MCP server responses.
/// </summary>
public record McpServerIncludeOptions
{
    private const string ConfigurationOption = "configuration";
    private const string EventsOption = "events";
    private const string RequestsOption = "requests";
    private const string InstancesOption = "instances";
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
    public static McpServerIncludeOptions All => new()
    {
        IncludeConfiguration = true,
        IncludeEvents = true,
        IncludeRequests = true,
        IncludeInstances = true
    };

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
                case ConfigurationOption:
                    result = result with { IncludeConfiguration = true };
                    break;
                case EventsOption:
                    result = result with { IncludeEvents = true };
                    break;
                case RequestsOption:
                    result = result with { IncludeRequests = true };
                    break;
                case InstancesOption:
                    result = result with { IncludeInstances = true };
                    break;
                default:
                    return Result<McpServerIncludeOptions, Error>.Failure(
                        Errors.InvalidIncludeOption(option));
            }
        }

        return Result<McpServerIncludeOptions, Error>.Success(result);
    }

    /// <summary>
    /// Gets whether to include configuration in the response.
    /// </summary>
    public bool IncludeConfiguration { get; private init; }

    /// <summary>
    /// Gets whether to include events in the response.
    /// </summary>
    public bool IncludeEvents { get; private init; }

    /// <summary>
    /// Gets whether to include requests in the response.
    /// </summary>
    public bool IncludeRequests { get; private init; }

    /// <summary>
    /// Gets whether to include instances in the response.
    /// </summary>
    public bool IncludeInstances { get; private init; }
}
