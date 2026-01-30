using Ave.Extensions.Functional;
using Core.Domain.McpServers;
using Core.Domain.Models;
using Microsoft.Extensions.Options;

namespace Core.Application.McpServers;

/// <summary>
/// Service for retrieving MCP server configurations from application settings.
/// </summary>
public class McpServerService : IMcpServerService
{
    private readonly McpServerOptions _options;

    public McpServerService(IOptions<McpServerOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public Result<IReadOnlyList<McpServerInfo>, Error> GetAll()
    {
        var servers = _options.Servers
            .Select(kvp => McpServerId.Create(kvp.Key)
                .OnSuccessMap(id => new McpServerInfo(id, kvp.Value.Command)))
            .Where(r => r.IsSuccess)
            .Select(r => r.Value)
            .OrderBy(s => s.Id.Value)
            .ToList();

        return Result<IReadOnlyList<McpServerInfo>, Error>.Success(servers);
    }

    /// <inheritdoc />
    public Result<Maybe<McpServerDefinition>, Error> GetById(McpServerId id)
    {
        if (!_options.Servers.TryGetValue(id.Value, out var entry))
        {
            return Result<Maybe<McpServerDefinition>, Error>.Success(Maybe<McpServerDefinition>.None);
        }

        var definition = new McpServerDefinition(
            id,
            entry.Command,
            entry.Args.AsReadOnly(),
            entry.Env.AsReadOnly());

        return Result<Maybe<McpServerDefinition>, Error>.Success(Maybe.From(definition));
    }
}
