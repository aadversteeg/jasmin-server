using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Core.Domain.Paging;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Extensions;
using Core.Infrastructure.WebApp.Models;
using Core.Infrastructure.WebApp.Models.McpServers;
using Core.Infrastructure.WebApp.Models.McpServers.Instances;
using Core.Infrastructure.WebApp.Models.McpServers.Prompts;
using Core.Infrastructure.WebApp.Models.McpServers.Resources;
using Core.Infrastructure.WebApp.Models.McpServers.Tools;
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
[Tags("McpServers")]
public class McpServersController : ControllerBase
{
    private readonly IMcpServerService _mcpServerService;
    private readonly IMcpServerConnectionStatusCache _statusCache;
    private readonly IMcpServerRequestStore _requestStore;
    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly McpServerStatusOptions _statusOptions;

    public McpServersController(
        IMcpServerService mcpServerService,
        IMcpServerConnectionStatusCache statusCache,
        IMcpServerRequestStore requestStore,
        IMcpServerInstanceManager instanceManager,
        IOptions<McpServerStatusOptions> statusOptions)
    {
        _mcpServerService = mcpServerService;
        _statusCache = statusCache;
        _requestStore = requestStore;
        _instanceManager = instanceManager;
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
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        return _mcpServerService
            .GetAll()
            .ToActionResult(info => Mapper.ToListResponse(info, resolvedTimeZone));
    }

