using System.Text.Json;

namespace Core.Domain.Events.Payloads;

/// <summary>
/// Payload for tool invocation events.
/// </summary>
/// <param name="ToolName">The name of the tool being invoked.</param>
/// <param name="Input">The input arguments for the tool.</param>
/// <param name="Output">The output from the tool invocation (null for invoking/failed events).</param>
public record ToolInvocationPayload(
    string ToolName,
    JsonElement? Input,
    JsonElement? Output);
