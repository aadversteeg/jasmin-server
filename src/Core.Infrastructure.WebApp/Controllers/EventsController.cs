using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Paging;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Models;
using Core.Infrastructure.WebApp.Models.Events;
using Core.Infrastructure.WebApp.Models.Paging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.WebApp.Controllers;

/// <summary>
/// Controller for global events endpoint.
/// </summary>
[ApiController]
[Route("v1/events")]
[Tags("McpServers")]
public class EventsController : ControllerBase
{
    private readonly IMcpServerService _mcpServerService;
    private readonly McpServerStatusOptions _statusOptions;

    public EventsController(
        IMcpServerService mcpServerService,
        IOptions<McpServerStatusOptions> statusOptions)
    {
        _mcpServerService = mcpServerService;
        _statusOptions = statusOptions.Value;
    }

    /// <summary>
    /// Gets global events (server created/deleted) with optional paging, filtering, and sorting.
    /// </summary>
    /// <param name="serverName">Optional filter by server name.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Number of items per page (1-100). Default: 20.</param>
    /// <param name="orderDirection">Sort direction: 'asc' or 'desc'. Default: 'desc'.</param>
    /// <param name="from">Filter events from this timestamp (inclusive).</param>
    /// <param name="to">Filter events up to this timestamp (inclusive).</param>
    /// <returns>A paged list of global events.</returns>
    [HttpGet(Name = "GetGlobalEvents")]
    [ProducesResponseType(typeof(PagedResponse<GlobalEventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetEvents(
        [FromQuery] string? serverName = null,
        [FromQuery] string? timeZone = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string orderDirection = "desc",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        var pagingResult = PagingParameters.Create(page, pageSize);
        if (pagingResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(pagingResult.Error));
        }

        McpServerName? serverNameFilter = null;
        if (!string.IsNullOrEmpty(serverName))
        {
            var serverNameResult = McpServerName.Create(serverName);
            if (serverNameResult.IsFailure)
            {
                return BadRequest(ErrorResponse.FromError(serverNameResult.Error));
            }
            serverNameFilter = serverNameResult.Value;
        }

        var dateFilter = new DateRangeFilter(from, to);
        var sortDir = orderDirection.ToLowerInvariant() == "asc"
            ? SortDirection.Ascending
            : SortDirection.Descending;

        var result = _mcpServerService.GetGlobalEvents(
            pagingResult.Value,
            serverNameFilter,
            dateFilter,
            sortDir);

        if (result.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromError(result.Error));
        }

        var response = GlobalEventMapper.ToPagedResponse(result.Value, resolvedTimeZone);
        return Ok(response);
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