    /// <summary>
    /// Gets details for a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="include">Optional comma-separated list of additional data to include (e.g., "configuration", "events", "requests", "instances", "tools", "prompts", "resources", or "all").</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>The server details if found.</returns>
    [HttpGet("{id}", Name = "GetMcpServerById")]
    [ProducesResponseType(typeof(DetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetById(string id, [FromQuery] string? include = null, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        var includeOptionsResult = McpServerIncludeOptions.Create(include);
        if (includeOptionsResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(includeOptionsResult.Error));
        }

        var includeOptions = includeOptionsResult.Value;

        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(serverNameResult.Error));
        }

        var serverName = serverNameResult.Value;

        // Check if server exists
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromError(definitionResult.Error));
        }

        if (!definitionResult.Value.HasValue)
        {
            return NotFound();
        }

        // Get status info
        var serverId = _statusCache.GetOrCreateId(serverName);
        var statusEntry = _statusCache.GetEntry(serverId);

        // Get optional configuration
        McpServerDefinition? definition = null;
        if (includeOptions.IncludeConfiguration)
        {
            definition = definitionResult.Value.Value;
        }

        // Get optional events
        IReadOnlyList<McpServerEvent>? events = null;
        if (includeOptions.IncludeEvents)
        {
            var eventsResult = _mcpServerService.GetEvents(serverName);
            if (eventsResult.IsSuccess)
            {
                events = eventsResult.Value;
            }
        }

        // Get optional requests
        IReadOnlyList<McpServerRequest>? requests = null;
        if (includeOptions.IncludeRequests)
        {
            requests = _requestStore.GetByServerName(serverName);
        }

        // Get optional instances
        IReadOnlyList<McpServerInstanceInfo>? instances = null;
        if (includeOptions.IncludeInstances)
        {
            instances = _instanceManager.GetRunningInstances(serverName);
        }

        // Get optional metadata for tools, prompts, resources
        McpServerMetadata? metadata = null;
        if (includeOptions.IncludeTools || includeOptions.IncludePrompts || includeOptions.IncludeResources)
        {
            metadata = _statusCache.GetMetadata(serverId);
        }

        return Ok(Mapper.ToDetailsResponse(
            serverName,
            statusEntry,
            resolvedTimeZone,
            definition,
            events,
            requests,
            instances,
            metadata,
            includeOptions.IncludeTools,
            includeOptions.IncludePrompts,
            includeOptions.IncludeResources));
    }

    /// <summary>
    /// Gets the configuration for a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <returns>The server configuration if found.</returns>
    [HttpGet("{id}/configuration", Name = "GetMcpServerConfiguration")]
    [ProducesResponseType(typeof(ConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetConfiguration(string id)
    {
        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(serverNameResult.Error));
        }

        var serverName = serverNameResult.Value;
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromError(definitionResult.Error));
        }

        if (!definitionResult.Value.HasValue)
        {
            return NotFound();
        }

        var definition = definitionResult.Value.Value;
        if (!definition.HasConfiguration)
        {
            return NotFound();
        }

        return Ok(Mapper.ToConfigurationResponse(definition));
    }

    /// <summary>
    /// Updates the configuration for a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="request">The new configuration.</param>
    /// <returns>The updated configuration.</returns>
    [HttpPut("{id}/configuration")]
    [ProducesResponseType(typeof(ConfigurationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult UpdateConfiguration(string id, [FromBody] ConfigurationRequest request)
    {
        return McpServerName.Create(id)
            .OnSuccessBind(serverId => Mapper.ToDomain(serverId, request))
            .OnSuccessBind(_mcpServerService.Update)
            .ToOkResult(Mapper.ToConfigurationResponse);
    }

    /// <summary>
    /// Deletes the configuration for a specific MCP server.
    /// The server entry remains but cannot be started without configuration.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id}/configuration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult DeleteConfiguration(string id)
    {
        return McpServerName.Create(id)
            .OnSuccessBind(_mcpServerService.DeleteConfiguration)
            .OnSuccessMap(_ => Unit.Value)
            .ToNoContentResult();
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
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        var pagingResult = PagingParameters.Create(page, pageSize);
        if (pagingResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(pagingResult.Error));
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
    /// Gets all running instances for a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>A list of running instances for the server.</returns>
    [HttpGet("{id}/instances", Name = "GetMcpServerInstances")]
    [ProducesResponseType(typeof(InstanceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetInstances(string id, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(serverNameResult.Error));
        }

        var serverName = serverNameResult.Value;

        // Check if server exists
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromError(definitionResult.Error));
        }

        if (!definitionResult.Value.HasValue)
        {
            return NotFound();
        }

        var instances = _instanceManager.GetRunningInstances(serverName);
        return Ok(InstanceMapper.ToListResponse(instances, resolvedTimeZone));
    }

    /// <summary>
    /// Gets a specific running instance for an MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="instanceId">The identifier of the instance.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>The instance details if found.</returns>
    [HttpGet("{id}/instances/{instanceId}", Name = "GetMcpServerInstance")]
    [ProducesResponseType(typeof(InstanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetInstance(string id, string instanceId, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(serverNameResult.Error));
        }

        var serverName = serverNameResult.Value;

        // Check if server exists
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromError(definitionResult.Error));
        }

        if (!definitionResult.Value.HasValue)
        {
            return NotFound();
        }

        var mcpInstanceId = McpServerInstanceId.From(instanceId);
        var instance = _instanceManager.GetInstance(serverName, mcpInstanceId);

        if (instance == null)
        {
            return NotFound();
        }

        return Ok(InstanceMapper.ToResponse(instance, resolvedTimeZone));
    }

    /// <summary>
    /// Gets the tools exposed by a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>A list of tools exposed by the server.</returns>
    [HttpGet("{id}/tools", Name = "GetMcpServerTools")]
    [ProducesResponseType(typeof(ToolListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetTools(string id, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(serverNameResult.Error));
        }

        var serverName = serverNameResult.Value;

        // Check if server exists
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromError(definitionResult.Error));
        }

        if (!definitionResult.Value.HasValue)
        {
            return NotFound();
        }

        var serverId = _statusCache.GetOrCreateId(serverName);
        var metadata = _statusCache.GetMetadata(serverId);

        return Ok(ToolMapper.ToListResponse(metadata, resolvedTimeZone));
    }

    /// <summary>
    /// Gets the prompts exposed by a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>A list of prompts exposed by the server.</returns>
    [HttpGet("{id}/prompts", Name = "GetMcpServerPrompts")]
    [ProducesResponseType(typeof(PromptListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetPrompts(string id, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(serverNameResult.Error));
        }

        var serverName = serverNameResult.Value;

        // Check if server exists
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromError(definitionResult.Error));
        }

        if (!definitionResult.Value.HasValue)
        {
            return NotFound();
        }

        var serverId = _statusCache.GetOrCreateId(serverName);
        var metadata = _statusCache.GetMetadata(serverId);

        return Ok(PromptMapper.ToListResponse(metadata, resolvedTimeZone));
    }

    /// <summary>
    /// Gets the resources exposed by a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>A list of resources exposed by the server.</returns>
    [HttpGet("{id}/resources", Name = "GetMcpServerResources")]
    [ProducesResponseType(typeof(ResourceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetResources(string id, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.Single("INVALID_TIMEZONE", $"Invalid timezone: {timeZone}"));
        }

        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(serverNameResult.Error));
        }

        var serverName = serverNameResult.Value;

        // Check if server exists
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, ErrorResponse.FromError(definitionResult.Error));
        }

        if (!definitionResult.Value.HasValue)
        {
            return NotFound();
        }

        var serverId = _statusCache.GetOrCreateId(serverName);
        var metadata = _statusCache.GetMetadata(serverId);

        return Ok(ResourceMapper.ToListResponse(metadata, resolvedTimeZone));
    }

    /// <summary>
    /// Creates a new MCP server.
    /// </summary>
    /// <param name="request">The server details to create.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>The created server details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DetailsResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Create([FromBody] CreateRequest request, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone) ?? TimeZoneInfo.Utc;

        return Mapper.ToDomain(request)
            .OnSuccessBind(_mcpServerService.Create)
            .ToCreatedResult(
                "GetMcpServerById",
                def => new { id = def.Id.Value },
                def => Mapper.ToDetailsResponseAfterCreate(def, resolvedTimeZone));
    }

    /// <summary>
    /// Deletes an MCP server.
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
