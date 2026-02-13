using Ave.Extensions.Functional;
using Core.Domain.Paging;
using Core.Domain.Requests;

namespace Core.Application.Requests;

/// <summary>
/// Store for generic async requests.
/// </summary>
public interface IRequestStore
{
    /// <summary>
    /// Adds a new request to the store.
    /// </summary>
    /// <param name="request">The request to add.</param>
    void Add(Request request);

    /// <summary>
    /// Gets a request by its ID.
    /// </summary>
    /// <param name="id">The request ID.</param>
    /// <returns>The request if found, or None.</returns>
    Maybe<Request> GetById(RequestId id);

    /// <summary>
    /// Gets all requests with paging, filtering, and sorting.
    /// </summary>
    /// <param name="paging">The paging parameters.</param>
    /// <param name="targetFilter">Optional target prefix filter.</param>
    /// <param name="actionFilter">Optional action filter.</param>
    /// <param name="statusFilter">Optional status filter.</param>
    /// <param name="dateFilter">Optional date range filter.</param>
    /// <param name="orderBy">The field to order by (createdAt or completedAt).</param>
    /// <param name="sortDirection">The sort direction (default: Descending).</param>
    /// <returns>A paged result of requests.</returns>
    PagedResult<Request> GetAll(
        PagingParameters paging,
        string? targetFilter = null,
        RequestAction? actionFilter = null,
        RequestStatus? statusFilter = null,
        DateRangeFilter? dateFilter = null,
        string orderBy = "createdAt",
        SortDirection sortDirection = SortDirection.Descending);

    /// <summary>
    /// Updates an existing request in the store.
    /// </summary>
    /// <param name="request">The request to update.</param>
    void Update(Request request);
}
