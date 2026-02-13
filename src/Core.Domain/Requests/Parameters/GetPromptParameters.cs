using System.Text.Json;

namespace Core.Domain.Requests.Parameters;

/// <summary>
/// Parameters for the get-prompt request action.
/// </summary>
/// <param name="PromptName">The name of the prompt to get.</param>
/// <param name="Arguments">Optional arguments for the prompt.</param>
public record GetPromptParameters(string PromptName, JsonElement? Arguments);
