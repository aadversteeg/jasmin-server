namespace Core.Domain.Requests.Parameters;

/// <summary>
/// Parameters for the read-resource request action.
/// </summary>
/// <param name="ResourceUri">The URI of the resource to read.</param>
public record ReadResourceParameters(string ResourceUri);
