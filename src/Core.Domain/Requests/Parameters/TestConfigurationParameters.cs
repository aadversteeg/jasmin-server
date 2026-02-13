namespace Core.Domain.Requests.Parameters;

/// <summary>
/// Parameters for the test-configuration request action.
/// </summary>
/// <param name="Command">The command to execute.</param>
/// <param name="Args">Optional command arguments.</param>
/// <param name="Env">Optional environment variables.</param>
public record TestConfigurationParameters(
    string Command,
    IReadOnlyList<string>? Args,
    IReadOnlyDictionary<string, string>? Env);
