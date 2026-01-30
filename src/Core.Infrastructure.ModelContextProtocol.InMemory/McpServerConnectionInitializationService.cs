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

    private async Task InitializeServerAsync(McpServerName serverName, CancellationToken stoppingToken)
    {
        var serverId = _statusCache.GetOrCreateId(serverName);

        try
        {
            var definitionResult = _repository.GetById(serverName);
            if (definitionResult.IsFailure || !definitionResult.Value.HasValue)
            {
                _logger.LogWarning("Could not retrieve definition for server {ServerName}", serverName.Value);
                _statusCache.RecordEvent(serverId, McpServerEventType.StartFailed, "Server definition not found");
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                return;
            }

            var definition = definitionResult.Value.Value;

            // Record starting event
            _statusCache.RecordEvent(serverId, McpServerEventType.Starting);

            bool success;
            string? errorMessage = null;
            try
            {
                success = await _client.TestConnectionAsync(definition, stoppingToken);
            }
            catch (Exception ex)
            {
                success = false;
                errorMessage = ex.Message;
            }

            if (success)
            {
                // Record started then stopping then stopped (since TestConnectionAsync starts and stops)
                _statusCache.RecordEvent(serverId, McpServerEventType.Started);
                _statusCache.RecordEvent(serverId, McpServerEventType.Stopping);
                _statusCache.RecordEvent(serverId, McpServerEventType.Stopped);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Verified);
                _logger.LogInformation("Successfully connected to MCP server {ServerName}", serverName.Value);
            }
            else
            {
                _statusCache.RecordEvent(serverId, McpServerEventType.StartFailed, errorMessage);
                _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
                _logger.LogWarning("Failed to connect to MCP server {ServerName}", serverName.Value);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Connection initialization cancelled for server {ServerName}", serverName.Value);
        }
        catch (Exception ex)
        {
            _statusCache.RecordEvent(serverId, McpServerEventType.StartFailed, ex.Message);
            _statusCache.SetStatus(serverId, McpServerConnectionStatus.Failed);
            _logger.LogError(ex, "Error initializing connection for server {ServerName}", serverName.Value);
        }
    }
}
