using System;

namespace Lullaby.Modules.Prometheon.Models
{
    /// <summary>
    /// Base event type for all Prometheon cognitive events.
    /// Follows append-only, deterministic principles.
    /// </summary>
    public abstract class CognitiveEvent
    {
        /// <summary>
        /// Unique identifier for this event (immutable).
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// UTC timestamp when event was created (immutable).
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Event type discriminator.
        /// </summary>
        public string EventType { get; init; } = string.Empty;

        /// <summary>
        /// Version of the Prometheon engine that created this event.
        /// </summary>
        public string EngineVersion { get; init; } = "1.0.0";

        /// <summary>
        /// User ID (for multi-user systems).
        /// </summary>
        public string? UserId { get; init; }

        /// <summary>
        /// Serialized event data (JSON).
        /// </summary>
        public string Data { get; init; } = string.Empty;

        /// <summary>
        /// Hash of event data for integrity verification.
        /// </summary>
        public string DataHash { get; init; } = string.Empty;
    }
}
