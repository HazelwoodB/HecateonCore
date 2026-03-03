using System;

namespace Lullaby.Modules.Prometheon.Models
{
    /// <summary>
    /// Types of transactional patterns in Transactional Analysis.
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Complementary - expected response (e.g., Adult→Adult)
        /// </summary>
        Complementary,

        /// <summary>
        /// Crossed - unexpected response (e.g., Adult→Child response)
        /// </summary>
        Crossed,

        /// <summary>
        /// Ulterior - hidden message beneath surface communication
        /// </summary>
        Ulterior
    }

    /// <summary>
    /// Transactional Analysis event.
    /// Records a communicative transaction between ego states.
    /// Immutable, append-only.
    /// </summary>
    public record Transaction
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
        /// Ego state initiating the transaction.
        /// </summary>
        public EgoState FromState { get; init; }

        /// <summary>
        /// Ego state receiving/responding to the transaction.
        /// </summary>
        public EgoState ToState { get; init; }

        /// <summary>
        /// Type of transaction pattern.
        /// </summary>
        public TransactionType Type { get; init; }

        /// <summary>
        /// Stimulus content (what was said/thought).
        /// </summary>
        public string Stimulus { get; init; } = string.Empty;

        /// <summary>
        /// Response content.
        /// </summary>
        public string Response { get; init; } = string.Empty;

        /// <summary>
        /// Was this transaction functional (healthy)?
        /// </summary>
        public bool IsFunctional { get; init; } = true;

        /// <summary>
        /// If dysfunctional, what pattern was detected?
        /// (e.g., "Drama Triangle", "Persecutor-Victim")
        /// </summary>
        public string? DysfunctionalPattern { get; init; }

        /// <summary>
        /// Emotional intensity of transaction (0.0 - 1.0).
        /// </summary>
        public double Intensity { get; init; } = 0.5;

        /// <summary>
        /// Context tags for categorization.
        /// </summary>
        public string[] Tags { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Explanation of the transaction analysis.
        /// </summary>
        public string Explanation { get; init; } = string.Empty;

        /// <summary>
        /// Operator state before this transaction.
        /// </summary>
        public Guid? BeforeStateId { get; init; }

        /// <summary>
        /// Operator state after this transaction.
        /// </summary>
        public Guid? AfterStateId { get; init; }

        /// <summary>
        /// Algorithm version used for analysis.
        /// </summary>
        public string AlgorithmVersion { get; init; } = "1.0.0";
    }
}
