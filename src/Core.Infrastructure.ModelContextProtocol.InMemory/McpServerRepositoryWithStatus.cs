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
                    var entry = _statusCache.GetEntry(s.Id);
                    return s.WithStatus(entry.Status, entry.UpdatedOnUtc);
                })
                .ToList() as IReadOnlyList<McpServerInfo>);
    }

    /// <inheritdoc />
    public Result<Maybe<McpServerDefinition>, Error> GetById(McpServerId id) =>
        _inner.GetById(id);

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Create(McpServerDefinition definition)
    {
        var result = _inner.Create(definition);
        if (result.IsSuccess)
        {
            _statusCache.SetStatus(definition.Id, McpServerConnectionStatus.Unknown);
        }
        return result;
    }

    /// <inheritdoc />
    public Result<McpServerDefinition, Error> Update(McpServerDefinition definition) =>
        _inner.Update(definition);

    /// <inheritdoc />
    public Result<Unit, Error> Delete(McpServerId id)
    {
        var result = _inner.Delete(id);
        if (result.IsSuccess)
        {
            _statusCache.RemoveStatus(id);
        }
        return result;
    }
}
