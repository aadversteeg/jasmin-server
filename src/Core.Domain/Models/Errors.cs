namespace Core.Domain.Models;

/// <summary>
/// Contains predefined errors for the application.
/// </summary>
public static class Errors
{
    public static readonly Error InvalidMcpServerName = new(
        ErrorCodes.InvalidMcpServerName,
        "Invalid value for McpServerName. It cannot be null or empty.");

    public static Error ConfigFileNotFound(string path) => new(
        ErrorCodes.ConfigFileNotFound,
        $"Configuration file not found: {path}");

    public static Error ConfigFileInvalid(string path) => new(
        ErrorCodes.ConfigFileInvalid,
        $"Configuration file is invalid or contains malformed JSON: {path}");

    public static Error ConfigFileReadError(string path, string message) => new(
        ErrorCodes.ConfigFileReadError,
        $"Error reading configuration file '{path}': {message}");

    public static Error ConfigFileWriteError(string path, string message) => new(
        ErrorCodes.ConfigFileWriteError,
        $"Error writing configuration file '{path}': {message}");

    public static Error DuplicateMcpServerName(string name) => new(
        ErrorCodes.DuplicateMcpServerName,
        $"An MCP server with name '{name}' already exists.");

    public static Error McpServerNotFound(string id) => new(
        ErrorCodes.McpServerNotFound,
        $"MCP server with id '{id}' was not found.");

    public static Error InvalidIncludeOption(string option) => new(
        ErrorCodes.InvalidIncludeOption,
        $"Invalid value for include option: '{option}'. Valid options are: configuration, events, requests, instances, tools, prompts, resources, all");

    public static Error InvalidInstanceIncludeOption(string option) => new(
        ErrorCodes.InvalidInstanceIncludeOption,
        $"Invalid value for instance include option: '{option}'. Valid options are: tools, prompts, resources, all");

    public static readonly Error ConfigurationRequired = new(
        ErrorCodes.ConfigurationRequired,
        "Configuration is required when creating a server.");

    public static Error InvalidRequestAction(string action) => new(
        ErrorCodes.InvalidRequestAction,
        $"Invalid request action: '{action}'. Valid actions are: start, stop");

    public static readonly Error InstanceIdRequiredForStop = new(
        ErrorCodes.InstanceIdRequiredForStop,
        "InstanceId is required for stop action.");

    public static Error InvalidPage() => new(
        ErrorCodes.InvalidPage,
        "Page must be greater than or equal to 1.");

    public static Error InvalidPageSize() => new(
        ErrorCodes.InvalidPageSize,
        "PageSize must be between 1 and 100.");

    public static Error ConfigurationMissing(string serverName) => new(
        ErrorCodes.ConfigurationMissing,
        $"Cannot start MCP server '{serverName}' because it has no configuration. Add configuration first using PUT /v1/mcp-servers/{serverName}/configuration.");
}
