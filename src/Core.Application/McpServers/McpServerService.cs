using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;

using McpServerEvent = Core.Domain.McpServers.McpServerEvent;

namespace Core.Application.McpServers;

/// <summary>
/// Service for managing MCP server configurations.
/// </summary>
public class McpServerService : IMcpServerService
{
    private readonly IMcpServerRepository _repository;
    private readonly IMcpServerConnectionStatusCache _statusCache;

    public McpServerService(
        IMcpServerRepository repository,
        IMcpServerConnectionStatusCache statusCache)
    {
        _repository = repository;
        _statusCache = statusCache;
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
        return _repository.Create(definition);
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Update(McpServerDefinition definition)
    {
        return _repository.Update(definition);
    }

    /// <inheritdoc />
    public Result<Unit, Error> Delete(McpServerName id)
    {
        return _repository.Delete(id);
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<McpServerEvent>, Error> GetEvents(McpServerName name)
    {
        var serverId = _statusCache.GetOrCreateId(name);
        var events = _statusCache.GetEvents(serverId);
        return Result<IReadOnlyList<McpServerEvent>, Error>.Success(events);
    }
}
