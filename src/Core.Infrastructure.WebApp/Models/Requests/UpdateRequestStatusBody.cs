namespace Core.Infrastructure.WebApp.Models.Requests;

/// <summary>
/// Request body for updating a request's status.
/// </summary>
/// <param name="Status">The desired status. Only "cancelled" is a valid value.</param>
public record UpdateRequestStatusBody(string Status);
