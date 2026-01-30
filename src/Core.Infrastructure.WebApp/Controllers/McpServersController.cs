using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Infrastructure.ModelContextProtocol.InMemory;
using Core.Infrastructure.WebApp.Extensions;
using Core.Infrastructure.WebApp.Models.McpServers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
    /// <returns>The server definition if found.</returns>
    [HttpGet("{id}", Name = "GetMcpServerById")]
    [ProducesResponseType(typeof(DetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetById(string id)
    {
        return McpServerName.Create(id)
            .OnSuccessBind(_mcpServerService.GetById)
            .ToActionResult(Mapper.ToDetailsResponse);
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
