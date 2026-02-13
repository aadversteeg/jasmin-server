using Ave.Extensions.ErrorPaths;

namespace Core.Domain.Models;

/// <summary>
/// Contains predefined hierarchical error codes for the application.
/// Codes are composable and support hierarchical matching via <see cref="Error.Is"/>.
/// </summary>
public static class ErrorCodes
{
    public static readonly ErrorCode McpServer = new("McpServer");

    public static class McpServers
    {
        public static readonly ErrorCode NotFound = McpServer / "NotFound";
        public static readonly ErrorCode InvalidName = McpServer / "InvalidName";
        public static readonly ErrorCode DuplicateName = McpServer / "DuplicateName";
        public static readonly ErrorCode InvalidTarget = McpServer / "InvalidTarget";

        public static class Config
        {
            private static readonly ErrorCode Root = McpServer / "Config";
            public static readonly ErrorCode NotFound = Root / "NotFound";
            public static readonly ErrorCode Invalid = Root / "Invalid";
            public static readonly ErrorCode ReadError = Root / "ReadError";
            public static readonly ErrorCode WriteError = Root / "WriteError";
            public static readonly ErrorCode Required = Root / "Required";
            public static readonly ErrorCode Missing = Root / "Missing";
        }

        public static class Instance
        {
            private static readonly ErrorCode Root = McpServer / "Instance";
            public static readonly ErrorCode NotFound = Root / "NotFound";
            public static readonly ErrorCode StartFailed = Root / "StartFailed";
            public static readonly ErrorCode StopFailed = Root / "StopFailed";
            public static readonly ErrorCode ConnectionFailed = Root / "ConnectionFailed";
        }

        public static class ToolInvocation
        {
            private static readonly ErrorCode Root = McpServer / "ToolInvocation";
            public static readonly ErrorCode Failed = Root / "Failed";
            public static readonly ErrorCode Timeout = Root / "Timeout";
            public static readonly ErrorCode Cancelled = Root / "Cancelled";
        }

        public static class Prompt
        {
            private static readonly ErrorCode Root = McpServer / "Prompt";
            public static readonly ErrorCode Failed = Root / "Failed";
            public static readonly ErrorCode Timeout = Root / "Timeout";
            public static readonly ErrorCode Cancelled = Root / "Cancelled";
        }

        public static class Resource
        {
            private static readonly ErrorCode Root = McpServer / "Resource";
            public static readonly ErrorCode Failed = Root / "Failed";
            public static readonly ErrorCode Timeout = Root / "Timeout";
            public static readonly ErrorCode Cancelled = Root / "Cancelled";
        }

        public static class Parameters
        {
            private static readonly ErrorCode Root = McpServer / "Parameters";
            public static readonly ErrorCode Required = Root / "Required";
            public static readonly ErrorCode ToolNameRequired = Root / "ToolNameRequired";
            public static readonly ErrorCode PromptNameRequired = Root / "PromptNameRequired";
            public static readonly ErrorCode ResourceUriRequired = Root / "ResourceUriRequired";
        }
    }

    public static class Request
    {
        private static readonly ErrorCode Root = new("Request");
        public static readonly ErrorCode InvalidAction = Root / "InvalidAction";
        public static readonly ErrorCode UnknownAction = Root / "UnknownAction";
        public static readonly ErrorCode Cancelled = Root / "Cancelled";
        public static readonly ErrorCode InvalidPage = Root / "InvalidPage";
        public static readonly ErrorCode InvalidPageSize = Root / "InvalidPageSize";
        public static readonly ErrorCode NotFound = Root / "NotFound";
        public static readonly ErrorCode InvalidStatus = Root / "InvalidStatus";
    }

    public static class Include
    {
        private static readonly ErrorCode Root = new("Include");
        public static readonly ErrorCode InvalidOption = Root / "InvalidOption";
        public static readonly ErrorCode InvalidInstanceOption = Root / "InvalidInstanceOption";
    }

    public static readonly ErrorCode InvalidTimezone = new("InvalidTimezone");
}
