using Ave.Extensions.Functional;
using Core.Application.McpServers;
using Core.Domain.McpServers;
using Core.Infrastructure.WebApp.Extensions;
using Core.Infrastructure.WebApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace Core.Infrastructure.WebApp.Controllers;

/// <summary>
/// Controller for MCP server configuration endpoints.
/// </summary>
[ApiController]
[Route("v1/mcp-servers")]
public class McpServersController : ControllerBase
{
    private readonly IMcpServerService _mcpServerService;

    public McpServersController(IMcpServerService mcpServerService)
    {
        _mcpServerService = mcpServerService;
    }

    /// <summary>
    /// Gets a list of all configured MCP servers.
    /// </summary>
    /// <returns>A list of MCP server summary information.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<McpServerInfoResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return _mcpServerService
            .GetAll()
            .ToActionResult(Mapper.ToResponse);
    }

    /// <summary>
    /// Gets the full configuration for a specific MCP server.
    /// </summary>
    /// <param name="id">The identifier of the MCP server.</param>
    /// <returns>The server definition if found.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(McpServerDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetById(string id)
    {
        return McpServerId.Create(id)
            .OnSuccessBind(_mcpServerService.GetById)
            .ToActionResult(Mapper.ToResponse);
    }
}
