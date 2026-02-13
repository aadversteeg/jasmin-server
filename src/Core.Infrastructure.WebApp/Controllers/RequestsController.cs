using Core.Application.Requests;
using Core.Domain.Models;
using Core.Domain.Paging;
using Core.Domain.Requests;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Models;
using Core.Infrastructure.WebApp.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Error = Ave.Extensions.ErrorPaths.Error;

namespace Core.Infrastructure.WebApp.Controllers;

/// <summary>
/// Controller for generic async request endpoints.
/// </summary>
[ApiController]
[Route("v1/requests")]
[Tags("Requests")]
public class RequestsController : ControllerBase
{
    private readonly IRequestStore _requestStore;
    private readonly IRequestQueue _requestQueue;
    private readonly McpServerStatusOptions _statusOptions;

    public RequestsController(
        IRequestStore requestStore,
        IRequestQueue requestQueue,
        IOptions<McpServerStatusOptions> statusOptions)
    {
        _requestStore = requestStore;
        _requestQueue = requestQueue;
        _statusOptions = statusOptions.Value;
    }

    /// <summary>
    /// Creates a new async request.
    /// </summary>
    /// <param name="body">The request details.</param>
    /// <param name="timeZone">Optional timezone for timestamps.</param>
    /// <returns>The created request with initial status.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateRequestBody body, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
        }

        var domainRequestResult = RequestMapper.ToDomain(body);
        if (domainRequestResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(domainRequestResult.Error));
        }

        var domainRequest = domainRequestResult.Value;

        _requestStore.Add(domainRequest);
        _requestQueue.Enqueue(domainRequest);

        var response = RequestMapper.ToResponse(domainRequest, resolvedTimeZone);

        return AcceptedAtRoute(
            "GetRequestById",
            new { requestId = domainRequest.Id.Value },
            response);
    }

    /// <summary>
    /// Gets all requests with optional paging, filtering, and sorting.
    /// </summary>
    /// <param name="timeZone">Optional timezone for timestamps.</param>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Number of items per page (1-100). Default: 20.</param>
    /// <param name="target">Optional target prefix filter.</param>
    /// <param name="action">Optional action filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="orderBy">Field to order by: 'createdAt' or 'completedAt'. Default: 'createdAt'.</param>
    /// <param name="orderDirection">Sort direction: 'asc' or 'desc'. Default: 'desc'.</param>
    /// <param name="from">Filter requests from this timestamp (inclusive).</param>
    /// <param name="to">Filter requests up to this timestamp (inclusive).</param>
    /// <returns>A paged list of requests.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Models.Paging.PagedResponse<RequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetAll(
        [FromQuery] string? timeZone = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? target = null,
        [FromQuery] string? action = null,
        [FromQuery] string? status = null,
        [FromQuery] string orderBy = "createdAt",
        [FromQuery] string orderDirection = "desc",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
        }

        var pagingResult = PagingParameters.Create(page, pageSize);
        if (pagingResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(pagingResult.Error));
        }

        RequestAction? actionFilter = null;
        if (!string.IsNullOrEmpty(action))
        {
            actionFilter = new RequestAction(action);
        }

        RequestStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (!Enum.TryParse<RequestStatus>(status, ignoreCase: true, out var parsedStatus))
            {
                return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.Request.InvalidStatus, $"Invalid status: '{status}'. Valid values are: pending, running, completed, failed")));
            }
            statusFilter = parsedStatus;
        }

        var dateFilter = new DateRangeFilter(from, to);
        var sortDir = orderDirection.ToLowerInvariant() == "asc"
            ? SortDirection.Ascending
            : SortDirection.Descending;

        var pagedRequests = _requestStore.GetAll(
            pagingResult.Value,
            target,
            actionFilter,
            statusFilter,
            dateFilter,
            orderBy,
            sortDir);

        return Ok(RequestMapper.ToPagedResponse(pagedRequests, resolvedTimeZone));
    }

    /// <summary>
    /// Gets a specific request by ID.
    /// </summary>
    /// <param name="requestId">The identifier of the request.</param>
    /// <param name="timeZone">Optional timezone for timestamps.</param>
    /// <returns>The request details.</returns>
    [HttpGet("{requestId}", Name = "GetRequestById")]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string requestId, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
        }

        var requestIdObj = RequestId.From(requestId);
        var maybeRequest = _requestStore.GetById(requestIdObj);

        if (maybeRequest.HasNoValue)
        {
            return NotFound(ErrorResponse.FromError(new Error(ErrorCodes.Request.NotFound, $"Request '{requestId}' not found")));
        }

        return Ok(RequestMapper.ToResponse(maybeRequest.Value, resolvedTimeZone));
    }

    private TimeZoneInfo? ResolveTimeZone(string? requestedTimeZone)
    {
        var timeZoneId = requestedTimeZone ?? _statusOptions.DefaultTimeZone;

        if (string.IsNullOrEmpty(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
    }
}
