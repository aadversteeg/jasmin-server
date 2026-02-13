namespace Core.Domain.Requests;

/// <summary>
/// Well-known request action catalog using nested static classes.
/// Use the <c>_</c> root and <c>/</c> operator for composition:
/// <c>RequestActions.McpServer.Instance.InvokeTool</c> produces <c>"mcp-server.instance.invoke-tool"</c>.
/// </summary>
public static class RequestActions
{
    private static readonly RequestAction _ = new("mcp-server");

    /// <summary>
    /// Actions targeting MCP servers (<c>mcp-server.*</c>).
    /// </summary>
    public static class McpServer
    {
        /// <summary>Start an MCP server (<c>mcp-server.start</c>).</summary>
        public static readonly RequestAction Start = _ / "start";

        /// <summary>Test an MCP server configuration (<c>mcp-server.test-configuration</c>).</summary>
        public static readonly RequestAction TestConfiguration = _ / "test-configuration";

        /// <summary>
        /// Actions targeting MCP server instances (<c>mcp-server.instance.*</c>).
        /// </summary>
        public static class Instance
        {
            private static readonly RequestAction _instance = _ / "instance";

            /// <summary>Stop an MCP server instance (<c>mcp-server.instance.stop</c>).</summary>
            public static readonly RequestAction Stop = _instance / "stop";

            /// <summary>Invoke a tool on an MCP server instance (<c>mcp-server.instance.invoke-tool</c>).</summary>
            public static readonly RequestAction InvokeTool = _instance / "invoke-tool";

            /// <summary>Get a prompt from an MCP server instance (<c>mcp-server.instance.get-prompt</c>).</summary>
            public static readonly RequestAction GetPrompt = _instance / "get-prompt";

            /// <summary>Read a resource from an MCP server instance (<c>mcp-server.instance.read-resource</c>).</summary>
            public static readonly RequestAction ReadResource = _instance / "read-resource";

            /// <summary>Refresh metadata of an MCP server instance (<c>mcp-server.instance.refresh-metadata</c>).</summary>
            public static readonly RequestAction RefreshMetadata = _instance / "refresh-metadata";
        }
    }
}
