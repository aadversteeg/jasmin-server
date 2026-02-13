namespace Core.Domain.Events.Payloads;

/// <summary>
/// Payload for instance started events, carrying the configuration used to start.
/// </summary>
/// <param name="Configuration">The configuration used to start the instance.</param>
public record InstanceStartedPayload(EventConfiguration Configuration);
