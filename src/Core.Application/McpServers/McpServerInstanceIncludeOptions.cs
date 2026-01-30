using Ave.Extensions.Functional;
using Core.Domain.Models;

namespace Core.Application.McpServers;

/// <summary>
/// Options for including additional data in MCP server instance responses.
/// </summary>
public record McpServerInstanceIncludeOptions
{
    private const string ToolsOption = "tools";
    private const string PromptsOption = "prompts";
    private const string ResourcesOption = "resources";
    private const string AllOption = "all";

    private McpServerInstanceIncludeOptions()
    {
    }

    /// <summary>
    /// Gets the default options with no additional data included.
    /// </summary>
    public static McpServerInstanceIncludeOptions Default => new();

    /// <summary>
    /// Gets options with all additional data included.
    /// </summary>
    public static McpServerInstanceIncludeOptions All => new()
    {
        IncludeTools = true,
        IncludePrompts = true,
        IncludeResources = true
    };

    /// <summary>
    /// Creates include options from a query parameter string.
    /// </summary>
    /// <param name="include">Comma-separated list of options or "all".</param>
    /// <returns>A result containing the parsed options or an error.</returns>
    public static Result<McpServerInstanceIncludeOptions, Error> Create(string? include)
    {
        if (string.IsNullOrEmpty(include))
        {
            return Result<McpServerInstanceIncludeOptions, Error>.Success(Default);
        }

        if (include.Trim().ToLowerInvariant() == AllOption)
        {
            return Result<McpServerInstanceIncludeOptions, Error>.Success(All);
        }

        var options = include.Split(',').Select(x => x.Trim().ToLowerInvariant()).ToList();
        var result = new McpServerInstanceIncludeOptions();

        foreach (var option in options)
        {
            switch (option)
            {
                case ToolsOption:
                    result = result with { IncludeTools = true };
                    break;
                case PromptsOption:
                    result = result with { IncludePrompts = true };
                    break;
                case ResourcesOption:
                    result = result with { IncludeResources = true };
                    break;
                default:
                    return Result<McpServerInstanceIncludeOptions, Error>.Failure(
                        Errors.InvalidInstanceIncludeOption(option));
            }
        }

        return Result<McpServerInstanceIncludeOptions, Error>.Success(result);
    }

    /// <summary>
    /// Gets whether to include tools in the response.
    /// </summary>
    public bool IncludeTools { get; private init; }

    /// <summary>
    /// Gets whether to include prompts in the response.
    /// </summary>
    public bool IncludePrompts { get; private init; }

    /// <summary>
    /// Gets whether to include resources in the response.
    /// </summary>
    public bool IncludeResources { get; private init; }
}
