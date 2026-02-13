namespace Core.Infrastructure.WebApp.Models.Requests;

/// <summary>
/// Response model for a request's status sub-resource.
/// </summary>
/// <param name="Status">The request status (pending, running, completed, failed, cancelled).</param>
public record RequestStatusResponse(string Status);
