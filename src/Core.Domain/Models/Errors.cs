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
}
