using System;

namespace Hecateon.Modules.Prometheon.Models
{
    /// <summary>
    /// Cognitive reframing technique type.
    /// </summary>
    public enum ReframeType
    {
        /// <summary>
        /// Cognitive restructuring - change thought pattern
        /// </summary>
        CognitiveRestructuring,

        /// <summary>
        /// Perspective shift - view from different angle
        /// </summary>
        PerspectiveShift,

        /// <summary>
        /// Temporal reframe - past/present/future lens
        /// </summary>
        TemporalReframe,

        /// <summary>
        /// Context reframe - change situational context
        /// </summary>
        ContextReframe,

        /// <summary>
        /// Meaning reframe - reinterpret significance
        /// </summary>
        MeaningReframe,

        /// <summary>
        /// Decatastrophizing - reduce catastrophic thinking
        /// </summary>
        Decatastrophizing
    }

    /// <summary>
    /// Narrative reframing event.
    /// Records a cognitive reframe intervention.
    /// Immutable, append-only.
    /// </summary>
    public record NarrativeFrame
    {
        /// <summary>
        /// Unique identifier.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// UTC timestamp.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Original narrative/thought pattern.
        /// </summary>
        public string OriginalNarrative { get; init; } = string.Empty;

        /// <summary>
        /// Reframed narrative/thought pattern.
        /// </summary>
        public string ReframedNarrative { get; init; } = string.Empty;

        /// <summary>
        /// Type of reframe technique used.
        /// </summary>
        public ReframeType Type { get; init; }

        /// <summary>
        /// Cognitive distortions identified in original.
        /// (e.g., "All-or-nothing thinking", "Overgeneralization")
        /// </summary>
        public string[] IdentifiedDistortions { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Effectiveness score (0.0 - 1.0).
        /// Measured by subsequent emotional/cognitive shifts.
        /// </summary>
        public double? EffectivenessScore { get; init; }

        /// <summary>
        /// User acceptance of reframe (true if adopted).
        /// </summary>
        public bool? UserAccepted { get; init; }

        /// <summary>
        /// Confidence in this reframe (0.0 - 1.0).
        /// </summary>
        public double Confidence { get; init; } = 0.8;

        /// <summary>
        /// Explanation of the reframing logic.
        /// </summary>
        public string Explanation { get; init; } = string.Empty;

        /// <summary>
        /// Operator state before reframe.
        /// </summary>
        public Guid? BeforeStateId { get; init; }

        /// <summary>
        /// Operator state after reframe (if measured).
        /// </summary>
        public Guid? AfterStateId { get; init; }

        /// <summary>
        /// Related transaction that triggered this reframe.
        /// </summary>
        public Guid? TriggeringTransactionId { get; init; }

        /// <summary>
        /// Tags for categorization.
        /// </summary>
        public string[] Tags { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Algorithm version.
        /// </summary>
        public string AlgorithmVersion { get; init; } = "1.0.0";
    }
}
