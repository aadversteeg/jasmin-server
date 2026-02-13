using System.Text.Json;
using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Domain.Models;
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
using Error = Ave.Extensions.ErrorPaths.Error;

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
    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly IMcpServerInstanceLogStore _logStore;
    private readonly McpServerStatusOptions _statusOptions;
    private readonly JsonSerializerOptions _jsonOptions;

    public McpServersController(
        IMcpServerService mcpServerService,
        IMcpServerConnectionStatusCache statusCache,
        IMcpServerInstanceManager instanceManager,
        IMcpServerInstanceLogStore logStore,
        IOptions<McpServerStatusOptions> statusOptions,
        IOptions<JsonOptions> jsonOptions)
    {
        _mcpServerService = mcpServerService;
        _statusCache = statusCache;
        _instanceManager = instanceManager;
        _logStore = logStore;
        _statusOptions = statusOptions.Value;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
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
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
        }

        return _mcpServerService
            .GetAll()
            .ToActionResult(info => Mapper.ToListResponse(info, resolvedTimeZone));
    }

    /// <summary>
    /// Gets details for a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="include">Optional comma-separated list of additional data to include (e.g., "configuration", "instances", "tools", "prompts", "resources", or "all").</param>
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
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
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
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
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
    /// <param name="include">Optional comma-separated list of additional data to include (e.g., "tools", "prompts", "resources", or "all").</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>The instance details if found.</returns>
    [HttpGet("{id}/instances/{instanceId}", Name = "GetMcpServerInstance")]
    [ProducesResponseType(typeof(InstanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetInstance(string id, string instanceId, [FromQuery] string? include = null, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
        }

        var includeOptionsResult = McpServerInstanceIncludeOptions.Create(include);
        if (includeOptionsResult.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(includeOptionsResult.Error));
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

        return Ok(InstanceMapper.ToResponse(instance, resolvedTimeZone, includeOptionsResult.Value));
    }

    /// <summary>
    /// Gets the tools exposed by a specific MCP server instance.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="instanceId">The identifier of the instance.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>A list of tools exposed by the instance.</returns>
    [HttpGet("{id}/instances/{instanceId}/tools", Name = "GetMcpServerInstanceTools")]
    [ProducesResponseType(typeof(ToolListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetInstanceTools(string id, string instanceId, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
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

        return Ok(ToolMapper.ToListResponse(instance.Metadata, resolvedTimeZone));
    }

    /// <summary>
    /// Gets the prompts exposed by a specific MCP server instance.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="instanceId">The identifier of the instance.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>A list of prompts exposed by the instance.</returns>
    [HttpGet("{id}/instances/{instanceId}/prompts", Name = "GetMcpServerInstancePrompts")]
    [ProducesResponseType(typeof(PromptListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetInstancePrompts(string id, string instanceId, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
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

        return Ok(PromptMapper.ToListResponse(instance.Metadata, resolvedTimeZone));
    }

    /// <summary>
    /// Gets the resources exposed by a specific MCP server instance.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="instanceId">The identifier of the instance.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>A list of resources exposed by the instance.</returns>
    [HttpGet("{id}/instances/{instanceId}/resources", Name = "GetMcpServerInstanceResources")]
    [ProducesResponseType(typeof(ResourceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetInstanceResources(string id, string instanceId, [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
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

        return Ok(ResourceMapper.ToListResponse(instance.Metadata, resolvedTimeZone));
    }

    /// <summary>
    /// Gets the stderr log entries for a specific MCP server instance with cursor-based pagination.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="instanceId">The identifier of the instance.</param>
    /// <param name="afterLine">Return entries after this line number. Use 0 to start from the beginning.</param>
    /// <param name="limit">Maximum number of entries to return (1-1000). Default: 100.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <returns>A paginated list of log entries.</returns>
    [HttpGet("{id}/instances/{instanceId}/logs", Name = "GetMcpServerInstanceLogs")]
    [ProducesResponseType(typeof(LogListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetInstanceLogs(
        string id,
        string instanceId,
        [FromQuery] long afterLine = 0,
        [FromQuery] int limit = 100,
        [FromQuery] string? timeZone = null)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
        }

        if (limit < 1 || limit > 1000)
        {
            return BadRequest(ErrorResponse.Single("INVALID_LIMIT", "Limit must be between 1 and 1000"));
        }

        if (afterLine < 0)
        {
            return BadRequest(ErrorResponse.Single("INVALID_AFTER_LINE", "afterLine must be 0 or greater"));
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

        var entries = _logStore.GetEntries(mcpInstanceId, afterLine, limit);
        var totalItems = _logStore.GetCount(mcpInstanceId);

        return Ok(LogMapper.ToListResponse(entries, totalItems, resolvedTimeZone));
    }

    /// <summary>
    /// Streams stderr log entries for a specific MCP server instance using Server-Sent Events (SSE).
    /// Supports reconnection via Last-Event-ID header (parsed as line number).
    /// First sends all buffered entries after the specified line, then streams new entries live.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <param name="instanceId">The identifier of the instance.</param>
    /// <param name="afterLine">Start streaming from entries after this line number. Use 0 to start from the beginning.</param>
    /// <param name="timeZone">Optional timezone for timestamps. Defaults to configured timezone or UTC.</param>
    /// <param name="cancellationToken">Cancellation token for the stream.</param>
    [HttpGet("{id}/instances/{instanceId}/logs/stream", Name = "StreamMcpServerInstanceLogs")]
    [Produces("text/event-stream")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task StreamInstanceLogs(
        string id,
        string instanceId,
        [FromQuery] long afterLine = 0,
        [FromQuery] string? timeZone = null,
        CancellationToken cancellationToken = default)
    {
        var resolvedTimeZone = ResolveTimeZone(timeZone);
        if (resolvedTimeZone == null)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var serverNameResult = McpServerName.Create(id);
        if (serverNameResult.IsFailure)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var serverName = serverNameResult.Value;

        // Check if server exists
        var definitionResult = _mcpServerService.GetById(serverName);
        if (definitionResult.IsFailure)
        {
            Response.StatusCode = StatusCodes.Status500InternalServerError;
            return;
        }

        if (!definitionResult.Value.HasValue)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var mcpInstanceId = McpServerInstanceId.From(instanceId);
        var instance = _instanceManager.GetInstance(serverName, mcpInstanceId);

        if (instance == null)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        // Check for Last-Event-ID header (takes precedence over query parameter)
        var lastEventIdHeader = Request.Headers["Last-Event-ID"].FirstOrDefault();
        if (!string.IsNullOrEmpty(lastEventIdHeader) && long.TryParse(lastEventIdHeader, out var parsedLine))
        {
            afterLine = parsedLine;
        }

        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.ContentType = "text/event-stream";

        await Response.WriteAsync(": connected\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        // Step 1: Subscribe BEFORE catching up to avoid missing entries
        var (subscriptionId, reader) = _logStore.Subscribe(mcpInstanceId);
        try
        {
            // Step 2: Catchup — send all buffered entries after the specified line
            var buffered = _logStore.GetEntries(mcpInstanceId, afterLine, int.MaxValue);
            var lastSentLine = afterLine;

            foreach (var entry in buffered)
            {
                await SendSseLogEntryAsync(entry, resolvedTimeZone, cancellationToken);
                lastSentLine = entry.LineNumber;
            }

            // Step 3: Live — stream from channel, skipping entries already sent during catchup
            await foreach (var entry in reader.ReadAllAsync(cancellationToken))
            {
                if (entry.LineNumber <= lastSentLine)
                {
                    continue;
                }

                lastSentLine = entry.LineNumber;
                await SendSseLogEntryAsync(entry, resolvedTimeZone, cancellationToken);
            }
        }
        finally
        {
            _logStore.Unsubscribe(mcpInstanceId, subscriptionId);
        }
    }

    private async Task SendSseLogEntryAsync(
        McpServerInstanceLogEntry entry,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken)
    {
        var response = LogMapper.ToResponse(entry, timeZone);
        var json = JsonSerializer.Serialize(response, _jsonOptions);

        await Response.WriteAsync($"id: {entry.LineNumber}\n", cancellationToken);
        await Response.WriteAsync("event: instance-log\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
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
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
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
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
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
            return BadRequest(ErrorResponse.FromError(new Error(ErrorCodes.InvalidTimezone, $"Invalid timezone: {timeZone}")));
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
    /// Tests an MCP server configuration without persisting it.
    /// Starts a temporary instance with the provided configuration and immediately stops it.
    /// </summary>
    /// <param name="request">The configuration to test.</param>
    /// <returns>Success if the configuration is valid and the server can start.</returns>
    [HttpPost("test-configuration")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TestConfiguration([FromBody] TestConfigurationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return BadRequest(ErrorResponse.Single("INVALID_COMMAND", "Command is required"));
        }

        var result = await _instanceManager.TestConfigurationAsync(
            request.Command,
            request.Args,
            request.Env,
            cancellationToken);

        if (result.IsFailure)
        {
            return BadRequest(ErrorResponse.FromError(result.Error));
        }

        return Ok(new { success = true });
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
