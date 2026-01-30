namespace Core.Domain.Models;

/// <summary>
/// Contains predefined errors for the application.
/// </summary>
public static class Errors
{
    public static readonly Error InvalidMcpServerId = new(
        ErrorCodes.InvalidMcpServerId,
        "Invalid value for McpServerId. It cannot be null or empty.");

    public static Error ConfigFileNotFound(string path) => new(
        ErrorCodes.ConfigFileNotFound,
        $"Configuration file not found: {path}");

    public static Error ConfigFileInvalid(string path) => new(
        ErrorCodes.ConfigFileInvalid,
        $"Configuration file is invalid or contains malformed JSON: {path}");

    public static Error ConfigFileReadError(string path, string message) => new(
        ErrorCodes.ConfigFileReadError,
        $"Error reading configuration file '{path}': {message}");
}
