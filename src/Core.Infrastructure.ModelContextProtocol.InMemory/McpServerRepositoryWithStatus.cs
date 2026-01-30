using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Decorator that enriches McpServerInfo with connection status from the cache.
/// </summary>
public class McpServerRepositoryWithStatus : IMcpServerRepository
{
    private readonly IMcpServerRepository _inner;
    private readonly IMcpServerConnectionStatusCache _statusCache;

    public McpServerRepositoryWithStatus(
        IMcpServerRepository inner,
        IMcpServerConnectionStatusCache statusCache)
    {
        _inner = inner;
        _statusCache = statusCache;
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<McpServerInfo>, Error> GetAll()
    {
        return _inner.GetAll()
            .OnSuccessMap(servers => servers
                .Select(s =>
                {
                    var serverId = _statusCache.GetOrCreateId(s.Id);
                    var entry = _statusCache.GetEntry(serverId);
                    return s.WithStatus(entry.Status, entry.UpdatedOnUtc);
                })
                .ToList() as IReadOnlyList<McpServerInfo>);
    }

    /// <inheritdoc />
    public Result<Maybe<McpServerDefinition>, Error> GetById(McpServerName id) =>
        _inner.GetById(id);

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Create(McpServerDefinition definition)
    {
        var result = _inner.Create(definition);
        if (result.IsSuccess)
        {
            var serverId = _statusCache.GetOrCreateId(definition.Id);
            _statusCache.SetStatus(serverId, McpServerConnectionStatus.Unknown);
        }
        return result;
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Update(McpServerDefinition definition) =>
        _inner.Update(definition);

    /// <inheritdoc />
    public Result<Unit, Error> Delete(McpServerName id)
    {
        var result = _inner.Delete(id);
        if (result.IsSuccess)
        {
            _statusCache.RemoveByName(id);
        }
        return result;
    }
}
