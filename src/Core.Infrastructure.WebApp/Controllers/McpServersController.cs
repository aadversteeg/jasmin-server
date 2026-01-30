using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Paging;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Extensions;
using Core.Infrastructure.WebApp.Models.McpServers;
using Core.Infrastructure.WebApp.Models.Paging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using McpServerEvent = Core.Domain.McpServers.McpServerEvent;

namespace Core.Infrastructure.WebApp.Controllers;

/// <summary>
/// Controller for MCP server configuration endpoints.
/// </summary>
[ApiController]
[Route("v1/mcp-servers")]
public class McpServersController : ControllerBase
{
    private readonly IMcpServerService _mcpServerService;
    private readonly McpServerStatusOptions _statusOptions;

    public McpServersController(
        IMcpServerService mcpServerService,
        IOptions<McpServerStatusOptions> statusOptions)
    {
        _mcpServerService = mcpServerService;
        _statusOptions = statusOptions.Value;
    }

    /// <summary>
    /// Gets a list of all configured MCP servers.
    /// </summary>
    /// <param name="timeZone">Optional timezone for the updatedOn timestamp. Defaults to configured timezone or UTC.</param>
    /// <returns>A list of MCP server summary information.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetAll([FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest($"Invalid timezone: {timeZone}");
        }

        return _mcpServerService
            .GetAll()
            .ToActionResult(info => Mapper.ToListResponse(info, resolvedTimeZone));
    }

    private TimeZoneInfo ResolveTimeZone(string? requestedTimeZone)
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
            return null!;
        }
    }

    /// <summary>
    /// Gets the full configuration for a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="include">Optional comma-separated list of additional data to include (e.g., "events" or "all").</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>The server definition if found.</returns>
    [HttpGet("{id}", Name = "GetMcpServerById")]
    [ProducesResponseType(typeof(DetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetById(string id, [FromQuery] string? include = null, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest($"Invalid timezone: {timeZone}");
        }

        var includeOptionsResult = McpServerIncludeOptions.Create(include);
        if (includeOptionsResult.IsFailure)
        {
            return BadRequest(includeOptionsResult.Error.Message);
        }

        var includeOptions = includeOptionsResult.Value;

        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(serverNameResult.Error.Message);
        }

        var serverName = serverNameResult.Value;
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, definitionResult.Error.Message);
        }

        if (!definitionResult.Value.HasValue)
        {
            return NotFound();
        }

        var definition = definitionResult.Value.Value;
        IReadOnlyList<McpServerEvent>? events = null;

        if (includeOptions.IncludeEvents)
        {
            var eventsResult = _mcpServerService.GetEvents(serverName);
            if (eventsResult.IsSuccess)
            {
                events = eventsResult.Value;
            }
        }

        return Ok(Mapper.ToDetailsResponse(definition, events, resolvedTimeZone));
    }

    /// <summary>
    /// Gets the events for a specific MCP server with optional paging, filtering, and sorting.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <param name="page">Page number (1-based). Default: 1.</param>
    /// <param name="pageSize">Number of items per page (1-100). Default: 20.</param>
    /// <param name="orderDirection">Sort direction: 'asc' or 'desc'. Default: 'desc'.</param>
    /// <param name="from">Filter events from this timestamp (inclusive).</param>
    /// <param name="to">Filter events up to this timestamp (inclusive).</param>
    /// <returns>A paged list of events for the server.</returns>
    [HttpGet("{id}/events", Name = "GetMcpServerEvents")]
    [ProducesResponseType(typeof(PagedResponse<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetEvents(
        string id,
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
            return BadRequest($"Invalid timezone: {timeZone}");
        }

        var pagingResult = PagingParameters.Create(page, pageSize);
        if (pagingResult.IsFailure)
        {
            return BadRequest(pagingResult.Error.Message);
        }

        var dateFilter = new DateRangeFilter(from, to);
        var sortDir = orderDirection.ToLowerInvariant() == "asc"
            ? SortDirection.Ascending
            : SortDirection.Descending;

        return McpServerName.Create(id)
            .OnSuccessBind(serverName =>
            {
                // First check if the server exists
                var definitionResult = _mcpServerService.GetById(serverName);
                if (definitionResult.IsFailure)
                {
                    return Result<PagedResult<McpServerEvent>, Core.Domain.Models.Error>.Failure(definitionResult.Error);
                }

                if (!definitionResult.Value.HasValue)
                {
                    return Result<PagedResult<McpServerEvent>, Core.Domain.Models.Error>.Failure(
                        Core.Domain.Models.Errors.McpServerNotFound(id));
                }

                return _mcpServerService.GetEvents(serverName, pagingResult.Value, dateFilter, sortDir);
            })
            .ToOkResult(pagedEvents => Mapper.ToPagedResponse(pagedEvents, resolvedTimeZone));
    }

    /// <summary>
    /// Creates a new MCP server configuration.
    /// </summary>
    /// <param name="request">The server configuration to create.</param>
    /// <returns>The created server definition.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Create([FromBody] CreateRequest request)
    {
        return Mapper.ToDomain(request)
            .OnSuccessBind(_mcpServerService.Create)
            .ToCreatedResult(
                "GetMcpServerById",
                def => new { id = def.Id.Value },
                Mapper.ToDetailsResponse);
    }

    /// <summary>
    /// Updates an existing MCP server configuration.
    /// </summary>
    /// <param name="id">The identifier of the MCP server to update.</param>
    /// <param name="request">The updated server configuration.</param>
    /// <returns>The updated server definition.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(DetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Update(string id, [FromBody] UpdateRequest request)
    {
        return McpServerName.Create(id)
            .OnSuccessBind(serverId => Mapper.ToDomain(serverId, request))
            .OnSuccessBind(_mcpServerService.Update)
            .ToOkResult(Mapper.ToDetailsResponse);
    }

    /// <summary>
    /// Deletes an MCP server configuration.
    /// </summary>
    /// <param name="id">The identifier of the MCP server to delete.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Delete(string id)
    {
        return McpServerName.Create(id)
            .OnSuccessBind(_mcpServerService.Delete)
            .ToNoContentResult();
    }
}
