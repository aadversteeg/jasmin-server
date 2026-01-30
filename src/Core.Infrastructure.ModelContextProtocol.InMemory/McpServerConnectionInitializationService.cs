using Core.Application.McpServers;
using Core.Domain.McpServers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Background service that initializes MCP server connection status on startup.
/// </summary>
public class McpServerConnectionInitializationService : BackgroundService
{
    private readonly IMcpServerRepository _repository;
    private readonly IMcpServerClient _client;
    private readonly IMcpServerConnectionStatusCache _statusCache;
    private readonly ILogger<McpServerConnectionInitializationService> _logger;

    public McpServerConnectionInitializationService(
        IMcpServerRepository repository,
        IMcpServerClient client,
        IMcpServerConnectionStatusCache statusCache,
        ILogger<McpServerConnectionInitializationService> logger)
    {
        _repository = repository;
        _client = client;
        _statusCache = statusCache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting MCP server connection initialization");

        var serversResult = _repository.GetAll();
        if (serversResult.IsFailure)
        {
            _logger.LogError("Failed to retrieve MCP servers: {Error}", serversResult.Error.Message);
            return;
        }

        var tasks = serversResult.Value.Select(server => InitializeServerAsync(server.Id, stoppingToken));
        await Task.WhenAll(tasks);

        _logger.LogInformation("MCP server connection initialization completed");
    }

    private async Task InitializeServerAsync(McpServerId serverId, CancellationToken stoppingToken)
    {
        try
        {
            var definitionResult = _repository.GetById(serverId);
            if (definitionResult.IsFailure || !definitionResult.Value.HasValue)
            {
                _logger.LogWarning("Could not retrieve definition for server {ServerId}", serverId.Value);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return;
            }

            var definition = definitionResult.Value.Value;
            var success = await _client.TestConnectionAsync(definition, stoppingToken);

            var status = success ? McpServerConnectionStatus.Verified : McpServerConnectionStatus.Failed;
            _statusCache.SetStatus(serverId, status);

            if (success)
            {
                _logger.LogInformation("Successfully connected to MCP server {ServerId}", serverId.Value);
            }
            else
            {
                _logger.LogWarning("Failed to connect to MCP server {ServerId}", serverId.Value);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Connection initialization cancelled for server {ServerId}", serverId.Value);
        }
        catch (Exception ex)
        {
            _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
            _logger.LogError(ex, "Error initializing connection for server {ServerId}", serverId.Value);
        }
    }
}
