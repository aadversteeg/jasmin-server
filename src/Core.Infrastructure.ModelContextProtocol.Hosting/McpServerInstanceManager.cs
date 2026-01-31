using System.Collections.Concurrent;
using System.Text.Json;
using Ave.Extensions.Functional;
using Core.Application.Events;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Core.Infrastructure.ModelContextProtocol.Hosting;

/// <summary>
/// Manages MCP server instances, including starting and stopping servers.
/// </summary>
public class McpServerInstanceManager : IMcpServerInstanceManager, IAsyncDisposable
{
    private readonly IMcpServerRepository _repository;
    private readonly IMcpServerConnectionStatusCache _statusCache;
    private readonly IEventPublisher<McpServerEvent> _eventPublisher;
    private readonly ILogger<McpServerInstanceManager> _logger;
    private readonly ConcurrentDictionary<string, ManagedMcpServerInstance> _instances = new();
    private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(30);

    public McpServerInstanceManager(
        IMcpServerRepository repository,
        IMcpServerConnectionStatusCache statusCache,
        IEventPublisher<McpServerEvent> eventPublisher,
        ILogger<McpServerInstanceManager> logger)
    {
        _repository = repository;
        _statusCache = statusCache;
        _eventPublisher = eventPublisher;
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

        // Record starting event immediately
        _logger.LogDebug("Starting MCP server instance {InstanceId} for {ServerName}", instanceId.Value, serverName.Value);
        PublishEvent(serverName, McpServerEventType.Starting, instanceId: instanceId, requestId: requestId);

        try
        {
            // Get server definition
            var definitionResult = _repository.GetById(serverName);
            if (definitionResult.IsFailure)
            {
                PublishEvent(serverName, McpServerEventType.StartFailed, ToEventErrors(definitionResult.Error), instanceId, requestId);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(definitionResult.Error);
            }

            if (!definitionResult.Value.HasValue)
            {
                var error = Errors.McpServerNotFound(serverName.Value);
                PublishEvent(serverName, McpServerEventType.StartFailed, ToEventErrors(error), instanceId, requestId);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(error);
            }

            var definition = definitionResult.Value.Value;

            // Check if server has configuration
            if (!definition.HasConfiguration)
            {
                var error = Errors.ConfigurationMissing(serverName.Value);
                PublishEvent(serverName, McpServerEventType.StartFailed, ToEventErrors(error), instanceId, requestId);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(error);
            }

            var startConfig = McpServerEventConfiguration.FromDefinition(definition);

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
                PublishEvent(serverName, McpServerEventType.StartFailed, ToEventErrors(ex), instanceId, requestId);
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

            // Record started event with configuration
            PublishEvent(serverName, McpServerEventType.Started, instanceId: instanceId, requestId: requestId, configuration: startConfig);
            _statusCache.SetStatus(serverId, McpServerConnectionStatus.Verified);
            _logger.LogInformation("Started MCP server instance {InstanceId} for {ServerName}", instanceId.Value, serverName.Value);

            // Retrieve metadata and store with instance
            var metadata = await RetrieveMetadataAsync(client, serverName, instanceId, requestId, cancellationToken);
            instance.Metadata = metadata;

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
            PublishEvent(serverName, McpServerEventType.StartFailed, ToEventErrors(ex), instanceId, requestId);
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
            PublishEvent(instance.ServerName, McpServerEventType.Stopping, instanceId: instanceId, requestId: requestId);

            await instance.Client.DisposeAsync();

            PublishEvent(instance.ServerName, McpServerEventType.Stopped, instanceId: instanceId, requestId: requestId);
            _logger.LogInformation("Stopped MCP server instance {InstanceId} for {ServerName}", instanceId.Value, instance.ServerName.Value);

            return Result<Unit, Error>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MCP server instance {InstanceId}", instanceId.Value);
            PublishEvent(instance.ServerName, McpServerEventType.StopFailed, ToEventErrors(ex), instanceId, requestId);
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
            .Select(i => new McpServerInstanceInfo(i.InstanceId, i.ServerName, i.StartedAtUtc, i.Configuration, i.Metadata))
            .ToList()
            .AsReadOnly();
    }

    /// <inheritdoc />
    public McpServerInstanceInfo? GetInstance(McpServerName serverName, McpServerInstanceId instanceId)
    {
        if (_instances.TryGetValue(instanceId.Value, out var instance) &&
            instance.ServerName.Value == serverName.Value)
        {
            return new McpServerInstanceInfo(instance.InstanceId, instance.ServerName, instance.StartedAtUtc, instance.Configuration, instance.Metadata);
        }

        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerInstanceInfo> GetAllRunningInstances()
    {
        return _instances.Values
            .Select(i => new McpServerInstanceInfo(i.InstanceId, i.ServerName, i.StartedAtUtc, i.Configuration, i.Metadata))
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
    public async Task<Result<McpToolInvocationResult, Error>> InvokeToolAsync(
        McpServerName serverName,
        McpServerInstanceId instanceId,
        string toolName,
        IReadOnlyDictionary<string, object?>? arguments,
        McpServerRequestId? requestId = null,
        CancellationToken cancellationToken = default)
    {
        // Get instance from dictionary, validate it exists
        if (!_instances.TryGetValue(instanceId.Value, out var instance))
        {
            return Result<McpToolInvocationResult, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' not found"));
        }

        if (instance.ServerName.Value != serverName.Value)
        {
            return Result<McpToolInvocationResult, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' does not belong to server '{serverName.Value}'"));
        }

        // Record ToolInvoking event
        _logger.LogDebug("Invoking tool {ToolName} on instance {InstanceId}", toolName, instanceId.Value);
        var inputJson = arguments != null ? JsonSerializer.SerializeToElement(arguments) : (JsonElement?)null;
        PublishEvent(serverName, McpServerEventType.ToolInvoking,
            instanceId: instanceId, requestId: requestId,
            toolInvocationData: new McpServerToolInvocationEventData(toolName, inputJson, null));

        try
        {
            // Call client.CallToolAsync
            var sdkResult = await instance.Client.CallToolAsync(
                toolName, arguments, cancellationToken: cancellationToken);

            // Map SDK result to domain model
            var result = MapToToolInvocationResult(sdkResult);

            // Record ToolInvoked event
            var outputJson = JsonSerializer.SerializeToElement(result);
            PublishEvent(serverName, McpServerEventType.ToolInvoked,
                instanceId: instanceId, requestId: requestId,
                toolInvocationData: new McpServerToolInvocationEventData(toolName, inputJson, outputJson));

            _logger.LogInformation("Tool {ToolName} invoked successfully on instance {InstanceId}", toolName, instanceId.Value);
            return Result<McpToolInvocationResult, Error>.Success(result);
        }
        catch (McpException ex)
        {
            // Record ToolInvocationFailed event (MCP protocol error)
            _logger.LogWarning(ex, "Tool invocation failed for {ToolName} on instance {InstanceId}: MCP error", toolName, instanceId.Value);
            PublishEvent(serverName, McpServerEventType.ToolInvocationFailed,
                errors: ToEventErrors(ex),
                instanceId: instanceId, requestId: requestId,
                toolInvocationData: new McpServerToolInvocationEventData(toolName, inputJson, null));

            return Result<McpToolInvocationResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Tool invocation failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            // Record ToolInvocationFailed event (unexpected error)
            _logger.LogError(ex, "Tool invocation failed for {ToolName} on instance {InstanceId}", toolName, instanceId.Value);
            PublishEvent(serverName, McpServerEventType.ToolInvocationFailed,
                errors: ToEventErrors(ex),
                instanceId: instanceId, requestId: requestId,
                toolInvocationData: new McpServerToolInvocationEventData(toolName, inputJson, null));

            return Result<McpToolInvocationResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Tool invocation failed: {ex.Message}"));
        }
    }

    private static McpToolInvocationResult MapToToolInvocationResult(CallToolResult sdkResult)
    {
        var content = sdkResult.Content
            .Select(MapContentBlock)
            .ToList()
            .AsReadOnly();

        return new McpToolInvocationResult(
            content,
            sdkResult.StructuredContent != null
                ? JsonSerializer.SerializeToElement(sdkResult.StructuredContent)
                : null,
            sdkResult.IsError ?? false);
    }

    private static McpToolContentBlock MapContentBlock(ContentBlock contentBlock)
    {
        return contentBlock switch
        {
            TextContentBlock text => new McpToolContentBlock(
                text.Type,
                text.Text,
                null,
                null,
                null),
            ImageContentBlock image => new McpToolContentBlock(
                image.Type,
                null,
                image.MimeType,
                image.Data,
                null),
            AudioContentBlock audio => new McpToolContentBlock(
                audio.Type,
                null,
                audio.MimeType,
                audio.Data,
                null),
            EmbeddedResourceBlock embedded => new McpToolContentBlock(
                embedded.Type,
                embedded.Resource is TextResourceContents textResource ? textResource.Text : null,
                embedded.Resource.MimeType,
                embedded.Resource is BlobResourceContents blobResource ? blobResource.Blob : null,
                embedded.Resource.Uri),
            ResourceLinkBlock resourceLink => new McpToolContentBlock(
                resourceLink.Type,
                null,
                resourceLink.MimeType,
                null,
                resourceLink.Uri),
            _ => new McpToolContentBlock(
                contentBlock.Type,
                null,
                null,
                null,
                null)
        };
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAllAsync();
    }

    private async Task<McpServerMetadata> RetrieveMetadataAsync(
        McpClient client,
        McpServerName serverName,
        McpServerInstanceId instanceId,
        McpServerRequestId? requestId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving metadata for MCP server {ServerName}", serverName.Value);

        var errors = new List<McpServerMetadataError>();
        IReadOnlyList<McpTool>? tools = null;
        IReadOnlyList<McpPrompt>? prompts = null;
        IReadOnlyList<McpResource>? resources = null;

        // Retrieve tools
        PublishEvent(serverName, McpServerEventType.ToolsRetrieving, instanceId: instanceId, requestId: requestId);
        try
        {
            var mcpTools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            tools = mcpTools.Select(t => new McpTool(
                t.Name,
                t.Title,
                t.Description,
                t.ProtocolTool.InputSchema.ToString())).ToList().AsReadOnly();
            _logger.LogDebug("Retrieved {Count} tools from MCP server {ServerName}", tools.Count, serverName.Value);
            PublishEvent(serverName, McpServerEventType.ToolsRetrieved, instanceId: instanceId, requestId: requestId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve tools from MCP server {ServerName}", serverName.Value);
            errors.Add(new McpServerMetadataError("Tools", ex.Message));
            PublishEvent(serverName, McpServerEventType.ToolsRetrievalFailed,
                [new McpServerEventError("Tools", ex.Message)], instanceId, requestId);
        }

        // Retrieve prompts
        PublishEvent(serverName, McpServerEventType.PromptsRetrieving, instanceId: instanceId, requestId: requestId);
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
            _logger.LogDebug("Retrieved {Count} prompts from MCP server {ServerName}", prompts.Count, serverName.Value);
            PublishEvent(serverName, McpServerEventType.PromptsRetrieved, instanceId: instanceId, requestId: requestId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve prompts from MCP server {ServerName}", serverName.Value);
            errors.Add(new McpServerMetadataError("Prompts", ex.Message));
            PublishEvent(serverName, McpServerEventType.PromptsRetrievalFailed,
                [new McpServerEventError("Prompts", ex.Message)], instanceId, requestId);
        }

        // Retrieve resources
        PublishEvent(serverName, McpServerEventType.ResourcesRetrieving, instanceId: instanceId, requestId: requestId);
        try
        {
            var mcpResources = await client.ListResourcesAsync(cancellationToken: cancellationToken);
            resources = mcpResources.Select(r => new McpResource(
                r.Name,
                r.Uri,
                r.Title,
                r.Description,
                r.MimeType)).ToList().AsReadOnly();
            _logger.LogDebug("Retrieved {Count} resources from MCP server {ServerName}", resources.Count, serverName.Value);
            PublishEvent(serverName, McpServerEventType.ResourcesRetrieved, instanceId: instanceId, requestId: requestId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve resources from MCP server {ServerName}", serverName.Value);
            errors.Add(new McpServerMetadataError("Resources", ex.Message));
            PublishEvent(serverName, McpServerEventType.ResourcesRetrievalFailed,
                [new McpServerEventError("Resources", ex.Message)], instanceId, requestId);
        }

        // Create metadata
        var metadata = new McpServerMetadata(
            tools,
            prompts,
            resources,
            DateTime.UtcNow,
            errors.Count > 0 ? errors.AsReadOnly() : null);

        if (errors.Count > 0)
        {
            _logger.LogWarning("Metadata retrieval completed with {ErrorCount} errors for MCP server {ServerName}", errors.Count, serverName.Value);
        }
        else
        {
            _logger.LogDebug("Metadata retrieval completed successfully for MCP server {ServerName}", serverName.Value);
        }

        return metadata;
    }

    private static IReadOnlyList<McpServerEventError> ToEventErrors(Error error)
    {
        return [new McpServerEventError(error.Code.Value, error.Message)];
    }

    private static IReadOnlyList<McpServerEventError> ToEventErrors(Exception ex)
    {
        return [new McpServerEventError(ex.GetType().Name, ex.Message)];
    }

    private void PublishEvent(
        McpServerName serverName,
        McpServerEventType eventType,
        IReadOnlyList<McpServerEventError>? errors = null,
        McpServerInstanceId? instanceId = null,
        McpServerRequestId? requestId = null,
        McpServerEventConfiguration? oldConfiguration = null,
        McpServerEventConfiguration? configuration = null,
        McpServerToolInvocationEventData? toolInvocationData = null)
    {
        var @event = new McpServerEvent(
            serverName,
            eventType,
            DateTime.UtcNow,
            errors,
            instanceId,
            requestId,
            oldConfiguration,
            configuration,
            toolInvocationData);

        _eventPublisher.Publish(@event);
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
        public McpServerMetadata? Metadata { get; set; }

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
