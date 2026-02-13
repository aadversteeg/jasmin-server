namespace Core.Domain.Events;

/// <summary>
/// Well-known event type catalog using nested static classes.
/// Use dot-separated paths: <c>EventTypes.McpServer.Instance.Started</c> produces <c>"mcp-server.instance.started"</c>.
/// </summary>
public static class EventTypes
{
    private static readonly EventType _ = new("mcp-server");

    /// <summary>
    /// Events for MCP servers (<c>mcp-server.*</c>).
    /// </summary>
    public static class McpServer
    {
        /// <summary>MCP server created (<c>mcp-server.created</c>).</summary>
        public static readonly EventType Created = _ / "created";

        /// <summary>MCP server deleted (<c>mcp-server.deleted</c>).</summary>
        public static readonly EventType Deleted = _ / "deleted";

        /// <summary>
        /// Configuration events (<c>mcp-server.configuration.*</c>).
        /// </summary>
        public static class Configuration
        {
            private static readonly EventType _configuration = _ / "configuration";

            /// <summary>Configuration created (<c>mcp-server.configuration.created</c>).</summary>
            public static readonly EventType Created = _configuration / "created";

            /// <summary>Configuration updated (<c>mcp-server.configuration.updated</c>).</summary>
            public static readonly EventType Updated = _configuration / "updated";

            /// <summary>Configuration deleted (<c>mcp-server.configuration.deleted</c>).</summary>
            public static readonly EventType Deleted = _configuration / "deleted";
        }

        /// <summary>
        /// Instance lifecycle events (<c>mcp-server.instance.*</c>).
        /// </summary>
        public static class Instance
        {
            private static readonly EventType _instance = _ / "instance";

            /// <summary>Instance starting (<c>mcp-server.instance.starting</c>).</summary>
            public static readonly EventType Starting = _instance / "starting";

            /// <summary>Instance started (<c>mcp-server.instance.started</c>).</summary>
            public static readonly EventType Started = _instance / "started";

            /// <summary>Instance start failed (<c>mcp-server.instance.start-failed</c>).</summary>
            public static readonly EventType StartFailed = _instance / "start-failed";

            /// <summary>Instance stopping (<c>mcp-server.instance.stopping</c>).</summary>
            public static readonly EventType Stopping = _instance / "stopping";

            /// <summary>Instance stopped (<c>mcp-server.instance.stopped</c>).</summary>
            public static readonly EventType Stopped = _instance / "stopped";

            /// <summary>Instance stop failed (<c>mcp-server.instance.stop-failed</c>).</summary>
            public static readonly EventType StopFailed = _instance / "stop-failed";
        }

        /// <summary>
        /// Metadata retrieval events (<c>mcp-server.metadata.*</c>).
        /// </summary>
        public static class Metadata
        {
            private static readonly EventType _metadata = _ / "metadata";

            /// <summary>
            /// Tool metadata events (<c>mcp-server.metadata.tools.*</c>).
            /// </summary>
            public static class Tools
            {
                private static readonly EventType _tools = _metadata / "tools";

                /// <summary>Tools retrieving (<c>mcp-server.metadata.tools.retrieving</c>).</summary>
                public static readonly EventType Retrieving = _tools / "retrieving";

                /// <summary>Tools retrieved (<c>mcp-server.metadata.tools.retrieved</c>).</summary>
                public static readonly EventType Retrieved = _tools / "retrieved";

                /// <summary>Tools retrieval failed (<c>mcp-server.metadata.tools.retrieval-failed</c>).</summary>
                public static readonly EventType RetrievalFailed = _tools / "retrieval-failed";
            }

            /// <summary>
            /// Prompt metadata events (<c>mcp-server.metadata.prompts.*</c>).
            /// </summary>
            public static class Prompts
            {
                private static readonly EventType _prompts = _metadata / "prompts";

                /// <summary>Prompts retrieving (<c>mcp-server.metadata.prompts.retrieving</c>).</summary>
                public static readonly EventType Retrieving = _prompts / "retrieving";

                /// <summary>Prompts retrieved (<c>mcp-server.metadata.prompts.retrieved</c>).</summary>
                public static readonly EventType Retrieved = _prompts / "retrieved";

                /// <summary>Prompts retrieval failed (<c>mcp-server.metadata.prompts.retrieval-failed</c>).</summary>
                public static readonly EventType RetrievalFailed = _prompts / "retrieval-failed";
            }

            /// <summary>
            /// Resource metadata events (<c>mcp-server.metadata.resources.*</c>).
            /// </summary>
            public static class Resources
            {
                private static readonly EventType _resources = _metadata / "resources";

                /// <summary>Resources retrieving (<c>mcp-server.metadata.resources.retrieving</c>).</summary>
                public static readonly EventType Retrieving = _resources / "retrieving";

                /// <summary>Resources retrieved (<c>mcp-server.metadata.resources.retrieved</c>).</summary>
                public static readonly EventType Retrieved = _resources / "retrieved";

                /// <summary>Resources retrieval failed (<c>mcp-server.metadata.resources.retrieval-failed</c>).</summary>
                public static readonly EventType RetrievalFailed = _resources / "retrieval-failed";
            }
        }

        /// <summary>
        /// Tool invocation events (<c>mcp-server.tool-invocation.*</c>).
        /// </summary>
        public static class ToolInvocation
        {
            private static readonly EventType _toolInvocation = _ / "tool-invocation";

            /// <summary>Tool invocation accepted (<c>mcp-server.tool-invocation.accepted</c>).</summary>
            public static readonly EventType Accepted = _toolInvocation / "accepted";

            /// <summary>Tool invoking (<c>mcp-server.tool-invocation.invoking</c>).</summary>
            public static readonly EventType Invoking = _toolInvocation / "invoking";

            /// <summary>Tool invoked (<c>mcp-server.tool-invocation.invoked</c>).</summary>
            public static readonly EventType Invoked = _toolInvocation / "invoked";

            /// <summary>Tool invocation failed (<c>mcp-server.tool-invocation.failed</c>).</summary>
            public static readonly EventType Failed = _toolInvocation / "failed";
        }
    }
}
