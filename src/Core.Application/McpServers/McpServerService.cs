using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;

using McpServerEvent = Core.Domain.McpServers.McpServerEvent;

namespace Core.Application.McpServers;

/// <summary>
/// Service for managing MCP server configurations.
/// </summary>
public class McpServerService : IMcpServerService
{
    private readonly IMcpServerRepository _repository;
    private readonly IMcpServerConnectionStatusCache _statusCache;
    private readonly IGlobalEventStore _globalEventStore;

    public McpServerService(
        IMcpServerRepository repository,
        IMcpServerConnectionStatusCache statusCache,
        IGlobalEventStore globalEventStore)
    {
        _repository = repository;
        _statusCache = statusCache;
        _globalEventStore = globalEventStore;
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
            // Record ServerCreated event to global store
            _globalEventStore.RecordEvent(GlobalEventType.ServerCreated, definition.Id);

            // Record ConfigurationCreated event if server has configuration
            if (definition.HasConfiguration)
            {
                var serverId = _statusCache.GetOrCreateId(definition.Id);
                var newConfig = McpServerEventConfiguration.FromDefinition(definition);
                _statusCache.RecordEvent(
                    serverId,
                    McpServerEventType.ConfigurationCreated,
                    newConfiguration: newConfig);
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
            var serverId = _statusCache.GetOrCreateId(definition.Id);
            var newConfig = McpServerEventConfiguration.FromDefinition(definition);

            var eventType = hadConfiguration
                ? McpServerEventType.ConfigurationUpdated
                : McpServerEventType.ConfigurationCreated;

            _statusCache.RecordEvent(
                serverId,
                eventType,
                oldConfiguration: oldConfig,
                newConfiguration: newConfig);
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
            var serverId = _statusCache.GetOrCreateId(id);
            _statusCache.RecordEvent(
                serverId,
                McpServerEventType.ConfigurationDeleted,
                oldConfiguration: oldConfig);
        }

        // Record ServerDeleted event to global store
        _globalEventStore.RecordEvent(GlobalEventType.ServerDeleted, id);

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
            var serverId = _statusCache.GetOrCreateId(id);
            _statusCache.RecordEvent(
                serverId,
                McpServerEventType.ConfigurationDeleted,
                oldConfiguration: oldConfig);
        }
        return result;
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<McpServerEvent>, Error> GetEvents(McpServerName name)
    {
        var serverId = _statusCache.GetOrCreateId(name);
        var events = _statusCache.GetEvents(serverId);
        return Result<IReadOnlyList<McpServerEvent>, Error>.Success(events);
    }

    /// <inheritdoc />
    public Result<PagedResult<McpServerEvent>, Error> GetEvents(
        McpServerName name,
        PagingParameters paging,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending)
    {
        var serverId = _statusCache.GetOrCreateId(name);
        var pagedEvents = _statusCache.GetEvents(serverId, paging, dateFilter, sortDirection);
        return Result<PagedResult<McpServerEvent>, Error>.Success(pagedEvents);
    }

    /// <inheritdoc />
    public Result<PagedResult<GlobalEvent>, Error> GetGlobalEvents(
        PagingParameters paging,
        McpServerName? serverNameFilter = null,
        DateRangeFilter? dateFilter = null,
        SortDirection sortDirection = SortDirection.Descending)
    {
        var pagedEvents = _globalEventStore.GetEvents(paging, serverNameFilter, dateFilter, sortDirection);
        return Result<PagedResult<GlobalEvent>, Error>.Success(pagedEvents);
    }
}
