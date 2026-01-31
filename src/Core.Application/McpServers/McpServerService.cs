using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;

namespace Core.Application.McpServers;

/// <summary>
/// Service for managing MCP server configurations.
/// </summary>
public class McpServerService : IMcpServerService
{
    private readonly IMcpServerRepository _repository;
    private readonly IMcpServerConnectionStatusCache _statusCache;
    private readonly IEventStore _eventStore;

    public McpServerService(
        IMcpServerRepository repository,
        IMcpServerConnectionStatusCache statusCache,
        IEventStore eventStore)
    {
        _repository = repository;
        _statusCache = statusCache;
        _eventStore = eventStore;
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
            // Record ServerCreated event
            _eventStore.RecordEvent(definition.Id, McpServerEventType.ServerCreated);

            // Record ConfigurationCreated event if server has configuration
            if (definition.HasConfiguration)
            {
                var newConfig = McpServerEventConfiguration.FromDefinition(definition);
                _eventStore.RecordEvent(
                    definition.Id,
                    McpServerEventType.ConfigurationCreated,
                    configuration: newConfig);
            }
        }
        return result;
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Update(McpServerDefinition definition)
    {
        // Get old configuration before update
        var oldDefinitionResult = _repository.GetById(definition.Id);
        McpServerEventConfiguration? oldConfig = null;
        bool hadConfiguration = false;

        if (oldDefinitionResult.IsSuccess && oldDefinitionResult.Value.HasValue)
        {
            var oldDefinition = oldDefinitionResult.Value.Value;
            hadConfiguration = oldDefinition.HasConfiguration;
            oldConfig = McpServerEventConfiguration.FromDefinition(oldDefinition);
        }

        var result = _repository.Update(definition);
        if (result.IsSuccess)
        {
            var newConfig = McpServerEventConfiguration.FromDefinition(definition);

            var eventType = hadConfiguration
                ? McpServerEventType.ConfigurationUpdated
                : McpServerEventType.ConfigurationCreated;

            _eventStore.RecordEvent(
                definition.Id,
                eventType,
                oldConfiguration: oldConfig,
                configuration: newConfig);
        }
        return result;
    }

    /// <inheritdoc />
    public Result<Unit, Error> Delete(McpServerName id)
    {
        // Get old definition before delete to check for configuration
        var oldDefinitionResult = _repository.GetById(id);
        McpServerEventConfiguration? oldConfig = null;

        if (oldDefinitionResult.IsSuccess && oldDefinitionResult.Value.HasValue)
        {
            oldConfig = McpServerEventConfiguration.FromDefinition(oldDefinitionResult.Value.Value);
        }

        // Record ConfigurationDeleted event if server had configuration
        if (oldConfig != null)
        {
            _eventStore.RecordEvent(
                id,
                McpServerEventType.ConfigurationDeleted,
                oldConfiguration: oldConfig);
        }

        // Record ServerDeleted event
        _eventStore.RecordEvent(id, McpServerEventType.ServerDeleted);

        // Delete from repository (this will also clear the status cache)
        return _repository.Delete(id);
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> DeleteConfiguration(McpServerName id)
    {
        // Get old configuration before delete
        var oldDefinitionResult = _repository.GetById(id);
        McpServerEventConfiguration? oldConfig = null;

        if (oldDefinitionResult.IsSuccess && oldDefinitionResult.Value.HasValue)
        {
            oldConfig = McpServerEventConfiguration.FromDefinition(oldDefinitionResult.Value.Value);
        }

        var result = _repository.DeleteConfiguration(id);
        if (result.IsSuccess && oldConfig != null)
        {
            _eventStore.RecordEvent(
                id,
                McpServerEventType.ConfigurationDeleted,
                oldConfiguration: oldConfig);
        }
        return result;
    }
}
