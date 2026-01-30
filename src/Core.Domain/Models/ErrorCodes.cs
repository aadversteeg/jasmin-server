namespace Core.Domain.Models;

/// <summary>
/// Contains predefined error codes for the application.
/// </summary>
public static class ErrorCodes
{
    public static readonly ErrorCode InvalidMcpServerId = new("INVALID_MCP_SERVER_ID");
    public static readonly ErrorCode ConfigFileNotFound = new("CONFIG_FILE_NOT_FOUND");
    public static readonly ErrorCode ConfigFileInvalid = new("CONFIG_FILE_INVALID");
    public static readonly ErrorCode ConfigFileReadError = new("CONFIG_FILE_READ_ERROR");
}
