using System.Text.Json;
using Core.Application.Requests;
using Core.Domain.Models;
using Core.Domain.Requests;
using Core.Domain.Requests.Parameters;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using Error = Ave.Extensions.ErrorPaths.Error;

namespace Core.Infrastructure.ModelContextProtocol.Hosting.RequestHandlers;

/// <summary>
/// Handles <c>mcp-server.test-configuration</c> requests by starting a temporary MCP server
/// process, verifying connectivity, and capturing stderr output.
/// </summary>
public class McpServerTestConfigurationHandler : IRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static JsonElement BuildOutput(bool success, List<string> stderrLines) =>
        JsonSerializer.SerializeToElement(new { success, stderr = stderrLines });

    private readonly ILogger<McpServerTestConfigurationHandler> _logger;
    private readonly TimeSpan _connectionTimeout;

    public McpServerTestConfigurationHandler(
        ILogger<McpServerTestConfigurationHandler> logger,
        McpServerHostingOptions options)
    {
        _logger = logger;
        _connectionTimeout = TimeSpan.FromSeconds(options.ConnectionTimeoutSeconds);
    }

    public async Task<RequestHandlerResult> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        if (!request.Parameters.HasValue)
        {
            return RequestHandlerResult.Failure([Errors.CommandRequired]);
        }

        var parameters = JsonSerializer.Deserialize<TestConfigurationParameters>(
            request.Parameters.Value.GetRawText(), JsonOptions);

        if (parameters == null || string.IsNullOrWhiteSpace(parameters.Command))
        {
            return RequestHandlerResult.Failure([Errors.CommandRequired]);
        }

        _logger.LogDebug("Testing MCP server configuration: command={Command}", parameters.Command);

        var stderrLines = new List<string>();

        try
        {
            var transportOptions = new StdioClientTransportOptions
            {
                Command = parameters.Command,
                Arguments = parameters.Args?.ToList() ?? [],
                Name = "test-configuration",
                EnvironmentVariables = parameters.Env?.ToDictionary(
                    kvp => kvp.Key, kvp => (string?)kvp.Value) ?? new Dictionary<string, string?>(),
                StandardErrorLines = line => stderrLines.Add(line)
            };

            var transport = new StdioClientTransport(transportOptions);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(_connectionTimeout);

            McpClient client;
            try
            {
                client = await McpClient.CreateAsync(transport, cancellationToken: timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                var output = BuildOutput(false, stderrLines);
                return RequestHandlerResult.Failure(output,
                    [new Error(ErrorCodes.McpServers.TestConfiguration.Timeout,
                        $"Connection timed out after {_connectionTimeout.TotalSeconds} seconds.")]);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Configuration test failed: could not start MCP server");
                var output = BuildOutput(false, stderrLines);
                return RequestHandlerResult.Failure(output,
                    [new Error(ErrorCodes.McpServers.TestConfiguration.ConnectionFailed,
                        $"Failed to start MCP server: {ex.Message}")]);
            }

            _logger.LogDebug("Configuration test passed: MCP server started successfully");
            await client.DisposeAsync();

            return RequestHandlerResult.Success(BuildOutput(true, stderrLines));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Configuration test cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Configuration test error");
            var output = BuildOutput(false, stderrLines);
            return RequestHandlerResult.Failure(output,
                [new Error(ErrorCodes.McpServers.TestConfiguration.ConnectionFailed,
                    $"Configuration test error: {ex.Message}")]);
        }
    }
}
