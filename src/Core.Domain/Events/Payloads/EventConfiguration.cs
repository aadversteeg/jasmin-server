namespace Core.Domain.Events.Payloads;

/// <summary>
/// Represents server configuration data captured in an event payload.
/// </summary>
/// <param name="Command">The command to execute.</param>
/// <param name="Args">Command line arguments.</param>
/// <param name="Env">Environment variables.</param>
public record EventConfiguration(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
