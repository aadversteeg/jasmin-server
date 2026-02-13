using System.Text.Json;

namespace Core.Domain.Requests;

/// <summary>
/// Represents a generic async request with typed parameters and output.
/// </summary>
public class Request
{
    public RequestId Id { get; }
    public RequestAction Action { get; }
    public string Target { get; }
    public RequestStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? CompletedAtUtc { get; private set; }
    public JsonElement? Parameters { get; }
    public JsonElement? Output { get; private set; }
    public IReadOnlyList<RequestError>? Errors { get; private set; }

    public Request(
        RequestId id,
        RequestAction action,
        string target,
        JsonElement? parameters = null)
    {
        Id = id;
        Action = action;
        Target = target;
        Status = RequestStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        Parameters = parameters;
    }

    public void MarkRunning()
    {
        Status = RequestStatus.Running;
    }

    public void MarkCompleted(JsonElement? output = null)
    {
        Status = RequestStatus.Completed;
        CompletedAtUtc = DateTime.UtcNow;
        Output = output;
    }

    public void MarkFailed(IReadOnlyList<RequestError> errors)
    {
        Status = RequestStatus.Failed;
        CompletedAtUtc = DateTime.UtcNow;
        Errors = errors;
    }
}
