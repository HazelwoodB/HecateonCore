using System;

namespace Hecateon.Modules.Prometheon.Models
{
    /// <summary>
    /// Represents ego state based on Transactional Analysis theory.
    /// Three primary states: Child, Adult, Parent.
    /// </summary>
    public enum EgoState
    {
        /// <summary>
        /// Free Child - spontaneous, creative, feeling-based
        /// </summary>
        FreeChild,

        /// <summary>
        /// Adaptive Child - compliant, submissive
        /// </summary>
        AdaptiveChild,

        /// <summary>
        /// Rebellious Child - resistant, defiant
        /// </summary>
        RebelliousChild,

        /// <summary>
        /// Adult - rational, logical, present-focused
        /// </summary>
        Adult,

        /// <summary>
        /// Nurturing Parent - caring, supportive
        /// </summary>
        NurturingParent,

        /// <summary>
        /// Critical Parent - judgmental, controlling
        /// </summary>
        CriticalParent
    }

    /// <summary>
    /// Current operator state snapshot.
    /// Tracks which ego state is dominant and energy levels.
    /// Immutable - changes result in new state objects (append-only).
    /// </summary>
    public record OperatorState
    {
        /// <summary>
        /// Unique identifier for this state snapshot.
        /// </summary>
        public Guid Id { get; init; } = Guid.NewGuid();

        /// <summary>
        /// UTC timestamp of this state.
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Currently dominant ego state.
        /// </summary>
        public EgoState DominantState { get; init; } = EgoState.Adult;

        /// <summary>
        /// Energy level of Child state (0.0 - 1.0).
        /// </summary>
        public double ChildEnergy { get; init; } = 0.33;

        /// <summary>
        /// Energy level of Adult state (0.0 - 1.0).
        /// </summary>
        public double AdultEnergy { get; init; } = 0.33;

        /// <summary>
        /// Energy level of Parent state (0.0 - 1.0).
        /// </summary>
        public double ParentEnergy { get; init; } = 0.33;

        /// <summary>
        /// Overall cognitive load index (0.0 - 1.0).
        /// Higher = more overloaded.
        /// </summary>
        public double OverloadIndex { get; init; } = 0.0;

        /// <summary>
        /// Dysregulation index (0.0 - 1.0).
        /// Measures how far from balanced Adult state.
        /// </summary>
        public double DysregulationIndex { get; init; } = 0.0;

        /// <summary>
        /// Risk score for cognitive destabilization (0.0 - 1.0).
        /// Used in unified safety ladder with NYPHOS.
        /// </summary>
        public double CognitiveRiskScore { get; init; } = 0.0;

        /// <summary>
        /// Confidence in this state assessment (0.0 - 1.0).
        /// </summary>
        public double Confidence { get; init; } = 1.0;

        /// <summary>
        /// Explanation of why this state was assessed.
        /// </summary>
        public string Explanation { get; init; } = string.Empty;

        /// <summary>
        /// Previous state ID (for history tracking).
        /// </summary>
        public Guid? PreviousStateId { get; init; }

        /// <summary>
        /// Version of the assessment algorithm.
        /// </summary>
        public string AlgorithmVersion { get; init; } = "1.0.0";
    }
}
