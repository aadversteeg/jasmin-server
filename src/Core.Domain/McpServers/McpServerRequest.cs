namespace Core.Domain.McpServers;

/// <summary>
/// Represents an async request to start or stop an MCP server instance.
/// </summary>
public class McpServerRequest
{
    public McpServerRequestId Id { get; }
    public McpServerName ServerName { get; }
    public McpServerRequestAction Action { get; }
    public McpServerRequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? CompletedAtUtc { get; private set; }

    /// <summary>
    /// For stop actions: the instance ID to stop.
    /// </summary>
    public McpServerInstanceId? TargetInstanceId { get; }

    /// <summary>
    /// For start actions: the instance ID created when completed.
    /// </summary>
    public McpServerInstanceId? ResultInstanceId { get; private set; }

    /// <summary>
    /// Errors if the request failed.
    /// </summary>
    public IReadOnlyList<McpServerRequestError>? Errors { get; private set; }

    public McpServerRequest(
        McpServerRequestId id,
        McpServerName serverName,
        McpServerRequestAction action,
        McpServerInstanceId? targetInstanceId = null)
    {
        Id = id;
        ServerName = serverName;
        Action = action;
        Status = McpServerRequestStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        TargetInstanceId = targetInstanceId;
    }

    public void MarkRunning()
    {
        Status = McpServerRequestStatus.Running;
    }

    public void MarkCompleted(McpServerInstanceId? resultInstanceId = null)
    {
        Status = McpServerRequestStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        ResultInstanceId = resultInstanceId;
    }

    public void MarkFailed(IReadOnlyList<McpServerRequestError> errors)
    {
        Status = McpServerRequestStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        Errors = errors;
    }
}
