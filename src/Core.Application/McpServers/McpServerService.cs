using Ave.Extensions.ErrorPaths;
using Ave.Extensions.Functional;
using Core.Application.Events;
using Core.Application.Requests;
using Core.Domain.Events;
using Core.Domain.Events.Payloads;
using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Service for managing MCP server configurations.
/// </summary>
public class McpServerService : IMcpServerService
{
    private readonly IMcpServerRepository _repository;
    private readonly IMcpServerConnectionStatusCache _statusCache;
    private readonly IEventPublisher<Event> _eventPublisher;

    public McpServerService(
        IMcpServerRepository repository,
        IMcpServerConnectionStatusCache statusCache,
        IEventPublisher<Event> eventPublisher)
    {
        _repository = repository;
        _statusCache = statusCache;
        _eventPublisher = eventPublisher;
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<McpServerInfo>, Error> GetAll()
    {
        return _repository.GetAll();
    }

    /// <inheritdoc />
    public Result<Maybe<McpServerDefinition>, Error> GetById(McpServerName id)
    {
        return _repository.GetById(id);
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Create(McpServerDefinition definition)
    {
        var result = _repository.Create(definition);
        if (result.IsSuccess)
        {
            var target = TargetUri.McpServer(definition.Id.Value);

            // Record ServerCreated event
            _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Created, target));

            // Record ConfigurationCreated event if server has configuration
            if (definition.HasConfiguration)
            {
                var newConfig = ToEventConfiguration(definition);
                _eventPublisher.Publish(EventFactory.Create(
                    EventTypes.McpServer.Configuration.Created,
                    target,
                    new ConfigurationPayload(null, newConfig)));
            }
        }
        return result;
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Update(McpServerDefinition definition)
    {
        // Get old configuration before update
        var oldDefinitionResult = _repository.GetById(definition.Id);
        EventConfiguration? oldConfig = null;
        bool hadConfiguration = false;

        if (oldDefinitionResult.IsSuccess && oldDefinitionResult.Value.HasValue)
        {
            var oldDefinition = oldDefinitionResult.Value.Value;
            hadConfiguration = oldDefinition.HasConfiguration;
            if (hadConfiguration)
            {
                oldConfig = ToEventConfiguration(oldDefinition);
            }
        }

        var result = _repository.Update(definition);
        if (result.IsSuccess)
        {
            var target = TargetUri.McpServer(definition.Id.Value);
            var newConfig = ToEventConfiguration(definition);

            var eventType = hadConfiguration
                ? EventTypes.McpServer.Configuration.Updated
                : EventTypes.McpServer.Configuration.Created;

            _eventPublisher.Publish(EventFactory.Create(
                eventType,
                target,
                new ConfigurationPayload(oldConfig, newConfig)));
        }
        return result;
    }

    /// <inheritdoc />
    public Result<Unit, Error> Delete(McpServerName id)
    {
        var target = TargetUri.McpServer(id.Value);

        // Get old definition before delete to check for configuration
        var oldDefinitionResult = _repository.GetById(id);
        EventConfiguration? oldConfig = null;

        if (oldDefinitionResult.IsSuccess && oldDefinitionResult.Value.HasValue)
        {
            var oldDef = oldDefinitionResult.Value.Value;
            if (oldDef.HasConfiguration)
            {
                oldConfig = ToEventConfiguration(oldDef);
            }
        }

        // Record ConfigurationDeleted event if server had configuration
        if (oldConfig != null)
        {
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.Configuration.Deleted,
                target,
                new ConfigurationPayload(oldConfig, null)));
        }

        // Record ServerDeleted event
        _eventPublisher.Publish(EventFactory.Create(EventTypes.McpServer.Deleted, target));

        // Delete from repository (this will also clear the status cache)
        return _repository.Delete(id);
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> DeleteConfiguration(McpServerName id)
    {
        var target = TargetUri.McpServer(id.Value);

        // Get old configuration before delete
        var oldDefinitionResult = _repository.GetById(id);
        EventConfiguration? oldConfig = null;

        if (oldDefinitionResult.IsSuccess && oldDefinitionResult.Value.HasValue)
        {
            var oldDef = oldDefinitionResult.Value.Value;
            if (oldDef.HasConfiguration)
            {
                oldConfig = ToEventConfiguration(oldDef);
            }
        }

        var result = _repository.DeleteConfiguration(id);
        if (result.IsSuccess && oldConfig != null)
        {
            _eventPublisher.Publish(EventFactory.Create(
                EventTypes.McpServer.Configuration.Deleted,
                target,
                new ConfigurationPayload(oldConfig, null)));
        }
        return result;
    }

    private static EventConfiguration ToEventConfiguration(McpServerDefinition definition)
    {
        return new EventConfiguration(definition.Command!, definition.Args!, definition.Env!);
    }
}
