using System.Text.Json;
using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;

namespace Core.Infrastructure.McpServers.FileStorage;

/// <summary>
/// Repository implementation that reads MCP server configurations from a JSON file.
/// </summary>
public class McpServerFileRepository : IMcpServerRepository
{
    private readonly string _configFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public McpServerFileRepository(string configFilePath)
    {
        _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<McpServerInfo>, Error> GetAll()
    {
        return ReadConfigFile()
            .OnSuccessMap(config => config.McpServers
                .Select(kvp => CreateServerInfo(kvp.Key, kvp.Value))
                .Where(result => result.IsSuccess)
                .Select(result => result.Value)
                .OrderBy(info => info.Id.Value)
                .ToList() as IReadOnlyList<McpServerInfo>);
    }

    /// <inheritdoc />
    public Result<Maybe<McpServerDefinition>, Error> GetById(McpServerId id)
    {
        return ReadConfigFile()
            .OnSuccessMap(config =>
            {
                if (!config.McpServers.TryGetValue(id.Value, out var entry))
                {
                    return Maybe<McpServerDefinition>.None;
                }

                return Maybe.From(new McpServerDefinition(
                    id,
                    entry.Command,
                    (entry.Args ?? []).AsReadOnly(),
                    (entry.Env ?? new Dictionary<string, string>()).AsReadOnly()));
            });
    }

    private Result<McpServerConfigFile, Error> ReadConfigFile()
    {
        var expandedPath = ExpandPath(_configFilePath);

        if (!File.Exists(expandedPath))
        {
            return Result<McpServerConfigFile, Error>.Failure(Errors.ConfigFileNotFound(expandedPath));
        }

        try
        {
            var json = File.ReadAllText(expandedPath);
            var config = JsonSerializer.Deserialize<McpServerConfigFile>(json, JsonOptions);

            if (config == null)
            {
                return Result<McpServerConfigFile, Error>.Failure(Errors.ConfigFileInvalid(expandedPath));
            }

            return Result<McpServerConfigFile, Error>.Success(config);
        }
        catch (JsonException)
        {
            return Result<McpServerConfigFile, Error>.Failure(Errors.ConfigFileInvalid(expandedPath));
        }
        catch (IOException ex)
        {
            return Result<McpServerConfigFile, Error>.Failure(Errors.ConfigFileReadError(expandedPath, ex.Message));
        }
    }

    private static Result<McpServerInfo, Error> CreateServerInfo(string name, McpServerConfigEntry entry)
    {
        return McpServerId.Create(name)
            .OnSuccessMap(id => new McpServerInfo(id, entry.Command));
    }

    private static string ExpandPath(string path)
    {
        if (path.StartsWith("~/"))
        {
            var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(homeDirectory, path[2..]);
        }
        return path;
    }
}
