using Ave.Extensions.ErrorPaths;

namespace Core.Domain.Models;

/// <summary>
/// Contains predefined errors for the application.
/// </summary>
public static class Errors
{
    public static readonly Error InvalidMcpServerName = new(
        ErrorCodes.McpServers.InvalidName,
        "Invalid value for McpServerName. It cannot be null or empty.");

    public static Error ConfigFileNotFound(string path) => new(
        ErrorCodes.McpServers.Config.NotFound,
        $"Configuration file not found: {path}");

    public static Error ConfigFileInvalid(string path) => new(
        ErrorCodes.McpServers.Config.Invalid,
        $"Configuration file is invalid or contains malformed JSON: {path}");

    public static Error ConfigFileReadError(string path, string message) => new(
        ErrorCodes.McpServers.Config.ReadError,
        $"Error reading configuration file '{path}': {message}");

    public static Error ConfigFileWriteError(string path, string message) => new(
        ErrorCodes.McpServers.Config.WriteError,
        $"Error writing configuration file '{path}': {message}");

    public static Error DuplicateMcpServerName(string name) => new(
        ErrorCodes.McpServers.DuplicateName,
        $"An MCP server with name '{name}' already exists.");

    public static Error McpServerNotFound(string id) => new(
        ErrorCodes.McpServers.NotFound,
        $"MCP server with id '{id}' was not found.");

    public static Error InvalidIncludeOption(string option) => new(
        ErrorCodes.Include.InvalidOption,
        $"Invalid value for include option: '{option}'. Valid options are: configuration, instances, tools, prompts, resources, all");

    public static Error InvalidInstanceIncludeOption(string option) => new(
        ErrorCodes.Include.InvalidInstanceOption,
        $"Invalid value for instance include option: '{option}'. Valid options are: tools, prompts, resources, all");

    public static readonly Error ConfigurationRequired = new(
        ErrorCodes.McpServers.Config.Required,
        "Configuration is required when creating a server.");

    public static Error InvalidRequestAction(string action) => new(
        ErrorCodes.Request.InvalidAction,
        $"Invalid request action: '{action}'.");

    public static readonly Error ToolNameRequired = new(
        ErrorCodes.McpServers.Parameters.ToolNameRequired,
        "ToolName is required for invoke-tool action.");

    public static readonly Error PromptNameRequired = new(
        ErrorCodes.McpServers.Parameters.PromptNameRequired,
        "PromptName is required for get-prompt action.");

    public static readonly Error ResourceUriRequired = new(
        ErrorCodes.McpServers.Parameters.ResourceUriRequired,
        "ResourceUri is required for read-resource action.");

    public static Error InvalidPage() => new(
        ErrorCodes.Request.InvalidPage,
        "Page must be greater than or equal to 1.");

    public static Error InvalidPageSize() => new(
        ErrorCodes.Request.InvalidPageSize,
        "PageSize must be between 1 and 100.");

    public static Error ConfigurationMissing(string serverName) => new(
        ErrorCodes.McpServers.Config.Missing,
        $"Cannot start MCP server '{serverName}' because it has no configuration. Add configuration first using PUT /v1/mcp-servers/{serverName}/configuration.");
}
