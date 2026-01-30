using Core.Application.McpServers;
using Core.Domain.McpServers;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace Core.Infrastructure.ModelContextProtocol.Hosting;

/// <summary>
/// Implementation of <see cref="IMcpServerClient"/> that uses the ModelContextProtocol library.
/// </summary>
public class McpServerClient : IMcpServerClient
{
    private readonly ILogger<McpServerClient> _logger;

    public McpServerClient(ILogger<McpServerClient> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(McpServerDefinition definition, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Testing connection to MCP server {ServerId}", definition.Id.Value);

            var transportOptions = new StdioClientTransportOptions
            {
                Command = definition.Command,
                Arguments = [.. definition.Args],
                Name = definition.Id.Value,
                EnvironmentVariables = definition.Env.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value)
            };

            var transport = new StdioClientTransport(transportOptions);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            var client = await McpClient.CreateAsync(transport, cancellationToken: timeoutCts.Token);
            await client.DisposeAsync();

            _logger.LogDebug("Successfully connected to MCP server {ServerId}", definition.Id.Value);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Connection test cancelled for MCP server {ServerId}", definition.Id.Value);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to connect to MCP server {ServerId}", definition.Id.Value);
            return false;
        }
    }
}
