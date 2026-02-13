namespace Core.Domain.Requests;

/// <summary>
/// Status of a request.
/// </summary>
public enum RequestStatus
{
    /// <summary>
    /// Request has been created but not yet started processing.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Request is currently being processed.
    /// </summary>
    Running = 1,

    /// <summary>
    /// Request completed successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Request failed.
    /// </summary>
    Failed = 3
}
