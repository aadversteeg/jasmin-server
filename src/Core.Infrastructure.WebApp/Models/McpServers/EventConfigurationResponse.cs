namespace Core.Infrastructure.WebApp.Models.McpServers;

/// <summary>
/// Response model for configuration data in an event.
/// </summary>
public record EventConfigurationResponse(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
