using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Models.McpServers.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Core.Infrastructure.WebApp.Controllers;

/// <summary>
/// Controller for MCP server async request endpoints.
/// </summary>
[ApiController]
[Route("v1/mcp-servers/{serverId}/requests")]
public class McpServerRequestsController : ControllerBase
{
    private readonly IMcpServerService _mcpServerService;
    private readonly IMcpServerRequestStore _requestStore;
    private readonly IMcpServerRequestQueue _requestQueue;
    private readonly McpServerStatusOptions _statusOptions;

    public McpServerRequestsController(
        IMcpServerService mcpServerService,
        IMcpServerRequestStore requestStore,
        IMcpServerRequestQueue requestQueue,
        IOptions<McpServerStatusOptions> statusOptions)
    {
        _mcpServerService = mcpServerService;
        _requestStore = requestStore;
        _requestQueue = requestQueue;
        _statusOptions = statusOptions.Value;
    }

    /// <summary>
    /// Creates a new async request (start or stop) for the server.
    /// </summary>
    /// <param name="serverId">The identifier of the MCP server.</param>
    /// <param name="request">The request details.</param>
    /// <param name="timeZone">Optional timezone for timestamps.</param>
    /// <returns>The created request with initial status.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Create(string serverId, [FromBody] CreateRequestRequest request, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(new { error = $"Invalid timezone: {timeZone}" });
        }

        // Validate server name
        var serverNameResult = McpServerName.Create(serverId);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(new { error = serverNameResult.Error.Message });
        }

        var serverName = serverNameResult.Value;

        // Verify server exists
        var serverResult = _mcpServerService.GetById(serverName);
        if (serverResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = serverResult.Error.Message });
        }

        if (!serverResult.Value.HasValue)
        {
            return NotFound(new { error = $"MCP server '{serverId}' not found" });
        }

        // Create domain request
        var domainRequestResult = RequestMapper.ToDomain(serverName, request);
        if (domainRequestResult.IsFailure)
        {
            return BadRequest(new { error = domainRequestResult.Error.Message });
        }

        var domainRequest = domainRequestResult.Value;

        // Store and enqueue for processing
        _requestStore.Add(domainRequest);
        _requestQueue.Enqueue(domainRequest);

        var response = RequestMapper.ToResponse(domainRequest, resolvedTimeZone);

        return AcceptedAtRoute(
            "GetMcpServerRequestById",
            new { serverId = serverId, requestId = domainRequest.Id.Value },
            response);
    }

    /// <summary>
    /// Gets all requests for a specific server.
    /// </summary>
    /// <param name="serverId">The identifier of the MCP server.</param>
    /// <param name="timeZone">Optional timezone for timestamps.</param>
    /// <returns>A list of requests for the server.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RequestResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetAll(string serverId, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(new { error = $"Invalid timezone: {timeZone}" });
        }

        var serverNameResult = McpServerName.Create(serverId);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(new { error = serverNameResult.Error.Message });
        }

        var serverName = serverNameResult.Value;

        // Verify server exists
        var serverResult = _mcpServerService.GetById(serverName);
        if (serverResult.IsFailure)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = serverResult.Error.Message });
        }

        if (!serverResult.Value.HasValue)
        {
            return NotFound(new { error = $"MCP server '{serverId}' not found" });
        }

        var requests = _requestStore.GetByServerName(serverName);
        var responses = requests.Select(r => RequestMapper.ToResponse(r, resolvedTimeZone)).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Gets a specific request by ID.
    /// </summary>
    /// <param name="serverId">The identifier of the MCP server.</param>
    /// <param name="requestId">The identifier of the request.</param>
    /// <param name="timeZone">Optional timezone for timestamps.</param>
    /// <returns>The request details.</returns>
    [HttpGet("{requestId}", Name = "GetMcpServerRequestById")]
    [ProducesResponseType(typeof(RequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult GetById(string serverId, string requestId, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(new { error = $"Invalid timezone: {timeZone}" });
        }

        var serverNameResult = McpServerName.Create(serverId);
        if (serverNameResult.IsFailure)
        {
            return BadRequest(new { error = serverNameResult.Error.Message });
        }

        var requestIdObj = McpServerRequestId.From(requestId);
        var maybeRequest = _requestStore.GetById(requestIdObj);

        if (maybeRequest.HasNoValue)
        {
            return NotFound(new { error = $"Request '{requestId}' not found" });
        }

        var request = maybeRequest.Value;

        // Verify the request belongs to the specified server
        if (request.ServerName.Value != serverNameResult.Value.Value)
        {
            return NotFound(new { error = $"Request '{requestId}' not found for server '{serverId}'" });
        }

        return Ok(RequestMapper.ToResponse(request, resolvedTimeZone));
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
