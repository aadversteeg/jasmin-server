using System.Collections.Concurrent;
using System.Text.Json;
using Ave.Extensions.Functional;
using Core.Application.Events;
using Core.Application.McpServers;
using Core.Application.Requests;
using Core.Domain.Events;
using Core.Domain.Events.Payloads;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly IEventPublisher<Event> _eventPublisher;
    private readonly IMcpServerInstanceLogStore _logStore;
    private readonly ILogger<McpServerInstanceManager> _logger;
    private readonly McpServerHostingOptions _options;
    private readonly ConcurrentDictionary<string, ManagedMcpServerInstance> _instances = new();
    private readonly TimeSpan _connectionTimeout;
    private readonly TimeSpan _toolInvocationTimeout;

    public McpServerInstanceManager(
        IMcpServerRepository repository,
        IMcpServerConnectionStatusCache statusCache,
        IEventPublisher<Event> eventPublisher,
        IMcpServerInstanceLogStore logStore,
        ILogger<McpServerInstanceManager> logger,
        IOptions<McpServerHostingOptions> options)
    {
        _repository = repository;
        _statusCache = statusCache;
        _eventPublisher = eventPublisher;
        _logStore = logStore;
        _logger = logger;
        _options = options.Value;
        _connectionTimeout = TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds);
        _toolInvocationTimeout = TimeSpan.FromSeconds(_options.ToolInvocationTimeoutSeconds);
    }

    /// <inheritdoc />
    public async Task<Result<McpServerInstanceId, Error>> StartInstanceAsync(
        McpServerName serverName,
        string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        var serverId = _statusCache.GetOrCreateId(serverName);
        var instanceId = McpServerInstanceId.Create();
        var target = TargetUri.McpServerInstance(serverName.Value, instanceId.Value);


        // Record starting event immediately
        _logger.LogDebug("Starting MCP server instance {InstanceId} for {ServerName}", instanceId.Value, serverName.Value);
        _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.Starting, target, requestId));

        try
        {
            // Get server definition
            var definitionResult = _repository.GetById(serverName);
            if (definitionResult.IsFailure)
            {
                _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.StartFailed, target, ToErrorPayload(definitionResult.Error), requestId));
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(definitionResult.Error);
            }

            if (!definitionResult.Value.HasValue)
            {
                var error = Errors.McpServerNotFound(serverName.Value);
                _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.StartFailed, target, ToErrorPayload(error), requestId));
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(error);
            }

            var definition = definitionResult.Value.Value;

            // Check if server has configuration
            if (!definition.HasConfiguration)
            {
                var error = Errors.ConfigurationMissing(serverName.Value);
                _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.StartFailed, target, ToErrorPayload(error), requestId));
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return Result<McpServerInstanceId, Error>.Failure(error);
            }

            var startConfig = new EventConfiguration(definition.Command!, definition.Args!, definition.Env!);

            // Create transport and connect
            var transportOptions = new StdioClientTransportOptions
            {
                Command = definition.Command!,
                Arguments = [.. definition.Args!],
                Name = definition.Id.Value,
                EnvironmentVariables = definition.Env!.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value),
                StandardErrorLines = line => _logStore.Append(instanceId, line)
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
                _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.StartFailed, target, ToErrorPayload(ex), requestId));
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
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.Instance.Started,
                target,
                new InstanceStartedPayload(startConfig),
                requestId));
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
            _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.StartFailed, target, ToErrorPayload(ex), requestId));
            _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
            return Result<McpServerInstanceId, Error>.Failure(new Error(
                ErrorCodes.ConfigFileReadError,
                $"Error starting MCP server '{serverName.Value}': {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<Unit, Error>> StopInstanceAsync(
        McpServerInstanceId instanceId,
        string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_instances.TryRemove(instanceId.Value, out var instance))
        {
            return Result<Unit, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' not found"));
        }

        var target = TargetUri.McpServerInstance(instance.ServerName.Value, instanceId.Value);


        try
        {
            _logger.LogDebug("Stopping MCP server instance {InstanceId} for {ServerName}", instanceId.Value, instance.ServerName.Value);
            _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.Stopping, target, requestId));

            await instance.Client.DisposeAsync();
            _logStore.Remove(instanceId);

            _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.Stopped, target, requestId));
            _logger.LogInformation("Stopped MCP server instance {InstanceId} for {ServerName}", instanceId.Value, instance.ServerName.Value);

            return Result<Unit, Error>.Success(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping MCP server instance {InstanceId}", instanceId.Value);
            _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Instance.StopFailed, target, ToErrorPayload(ex), requestId));
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
        string? requestId = null,
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

        var target = TargetUri.McpServerInstance(serverName.Value, instanceId.Value);


        // Record ToolInvoking event
        _logger.LogDebug("Invoking tool {ToolName} on instance {InstanceId}", toolName, instanceId.Value);
        var inputJson = arguments != null ? JsonSerializer.SerializeToElement(arguments) : (JsonElement?)null;
        _eventPublisher.Publish(EventFactory.Create(
            EventTypes.McpServer.ToolInvocation.Invoking,
            target,
            new ToolInvocationPayload(toolName, inputJson, null),
            requestId));

        // Create a linked cancellation token with timeout
        using var timeoutCts = new CancellationTokenSource(_toolInvocationTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Call client.CallToolAsync with timeout
            var sdkResult = await instance.Client.CallToolAsync(
                toolName, arguments, cancellationToken: linkedCts.Token);

            // Map SDK result to domain model
            var result = MapToToolInvocationResult(sdkResult);

            // Record ToolInvoked event
            var outputJson = JsonSerializer.SerializeToElement(result);
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.ToolInvocation.Invoked,
                target,
                new ToolInvocationPayload(toolName, inputJson, outputJson),
                requestId));

            _logger.LogInformation("Tool {ToolName} invoked successfully on instance {InstanceId}", toolName, instanceId.Value);
            return Result<McpToolInvocationResult, Error>.Success(result);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not external cancellation)
            _logger.LogWarning("Tool invocation timed out for {ToolName} on instance {InstanceId} after {Timeout}",
                toolName, instanceId.Value, _toolInvocationTimeout);
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.ToolInvocation.Failed,
                target,
                new ErrorPayload([new EventError("TOOL_INVOCATION_TIMEOUT", $"Tool invocation timed out after {_toolInvocationTimeout.TotalSeconds} seconds")]),
                requestId));

            return Result<McpToolInvocationResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Tool invocation timed out after {_toolInvocationTimeout.TotalSeconds} seconds"));
        }
        catch (OperationCanceledException)
        {
            // External cancellation
            _logger.LogInformation("Tool invocation cancelled for {ToolName} on instance {InstanceId}", toolName, instanceId.Value);
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.ToolInvocation.Failed,
                target,
                new ErrorPayload([new EventError("TOOL_INVOCATION_CANCELLED", "Tool invocation was cancelled")]),
                requestId));

            return Result<McpToolInvocationResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                "Tool invocation was cancelled"));
        }
        catch (McpException ex)
        {
            // Record ToolInvocationFailed event (MCP protocol error)
            _logger.LogWarning(ex, "Tool invocation failed for {ToolName} on instance {InstanceId}: MCP error", toolName, instanceId.Value);
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.ToolInvocation.Failed,
                target,
                ToErrorPayload(ex),
                requestId));

            return Result<McpToolInvocationResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Tool invocation failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            // Record ToolInvocationFailed event (unexpected error)
            _logger.LogError(ex, "Tool invocation failed for {ToolName} on instance {InstanceId}", toolName, instanceId.Value);
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.ToolInvocation.Failed,
                target,
                ToErrorPayload(ex),
                requestId));

            return Result<McpToolInvocationResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Tool invocation failed: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<McpPromptResult, Error>> GetPromptAsync(
        McpServerName serverName,
        McpServerInstanceId instanceId,
        string promptName,
        IReadOnlyDictionary<string, string?>? arguments,
        string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        // Get instance from dictionary, validate it exists
        if (!_instances.TryGetValue(instanceId.Value, out var instance))
        {
            return Result<McpPromptResult, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' not found"));
        }

        if (instance.ServerName.Value != serverName.Value)
        {
            return Result<McpPromptResult, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' does not belong to server '{serverName.Value}'"));
        }

        _logger.LogDebug("Getting prompt {PromptName} from instance {InstanceId}", promptName, instanceId.Value);

        // Create a linked cancellation token with timeout
        using var timeoutCts = new CancellationTokenSource(_toolInvocationTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Convert string arguments to object arguments (SDK expects IReadOnlyDictionary<string, object?>)
            IReadOnlyDictionary<string, object?>? objectArguments = null;
            if (arguments != null)
            {
                objectArguments = arguments.ToDictionary(
                    kvp => kvp.Key,
                    kvp => (object?)kvp.Value);
            }

            // Call client.GetPromptAsync
            var sdkResult = await instance.Client.GetPromptAsync(
                promptName, objectArguments, cancellationToken: linkedCts.Token);

            // Map SDK result to domain model
            var result = MapToPromptResult(sdkResult);

            _logger.LogInformation("Prompt {PromptName} retrieved successfully from instance {InstanceId}", promptName, instanceId.Value);
            return Result<McpPromptResult, Error>.Success(result);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not external cancellation)
            _logger.LogWarning("Get prompt timed out for {PromptName} on instance {InstanceId} after {Timeout}",
                promptName, instanceId.Value, _toolInvocationTimeout);

            return Result<McpPromptResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Get prompt timed out after {_toolInvocationTimeout.TotalSeconds} seconds"));
        }
        catch (OperationCanceledException)
        {
            // External cancellation
            _logger.LogInformation("Get prompt cancelled for {PromptName} on instance {InstanceId}", promptName, instanceId.Value);

            return Result<McpPromptResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                "Get prompt was cancelled"));
        }
        catch (McpException ex)
        {
            _logger.LogWarning(ex, "Get prompt failed for {PromptName} on instance {InstanceId}: MCP error", promptName, instanceId.Value);

            return Result<McpPromptResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Get prompt failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get prompt failed for {PromptName} on instance {InstanceId}", promptName, instanceId.Value);

            return Result<McpPromptResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Get prompt failed: {ex.Message}"));
        }
    }

    private static McpPromptResult MapToPromptResult(GetPromptResult sdkResult)
    {
        var messages = sdkResult.Messages
            .Select(m => new McpPromptMessage(
                m.Role.ToString().ToLowerInvariant(),
                MapPromptContent(m.Content)))
            .ToList()
            .AsReadOnly();

        return new McpPromptResult(messages, sdkResult.Description);
    }

    private static McpPromptMessageContent MapPromptContent(ContentBlock contentBlock)
    {
        // ContentBlock has various derived types like TextContentBlock, ImageContentBlock, etc.
        return contentBlock switch
        {
            TextContentBlock text => new McpPromptMessageContent(
                text.Type,
                text.Text,
                null,
                null,
                null),
            ImageContentBlock image => new McpPromptMessageContent(
                image.Type,
                null,
                image.MimeType,
                image.Data,
                null),
            AudioContentBlock audio => new McpPromptMessageContent(
                audio.Type,
                null,
                audio.MimeType,
                audio.Data,
                null),
            EmbeddedResourceBlock embedded => new McpPromptMessageContent(
                embedded.Type,
                embedded.Resource is TextResourceContents textResource ? textResource.Text : null,
                embedded.Resource.MimeType,
                embedded.Resource is BlobResourceContents blobResource ? blobResource.Blob : null,
                embedded.Resource.Uri),
            ResourceLinkBlock resourceLink => new McpPromptMessageContent(
                resourceLink.Type,
                null,
                resourceLink.MimeType,
                null,
                resourceLink.Uri),
            _ => new McpPromptMessageContent(
                contentBlock.Type,
                null,
                null,
                null,
                null)
        };
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
    public async Task<Result<McpResourceReadResult, Error>> ReadResourceAsync(
        McpServerName serverName,
        McpServerInstanceId instanceId,
        string resourceUri,
        string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_instances.TryGetValue(instanceId.Value, out var instance))
        {
            return Result<McpResourceReadResult, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' not found"));
        }

        if (instance.ServerName.Value != serverName.Value)
        {
            return Result<McpResourceReadResult, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' does not belong to server '{serverName.Value}'"));
        }

        _logger.LogDebug("Reading resource {ResourceUri} from instance {InstanceId}", resourceUri, instanceId.Value);

        // Create a linked cancellation token with timeout
        using var timeoutCts = new CancellationTokenSource(_toolInvocationTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            // Call client.ReadResourceAsync
            var sdkResult = await instance.Client.ReadResourceAsync(
                resourceUri, cancellationToken: linkedCts.Token);

            // Map SDK result to domain model
            var contents = sdkResult.Contents
                .Select(MapResourceContents)
                .ToList()
                .AsReadOnly();

            var result = new McpResourceReadResult(contents);

            _logger.LogInformation("Resource {ResourceUri} read successfully from instance {InstanceId}", resourceUri, instanceId.Value);
            return Result<McpResourceReadResult, Error>.Success(result);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not external cancellation)
            _logger.LogWarning("Read resource timed out for {ResourceUri} on instance {InstanceId} after {Timeout}",
                resourceUri, instanceId.Value, _toolInvocationTimeout);

            return Result<McpResourceReadResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Read resource timed out after {_toolInvocationTimeout.TotalSeconds} seconds"));
        }
        catch (OperationCanceledException)
        {
            // External cancellation
            _logger.LogInformation("Read resource cancelled for {ResourceUri} on instance {InstanceId}", resourceUri, instanceId.Value);

            return Result<McpResourceReadResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                "Read resource was cancelled"));
        }
        catch (McpException ex)
        {
            _logger.LogWarning(ex, "Read resource failed for {ResourceUri} on instance {InstanceId}: MCP error", resourceUri, instanceId.Value);

            return Result<McpResourceReadResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Read resource failed: {ex.Message}"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Read resource failed for {ResourceUri} on instance {InstanceId}", resourceUri, instanceId.Value);

            return Result<McpResourceReadResult, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Read resource failed: {ex.Message}"));
        }
    }

    private static McpResourceContent MapResourceContents(ResourceContents contents)
    {
        return contents switch
        {
            TextResourceContents text => new McpResourceContent(
                text.Uri,
                text.MimeType,
                text.Text,
                null),
            BlobResourceContents blob => new McpResourceContent(
                blob.Uri,
                blob.MimeType,
                null,
                blob.Blob),
            _ => new McpResourceContent(
                contents.Uri,
                contents.MimeType,
                null,
                null)
        };
    }

    /// <inheritdoc />
    public async Task<Result<Unit, Error>> TestConfigurationAsync(
        string command,
        IReadOnlyList<string>? args,
        IReadOnlyDictionary<string, string>? env,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Testing MCP server configuration: command={Command}", command);

        try
        {
            // Create transport with the provided configuration
            var transportOptions = new StdioClientTransportOptions
            {
                Command = command,
                Arguments = args?.ToList() ?? [],
                Name = "test-configuration",
                EnvironmentVariables = env?.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value) ?? new Dictionary<string, string?>()
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
                _logger.LogDebug(ex, "Configuration test failed: could not start MCP server");
                return Result<Unit, Error>.Failure(new Error(
                    ErrorCodes.ConfigFileReadError,
                    $"Failed to start MCP server: {ex.Message}"));
            }

            // Successfully connected - dispose immediately
            _logger.LogDebug("Configuration test passed: MCP server started successfully");
            await client.DisposeAsync();

            return Result<Unit, Error>.Success(Unit.Value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Configuration test cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration test error");
            return Result<Unit, Error>.Failure(new Error(
                ErrorCodes.ConfigFileReadError,
                $"Configuration test error: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<McpServerMetadata, Error>> RefreshMetadataAsync(
        McpServerName serverName,
        McpServerInstanceId instanceId,
        string? requestId = null,
        CancellationToken cancellationToken = default)
    {
        if (!_instances.TryGetValue(instanceId.Value, out var instance))
        {
            return Result<McpServerMetadata, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' not found"));
        }

        if (instance.ServerName.Value != serverName.Value)
        {
            return Result<McpServerMetadata, Error>.Failure(new Error(
                ErrorCodes.McpServerInstanceNotFound,
                $"Instance '{instanceId.Value}' does not belong to server '{serverName.Value}'"));
        }

        _logger.LogDebug("Refreshing metadata for instance {InstanceId} of server {ServerName}",
            instanceId.Value, serverName.Value);

        try
        {
            // Retrieve fresh metadata from the running instance
            var metadata = await RetrieveMetadataAsync(
                instance.Client,
                serverName,
                instanceId,
                requestId,
                cancellationToken);

            // Update the instance's cached metadata
            instance.Metadata = metadata;

            // Update the status cache with the new metadata
            var serverId = _statusCache.GetOrCreateId(serverName);
            _statusCache.SetMetadata(serverId, metadata);

            _logger.LogInformation("Metadata refreshed successfully for instance {InstanceId} of server {ServerName}",
                instanceId.Value, serverName.Value);

            return Result<McpServerMetadata, Error>.Success(metadata);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Refresh metadata cancelled for instance {InstanceId}", instanceId.Value);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh metadata for instance {InstanceId}", instanceId.Value);
            return Result<McpServerMetadata, Error>.Failure(new Error(
                ErrorCodes.ToolInvocationFailed,
                $"Failed to refresh metadata: {ex.Message}"));
        }
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
        string? requestId,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Retrieving metadata for MCP server {ServerName}", serverName.Value);

        var target = TargetUri.McpServerInstance(serverName.Value, instanceId.Value);

        var errors = new List<McpServerMetadataError>();
        IReadOnlyList<McpTool>? tools = null;
        IReadOnlyList<McpPrompt>? prompts = null;
        IReadOnlyList<McpResource>? resources = null;

        // Retrieve tools
        _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Metadata.Tools.Retrieving, target, requestId));
        try
        {
            var mcpTools = await client.ListToolsAsync(cancellationToken: cancellationToken);
            tools = mcpTools.Select(t => new McpTool(
                t.Name,
                t.Title,
                t.Description,
                t.ProtocolTool.InputSchema.ToString())).ToList().AsReadOnly();
            _logger.LogDebug("Retrieved {Count} tools from MCP server {ServerName}", tools.Count, serverName.Value);
            _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Metadata.Tools.Retrieved, target, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve tools from MCP server {ServerName}", serverName.Value);
            errors.Add(new McpServerMetadataError("Tools", ex.Message));
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.Metadata.Tools.RetrievalFailed,
                target,
                new ErrorPayload([new EventError("Tools", ex.Message)]),
                requestId));
        }

        // Retrieve prompts
        _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Metadata.Prompts.Retrieving, target, requestId));
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
            _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Metadata.Prompts.Retrieved, target, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve prompts from MCP server {ServerName}", serverName.Value);
            errors.Add(new McpServerMetadataError("Prompts", ex.Message));
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.Metadata.Prompts.RetrievalFailed,
                target,
                new ErrorPayload([new EventError("Prompts", ex.Message)]),
                requestId));
        }

        // Retrieve resources
        _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Metadata.Resources.Retrieving, target, requestId));
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
            _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Metadata.Resources.Retrieved, target, requestId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve resources from MCP server {ServerName}", serverName.Value);
            errors.Add(new McpServerMetadataError("Resources", ex.Message));
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.Metadata.Resources.RetrievalFailed,
                target,
                new ErrorPayload([new EventError("Resources", ex.Message)]),
                requestId));
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

    private static ErrorPayload ToErrorPayload(Error error)
    {
        return new ErrorPayload([new EventError(error.Code.Value, error.Message)]);
    }

    private static ErrorPayload ToErrorPayload(Exception ex)
    {
        return new ErrorPayload([new EventError(ex.GetType().Name, ex.Message)]);
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
        public EventConfiguration? Configuration { get; }
        public McpServerMetadata? Metadata { get; set; }

        public ManagedMcpServerInstance(
            McpServerInstanceId instanceId,
            McpServerName serverName,
            McpServerId serverId,
            McpClient client,
            DateTime startedAtUtc,
            EventConfiguration? configuration)
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
