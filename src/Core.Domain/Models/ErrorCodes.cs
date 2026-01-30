namespace Core.Domain.Models;

/// <summary>
/// Contains predefined error codes for the application.
/// </summary>
public static class ErrorCodes
{
    public static readonly ErrorCode InvalidMcpServerName = new("INVALID_MCP_SERVER_NAME");
    public static readonly ErrorCode ConfigFileNotFound = new("CONFIG_FILE_NOT_FOUND");
    public static readonly ErrorCode ConfigFileInvalid = new("CONFIG_FILE_INVALID");
    public static readonly ErrorCode ConfigFileReadError = new("CONFIG_FILE_READ_ERROR");
    public static readonly ErrorCode ConfigFileWriteError = new("CONFIG_FILE_WRITE_ERROR");
    public static readonly ErrorCode DuplicateMcpServerName = new("DUPLICATE_MCP_SERVER_NAME");
    public static readonly ErrorCode McpServerNotFound = new("MCP_SERVER_NOT_FOUND");
    public static readonly ErrorCode InvalidIncludeOption = new("INVALID_INCLUDE_OPTION");
    public static readonly ErrorCode InvalidRequestAction = new("INVALID_REQUEST_ACTION");
    public static readonly ErrorCode InstanceIdRequiredForStop = new("INSTANCE_ID_REQUIRED_FOR_STOP");
}
