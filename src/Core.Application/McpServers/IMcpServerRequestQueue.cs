using Core.Domain.McpServers;

namespace Core.Application.McpServers;

/// <summary>
/// Queue for MCP server requests to be processed sequentially.
/// </summary>
public interface IMcpServerRequestQueue
{
    /// <summary>
    /// Enqueues a request for background processing.
    /// </summary>
    /// <param name="request">The request to enqueue.</param>
    void Enqueue(McpServerRequest request);

    /// <summary>
    /// Dequeues the next request for processing.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The next request to process.</returns>
    ValueTask<McpServerRequest> DequeueAsync(CancellationToken cancellationToken);
}
