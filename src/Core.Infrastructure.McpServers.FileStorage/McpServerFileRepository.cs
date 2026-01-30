using System.Text.Json;
using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;

namespace Core.Infrastructure.McpServers.FileStorage;

/// <summary>
/// Thread-safe repository implementation that manages MCP server configurations in a JSON file.
/// </summary>
public class McpServerFileRepository : IMcpServerRepository
{
    private readonly string _configFilePath;
    private readonly ReaderWriterLockSlim _lock = new();

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true
    };

    public McpServerFileRepository(string configFilePath)
    {
        _configFilePath = configFilePath ?? throw new ArgumentNullException(nameof(configFilePath));
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<McpServerInfo>, Error> GetAll()
    {
        _lock.EnterReadLock();
        try
        {
            return ReadConfigFileInternal()
                .OnSuccessMap(config => config.McpServers
                    .Select(kvp => CreateServerInfo(kvp.Key, kvp.Value))
                    .Where(result => result.IsSuccess)
                    .Select(result => result.Value)
                    .OrderBy(info => info.Id.Value)
                    .ToList() as IReadOnlyList<McpServerInfo>);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Result<Maybe<McpServerDefinition>, Error> GetById(McpServerId id)
    {
        _lock.EnterReadLock();
        try
        {
            return ReadConfigFileInternal()
                .OnSuccessMap(config =>
                {
                    if (!config.McpServers.TryGetValue(id.Value, out var entry))
                    {
                        return Maybe<McpServerDefinition>.None;
                    }

                    return Maybe.From(CreateDefinition(id, entry));
                });
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Create(McpServerDefinition definition)
    {
        _lock.EnterWriteLock();
        try
        {
            var configResult = ReadConfigFileInternal();
            if (configResult.IsFailure)
            {
                return Result<McpServerDefinition, Error>.Failure(configResult.Error);
            }

            var config = configResult.Value;

            if (config.McpServers.ContainsKey(definition.Id.Value))
            {
                return Result<McpServerDefinition, Error>.Failure(
                    Errors.DuplicateMcpServerId(definition.Id.Value));
            }

            config.McpServers[definition.Id.Value] = CreateConfigEntry(definition);

            var writeResult = WriteConfigFileInternal(config);
            if (writeResult.IsFailure)
            {
                return Result<McpServerDefinition, Error>.Failure(writeResult.Error);
            }

            return Result<McpServerDefinition, Error>.Success(definition);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Update(McpServerDefinition definition)
    {
        _lock.EnterWriteLock();
        try
        {
            var configResult = ReadConfigFileInternal();
            if (configResult.IsFailure)
            {
                return Result<McpServerDefinition, Error>.Failure(configResult.Error);
            }

            var config = configResult.Value;

            if (!config.McpServers.ContainsKey(definition.Id.Value))
            {
                return Result<McpServerDefinition, Error>.Failure(
                    Errors.McpServerNotFound(definition.Id.Value));
            }

            config.McpServers[definition.Id.Value] = CreateConfigEntry(definition);

            var writeResult = WriteConfigFileInternal(config);
            if (writeResult.IsFailure)
            {
                return Result<McpServerDefinition, Error>.Failure(writeResult.Error);
            }

            return Result<McpServerDefinition, Error>.Success(definition);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public Result<Unit, Error> Delete(McpServerId id)
    {
        _lock.EnterWriteLock();
        try
        {
            var configResult = ReadConfigFileInternal();
            if (configResult.IsFailure)
            {
                return Result<Unit, Error>.Failure(configResult.Error);
            }

            var config = configResult.Value;

            if (!config.McpServers.Remove(id.Value))
            {
                return Result<Unit, Error>.Failure(Errors.McpServerNotFound(id.Value));
            }

            var writeResult = WriteConfigFileInternal(config);
            if (writeResult.IsFailure)
            {
                return Result<Unit, Error>.Failure(writeResult.Error);
            }

            return Result<Unit, Error>.Success(Unit.Value);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private Result<McpServerConfigFile, Error> ReadConfigFileInternal()
    {
        var expandedPath = ExpandPath(_configFilePath);

        if (!File.Exists(expandedPath))
        {
            return Result<McpServerConfigFile, Error>.Failure(Errors.ConfigFileNotFound(expandedPath));
        }

        try
        {
            var json = File.ReadAllText(expandedPath);
            var config = JsonSerializer.Deserialize<McpServerConfigFile>(json, JsonReadOptions);

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

    private Result<Unit, Error> WriteConfigFileInternal(McpServerConfigFile config)
    {
        var expandedPath = ExpandPath(_configFilePath);
        var tempPath = expandedPath + ".tmp";

        try
        {
            var json = JsonSerializer.Serialize(config, JsonWriteOptions);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, expandedPath, overwrite: true);
            return Result<Unit, Error>.Success(Unit.Value);
        }
        catch (IOException ex)
        {
            // Clean up temp file if it exists
            try { File.Delete(tempPath); } catch { /* ignore */ }
            return Result<Unit, Error>.Failure(Errors.ConfigFileWriteError(expandedPath, ex.Message));
        }
    }

    private static McpServerDefinition CreateDefinition(McpServerId id, McpServerConfigEntry entry)
    {
        return new McpServerDefinition(
            id,
            entry.Command,
            (entry.Args ?? []).AsReadOnly(),
            (entry.Env ?? new Dictionary<string, string>()).AsReadOnly());
    }

    private static McpServerConfigEntry CreateConfigEntry(McpServerDefinition definition)
    {
        return new McpServerConfigEntry
        {
            Command = definition.Command,
            Args = definition.Args.ToList(),
            Env = definition.Env.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
        };
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
