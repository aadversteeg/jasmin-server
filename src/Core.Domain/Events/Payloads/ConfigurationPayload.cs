namespace Core.Domain.Events.Payloads;

/// <summary>
/// Payload for configuration change events, carrying old and new configuration.
/// </summary>
/// <param name="OldConfiguration">The previous configuration, or null for creation events.</param>
/// <param name="NewConfiguration">The new configuration, or null for deletion events.</param>
public record ConfigurationPayload(
    EventConfiguration? OldConfiguration,
    EventConfiguration? NewConfiguration);
