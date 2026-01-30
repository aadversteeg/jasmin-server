namespace Core.Domain.Models;

/// <summary>
/// Contains predefined errors for the application.
/// </summary>
public static class Errors
{
    public static readonly Error InvalidMcpServerId = new(
        ErrorCodes.InvalidMcpServerId,
        "Invalid value for McpServerId. It cannot be null or empty.");
}
