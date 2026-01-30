using System.Text.Json;

namespace Core.Domain.McpServers;

/// <summary>
/// Data associated with tool invocation events.
/// </summary>
/// <param name="ToolName">The name of the tool being invoked.</param>
/// <param name="Input">The input arguments for the tool.</param>
/// <param name="Output">The output from the tool invocation (null for ToolInvoking and ToolInvocationFailed events).</param>
public record McpServerToolInvocationEventData(
    string ToolName,
    JsonElement? Input,
    JsonElement? Output);
