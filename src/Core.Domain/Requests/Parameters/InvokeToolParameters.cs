using System.Text.Json;

namespace Core.Domain.Requests.Parameters;

/// <summary>
/// Parameters for the invoke-tool request action.
/// </summary>
/// <param name="ToolName">The name of the tool to invoke.</param>
/// <param name="Input">Optional input arguments for the tool.</param>
public record InvokeToolParameters(string ToolName, JsonElement? Input);
