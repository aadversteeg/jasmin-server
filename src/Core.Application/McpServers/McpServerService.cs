using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;

namespace Core.Application.McpServers;

/// <summary>
/// Service for retrieving MCP server configurations.
/// </summary>
public class McpServerService : IMcpServerService
{
    private readonly IMcpServerRepository _repository;

    public McpServerService(IMcpServerRepository repository)
    {
        _repository = repository;
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<McpServerInfo>, Error> GetAll()
    {
        return _repository.GetAll();
    }

    /// <inheritdoc />
    public Result<Maybe<McpServerDefinition>, Error> GetById(McpServerId id)
    {
        return _repository.GetById(id);
    }
}
