using Core.Application.McpServers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Core.Infrastructure.ModelContextProtocol.InMemory;

/// <summary>
/// Background service that initializes MCP server instances on startup.
/// </summary>
public class McpServerConnectionInitializationService : BackgroundService
{
    private readonly IMcpServerRepository _repository;
    private readonly IMcpServerInstanceManager _instanceManager;
    private readonly ILogger<McpServerConnectionInitializationService> _logger;

    public McpServerConnectionInitializationService(
        IMcpServerRepository repository,
        IMcpServerInstanceManager instanceManager,
        ILogger<McpServerConnectionInitializationService> logger)
    {
        _repository = repository;
        _instanceManager = instanceManager;
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

    private async Task InitializeServerAsync(Core.Domain.McpServers.McpServerName serverName, CancellationToken stoppingToken)
    {
        try
        {
            var startResult = await _instanceManager.StartInstanceAsync(serverName, stoppingToken);

            if (startResult.IsFailure)
            {
                _logger.LogWarning("Failed to start MCP server {ServerName}: {Error}",
                    serverName.Value, startResult.Error.Message);
                return;
            }

            var instanceId = startResult.Value;
            _logger.LogInformation("Successfully started MCP server {ServerName} with instance {InstanceId}",
                serverName.Value, instanceId.Value);

            // Stop the instance after verifying connection
            var stopResult = await _instanceManager.StopInstanceAsync(instanceId, stoppingToken);

            if (stopResult.IsFailure)
            {
                _logger.LogWarning("Failed to stop MCP server {ServerName} instance {InstanceId}: {Error}",
                    serverName.Value, instanceId.Value, stopResult.Error.Message);
            }
            else
            {
                _logger.LogInformation("Successfully verified MCP server {ServerName}", serverName.Value);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Connection initialization cancelled for server {ServerName}", serverName.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing connection for server {ServerName}", serverName.Value);
        }
    }
}
