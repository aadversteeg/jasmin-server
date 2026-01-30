using System.Collections.Concurrent;
using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Core.Infrastructure.ModelContextProtocol.Hosting;

/// <summary>
/// Manages MCP server instances, including starting and stopping servers.
/// </summary>
public class McpServerInstanceManager : IMcpServerInstanceManager, IAsyncDisposable
{
    private readonly IMcpServerRepository _repository;
    private readonly IMcpServerConnectionStatusCache _statusCache;
    private readonly ILogger<McpServerInstanceManager> _logger;
    private readonly ConcurrentDictionary<string, ManagedMcpServerInstance> _instances = new();
    private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(30);

    public McpServerInstanceManager(
        IMcpServerRepository repository,
        IMcpServerConnectionStatusCache statusCache,
        ILogger<McpServerInstanceManager> logger)
    {
        _repository = repository;
        _statusCache = statusCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<McpServerInstanceId, Error>> StartInstanceAsync(
        McpServerName serverName,
        McpServerRequestId? requestId = null,
        CancellationToken cancellationToken = default)
    {
        var serverId = _statusCache.GetOrCreateId(serverName);
        var instanceId = McpServerInstanceId.Create();

        try
        {
            // Get server definition
            var definitionResult = _repository.GetById(serverName);
            if (definitionResult.IsFailure)
            {
                _statusCache.RecordEvent(serverId, McpServerEventType.StartFailed, ToEventErrors(definitionResult.Error), instanceId, requestId);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(definitionResult.Error);
            }

            if (!definitionResult.Value.HasValue)
            {
                var error = Errors.McpServerNotFound(serverName.Value);
                _statusCache.RecordEvent(serverId, McpServerEventType.StartFailed, ToEventErrors(error), instanceId, requestId);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(error);
            }

            var definition = definitionResult.Value.Value;

            // Check if server has configuration
            if (!definition.HasConfiguration)
            {
                var error = Errors.ConfigurationMissing(serverName.Value);
                _statusCache.RecordEvent(serverId, McpServerEventType.StartFailed, ToEventErrors(error), instanceId, requestId);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(error);
            }

            // Record starting event
            _logger.LogDebug("Starting MCP server instance {InstanceId} for {ServerName}", instanceId.Value, serverName.Value);
            _statusCache.RecordEvent(serverId, McpServerEventType.Starting, null, instanceId, requestId);

            // Create transport and connect
            var transportOptions = new StdioClientTransportOptions
            {
                Command = definition.Command!,
                Arguments = [.. definition.Args!],
                Name = definition.Id.Value,
                EnvironmentVariables = definition.Env!.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value)
            };

            var transport = new StdioClientTransport(transportOptions);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_connectionTimeout);

            McpClient client;
            try
            {
                client = await McpClient.CreateAsync(transport, cancellationToken: timeoutCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to start MCP server instance {InstanceId} for {ServerName}", instanceId.Value, serverName.Value);
                _statusCache.RecordEvent(serverId, McpServerEventType.StartFailed, ToEventErrors(ex), instanceId, requestId);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(new Error(
                    ErrorCodes.ConfigFileReadError,
                    $"Failed to start MCP server '{serverName.Value}': {ex.Message}"));
            }

            // Store the instance
            var instance = new ManagedMcpServerInstance(
                instanceId,
                serverName,
                serverId,
                client,
                DateTime.UtcNow);

            _instances[instanceId.Value] = instance;

            // Record started event
            _statusCache.RecordEvent(serverId, McpServerEventType.Started, null, instanceId, requestId);
            _statusCache.SetStatus(serverId, McpServerConnectionStatus.Verified);
            _logger.LogInformation("Started MCP server instance {InstanceId} for {ServerName}", instanceId.Value, serverName.Value);

            return Result<McpServerInstanceId, Error>.Success(instanceId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Start instance cancelled for MCP server {ServerName}", serverName.Value);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting MCP server instance for {ServerName}", serverName.Value);
            _statusCache.RecordEvent(serverId, McpServerEventType.StartFailed, ToEventErrors(ex), instanceId, requestId);
            _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
            return Result<McpServerInstanceId, Error>.Failure(new Error(
                ErrorCodes.ConfigFileReadError,
                $"Error starting MCP server '{serverName.Value}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Unit, Error>> StopInstanceAsync(
        McpServerInstanceId instanceId,
        McpServerRequestId? requestId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_instances.TryRemove(instanceId.Value, out var instance))
        {
            return Result<Unit, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' not found"));
        }

        try
        {
            _logger.LogDebug("Stopping MCP server instance {InstanceId} for {ServerName}", instanceId.Value, instance.ServerName.Value);
            _statusCache.RecordEvent(instance.ServerId, McpServerEventType.Stopping, null, instanceId, requestId);

            await instance.Client.DisposeAsync();

            _statusCache.RecordEvent(instance.ServerId, McpServerEventType.Stopped, null, instanceId, requestId);
            _logger.LogInformation("Stopped MCP server instance {InstanceId} for {ServerName}", instanceId.Value, instance.ServerName.Value);

            return Result<Unit, Error>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MCP server instance {InstanceId}", instanceId.Value);
            _statusCache.RecordEvent(instance.ServerId, McpServerEventType.StopFailed, ToEventErrors(ex), instanceId, requestId);
            return Result<Unit, Error>.Failure(new Error(
                ErrorCodes.ConfigFileWriteError,
                $"Error stopping instance '{instanceId.Value}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerInstanceInfo> GetRunningInstances(McpServerName serverName)
    {
        return _instances.Values
            .Where(i => i.ServerName.Value == serverName.Value)
            .Select(i => new McpServerInstanceInfo(i.InstanceId, i.ServerName, i.StartedAtUtc))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerInstanceInfo> GetAllRunningInstances()
    {
        return _instances.Values
            .Select(i => new McpServerInstanceInfo(i.InstanceId, i.ServerName, i.StartedAtUtc))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping all MCP server instances ({Count} instances)", _instances.Count);

        var tasks = _instances.Keys
            .Select(id => StopInstanceAsync(McpServerInstanceId.From(id), null, cancellationToken))
            .ToList();

        await Task.WhenAll(tasks);

        _logger.LogInformation("All MCP server instances stopped");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAllAsync();
    }

    private static IReadOnlyList<McpServerEventError> ToEventErrors(Error error)
    {
        return [new McpServerEventError(error.Code.Value, error.Message)];
    }

    private static IReadOnlyList<McpServerEventError> ToEventErrors(Exception ex)
    {
        return [new McpServerEventError(ex.GetType().Name, ex.Message)];
    }

    /// <summary>
    /// Internal class to track a managed MCP server instance.
    /// </summary>
    private sealed class ManagedMcpServerInstance
    {
        public McpServerInstanceId InstanceId { get; }
        public McpServerName ServerName { get; }
        public McpServerId ServerId { get; }
        public McpClient Client { get; }
        public DateTime StartedAtUtc { get; }

        public ManagedMcpServerInstance(
            McpServerInstanceId instanceId,
            McpServerName serverName,
            McpServerId serverId,
            McpClient client,
            DateTime startedAtUtc)
        {
            InstanceId = instanceId;
            ServerName = serverName;
            ServerId = serverId;
            Client = client;
            StartedAtUtc = startedAtUtc;
        }
    }
}
