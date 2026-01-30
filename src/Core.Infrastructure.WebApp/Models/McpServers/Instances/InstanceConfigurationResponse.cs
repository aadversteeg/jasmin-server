namespace Core.Infrastructure.WebApp.Models.McpServers.Instances;

/// <summary>
/// Response model for instance configuration.
/// </summary>
/// <param name="Command">The command used to start the instance.</param>
/// <param name="Args">The command line arguments.</param>
/// <param name="Env">The environment variables.</param>
public record InstanceConfigurationResponse(
    string Command,
    IReadOnlyList<string> Args,
    IReadOnlyDictionary<string, string> Env);
