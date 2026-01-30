using System.Collections.Concurrent;
using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Paging;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Thread-safe in-memory store for MCP server requests.
/// </summary>
public class McpServerRequestStore : IMcpServerRequestStore
{
    private readonly ConcurrentDictionary<string, McpServerRequest> _requests = new();
    private readonly ConcurrentDictionary<string, List<string>> _serverToRequests = new();
    private readonly object _listLock = new();

    /// <inheritdoc />
    public void Add(McpServerRequest request)
    {
        _requests[request.Id.Value] = request;

        _serverToRequests.AddOrUpdate(
            request.ServerName.Value,
            _ => new List<string> { request.Id.Value },
            (_, list) =>
            {
                lock (_listLock)
                {
                    list.Add(request.Id.Value);
                }
                return list;
            });
    }

    /// <inheritdoc />
    public Maybe<McpServerRequest> GetById(McpServerRequestId id)
    {
        return _requests.TryGetValue(id.Value, out var request)
            ? Maybe.From(request)
            : Maybe<McpServerRequest>.None;
    }

    /// <inheritdoc />
    public IReadOnlyList<McpServerRequest> GetByServerName(McpServerName serverName)
    {
        if (!_serverToRequests.TryGetValue(serverName.Value, out var requestIds))
        {
            return Array.Empty<McpServerRequest>();
        }

        lock (_listLock)
        {
            return requestIds
                .Select(id => _requests.TryGetValue(id, out var req) ? req : null)
                .Where(req => req != null)
                .OrderByDescending(req => req!.CreatedAtUtc)
                .ToList()
                .AsReadOnly()!;
        }
    }

    /// <inheritdoc />
    public void Update(McpServerRequest request)
    {
        _requests[request.Id.Value] = request;
    }

    /// <inheritdoc />
    public PagedResult<McpServerRequest> GetByServerName(
        McpServerName serverName,
        PagingParameters paging,
        DateRangeFilter? dateFilter = null,
        string orderBy = "createdAt",
        SortDirection sortDirection = SortDirection.Descending)
    {
        if (!_serverToRequests.TryGetValue(serverName.Value, out var requestIds))
        {
            return new PagedResult<McpServerRequest>([], paging.Page, paging.PageSize, 0);
        }

        lock (_listLock)
        {
            var requests = requestIds
                .Select(id => _requests.TryGetValue(id, out var req) ? req : null)
                .Where(req => req != null)
                .Select(req => req!);

            if (dateFilter != null)
            {
                requests = requests.Where(r => dateFilter.IsInRange(r.CreatedAtUtc));
            }

            var totalItems = requests.Count();

            requests = orderBy.ToLowerInvariant() switch
            {
                "completedat" => sortDirection == SortDirection.Ascending
                    ? requests.OrderBy(r => r.CompletedAtUtc ?? DateTime.MaxValue)
                    : requests.OrderByDescending(r => r.CompletedAtUtc ?? DateTime.MinValue),
                _ => sortDirection == SortDirection.Ascending
                    ? requests.OrderBy(r => r.CreatedAtUtc)
                    : requests.OrderByDescending(r => r.CreatedAtUtc)
            };

            var items = requests.Skip(paging.Skip).Take(paging.PageSize).ToList();

            return new PagedResult<McpServerRequest>(items, paging.Page, paging.PageSize, totalItems);
        }
    }
}
