using System.Collections.Concurrent;
using Ave.Extensions.Functional;
using Core.Application.Requests;
using Core.Domain.Paging;
using Core.Domain.Requests;

namespace Core.Infrastructure.ModelContextProtocol.InMemory.Requests;

/// <summary>
/// Thread-safe in-memory store for generic requests with prefix-match filtering by target.
/// </summary>
public class RequestStore : IRequestStore
{
    private readonly ConcurrentDictionary<string, Request> _requests = new();
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Add(Request request)
    {
        _requests[request.Id.Value] = request;
    }

    /// <inheritdoc />
    public Maybe<Request> GetById(RequestId id)
    {
        return _requests.TryGetValue(id.Value, out var request)
            ? Maybe.From(request)
            : Maybe<Request>.None;
    }

    /// <inheritdoc />
    public PagedResult<Request> GetAll(
        PagingParameters paging,
        string? targetFilter = null,
        RequestAction? actionFilter = null,
        RequestStatus? statusFilter = null,
        DateRangeFilter? dateFilter = null,
        string orderBy = "createdAt",
        SortDirection sortDirection = SortDirection.Descending)
    {
        lock (_lock)
        {
            IEnumerable<Request> requests = _requests.Values;

            if (!string.IsNullOrEmpty(targetFilter))
            {
                requests = requests.Where(r =>
                    r.Target.Equals(targetFilter, StringComparison.Ordinal) ||
                    r.Target.StartsWith(targetFilter + "/", StringComparison.Ordinal));
            }

            if (actionFilter.HasValue)
            {
                requests = requests.Where(r => r.Action == actionFilter.Value);
            }

            if (statusFilter.HasValue)
            {
                requests = requests.Where(r => r.Status == statusFilter.Value);
            }

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

            return new PagedResult<Request>(items, paging.Page, paging.PageSize, totalItems);
        }
    }

    /// <inheritdoc />
    public void Update(Request request)
    {
        _requests[request.Id.Value] = request;
    }
}
