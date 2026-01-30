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

            // Record starting event with configuration
            _logger.LogDebug("Starting MCP server instance {InstanceId} for {ServerName}", instanceId.Value, serverName.Value);
            var startConfig = McpServerEventConfiguration.FromDefinition(definition);
            _statusCache.RecordEvent(serverId, McpServerEventType.Starting, null, instanceId, requestId, configuration: startConfig);

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
                DateTime.UtcNow,
                startConfig);

            _instances[instanceId.Value] = instance;

            // Record started event
            _statusCache.RecordEvent(serverId, McpServerEventType.Started, null, instanceId, requestId);
            _statusCache.SetStatus(serverId, McpServerConnectionStatus.Verified);
            _logger.LogInformation("Started MCP server instance {InstanceId} for {ServerName}", instanceId.Value, serverName.Value);

            // Retrieve and cache metadata
            await RetrieveAndCacheMetadataAsync(client, serverId, instanceId, requestId, cancellationToken);

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
            .Select(i => new McpServerInstanceInfo(i.InstanceId, i.ServerName, i.StartedAtUtc, i.Configuration))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public McpServerInstanceInfo? GetInstance(McpServerName serverName, McpServerInstanceId instanceId)
    {
        if (_instances.TryGetValue(instanceId.Value, out var instance) &&
            instance.ServerName.Value == serverName.Value)
        {
            return new McpServerInstanceInfo(instance.InstanceId, instance.ServerName, instance.StartedAtUtc, instance.Configuration);
        }

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerInstanceInfo> GetAllRunningInstances()
    {
        return _instances.Values
            .Select(i => new McpServerInstanceInfo(i.InstanceId, i.ServerName, i.StartedAtUtc, i.Configuration))
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

    private async Task RetrieveAndCacheMetadataAsync(
        McpClient client,
        McpServerId serverId,
        McpServerInstanceId instanceId,
        McpServerRequestId? requestId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving metadata for MCP server {ServerId}", serverId.Value);
        _statusCache.RecordEvent(serverId, McpServerEventType.MetadataRetrieving, null, instanceId, requestId);

        var errors = new List<McpServerMetadataError>();
        IReadOnlyList<McpTool>? tools = null;
        IReadOnlyList<McpPrompt>? prompts = null;
        IReadOnlyList<McpResource>? resources = null;

        // Retrieve tools
        try
        {
            var mcpTools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            tools = mcpTools.Select(t => new McpTool(
                t.Name,
                t.Title,
                t.Description,
                t.ProtocolTool.InputSchema.ToString())).ToList().AsReadOnly();
            _logger.LogDebug("Retrieved {Count} tools from MCP server {ServerId}", tools.Count, serverId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve tools from MCP server {ServerId}", serverId.Value);
            errors.Add(new McpServerMetadataError("Tools", ex.Message));
        }

        // Retrieve prompts
        try
        {
            var mcpPrompts = await client.ListPromptsAsync(cancellationToken: cancellationToken);
            prompts = mcpPrompts.Select(p => new McpPrompt(
                p.Name,
                p.Title,
                p.Description,
                p.ProtocolPrompt.Arguments?.Select(a => new McpPromptArgument(
                    a.Name,
                    a.Description,
                    a.Required ?? false)).ToList().AsReadOnly())).ToList().AsReadOnly();
            _logger.LogDebug("Retrieved {Count} prompts from MCP server {ServerId}", prompts.Count, serverId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve prompts from MCP server {ServerId}", serverId.Value);
            errors.Add(new McpServerMetadataError("Prompts", ex.Message));
        }

        // Retrieve resources
        try
        {
            var mcpResources = await client.ListResourcesAsync(cancellationToken: cancellationToken);
            resources = mcpResources.Select(r => new McpResource(
                r.Name,
                r.Uri,
                r.Title,
                r.Description,
                r.MimeType)).ToList().AsReadOnly();
            _logger.LogDebug("Retrieved {Count} resources from MCP server {ServerId}", resources.Count, serverId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve resources from MCP server {ServerId}", serverId.Value);
            errors.Add(new McpServerMetadataError("Resources", ex.Message));
        }

        // Create and cache metadata
        var metadata = new McpServerMetadata(
            tools,
            prompts,
            resources,
            DateTime.UtcNow,
            errors.Count > 0 ? errors.AsReadOnly() : null);

        _statusCache.SetMetadata(serverId, metadata);

        // Record completion event
        if (errors.Count == 0)
        {
            _logger.LogDebug("Metadata retrieval completed successfully for MCP server {ServerId}", serverId.Value);
            _statusCache.RecordEvent(serverId, McpServerEventType.MetadataRetrieved, null, instanceId, requestId);
        }
        else
        {
            _logger.LogWarning("Metadata retrieval completed with {ErrorCount} errors for MCP server {ServerId}", errors.Count, serverId.Value);
            var eventErrors = errors.Select(e => new McpServerEventError(e.Category, e.ErrorMessage)).ToList();
            _statusCache.RecordEvent(serverId, McpServerEventType.MetadataRetrievalFailed, eventErrors, instanceId, requestId);
        }
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
        public McpServerEventConfiguration? Configuration { get; }

        public ManagedMcpServerInstance(
            McpServerInstanceId instanceId,
            McpServerName serverName,
            McpServerId serverId,
            McpClient client,
            DateTime startedAtUtc,
            McpServerEventConfiguration? configuration)
        {
            InstanceId = instanceId;
            ServerName = serverName;
            ServerId = serverId;
            Client = client;
            StartedAtUtc = startedAtUtc;
            Configuration = configuration;
        }
    }
}
