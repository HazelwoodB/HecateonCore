using System;
using System.Collections.Generic;

namespace Hecateon.Modules.Prometheon.Models
{
    /// <summary>
    /// Outcome of a simulated decision path.
    /// </summary>
    public enum SimulationOutcome
    {
        /// <summary>
        /// Positive outcome - beneficial
        /// </summary>
        Positive,

        /// <summary>
        /// Neutral outcome - no significant impact
        /// </summary>
        Neutral,

        /// <summary>
        /// Negative outcome - detrimental
        /// </summary>
        Negative,

        /// <summary>
        /// Uncertain - cannot determine
        /// </summary>
        Uncertain
    }

    /// <summary>
    /// Single decision pathway in a simulation.
    /// </summary>
    public record DecisionPath
    {
        /// <summary>
        /// Path identifier.
        /// </summary>
        public string PathId { get; init; } = string.Empty;

        /// <summary>
        /// Description of this decision path.
        /// </summary>
        public string Description { get; init; } = string.Empty;

        /// <summary>
        /// Predicted outcome.
        /// </summary>
        public SimulationOutcome Outcome { get; init; }

        /// <summary>
        /// Probability of this outcome (0.0 - 1.0).
        /// </summary>
        public double Probability { get; init; } = 0.5;

        /// <summary>
        /// Estimated impact magnitude (0.0 - 1.0).
        /// </summary>
        public double ImpactMagnitude { get; init; } = 0.5;

        /// <summary>
        /// Time horizon (how far into future).
        /// </summary>
        public TimeSpan TimeHorizon { get; init; } = TimeSpan.FromDays(1);

        /// <summary>
        /// Risk factors identified in this path.
        /// </summary>
        public string[] RiskFactors { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Protective factors in this path.
        /// </summary>
        public string[] ProtectiveFactors { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Consequences of this path.
        /// </summary>
        public string[] Consequences { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// Foresight simulation event.
    /// Projects potential outcomes of decisions.
    /// Deterministic - same inputs yield same projections.
    /// Immutable, append-only.
    /// </summary>
    public record ForesightSimulation
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
        /// Decision being simulated.
        /// </summary>
        public string Decision { get; init; } = string.Empty;

        /// <summary>
        /// Current context/situation.
        /// </summary>
        public string Context { get; init; } = string.Empty;

        /// <summary>
        /// Simulated decision paths.
        /// </summary>
        public DecisionPath[] Paths { get; init; } = Array.Empty<DecisionPath>();

        /// <summary>
        /// Recommended path (if any).
        /// </summary>
        public string? RecommendedPathId { get; init; }

        /// <summary>
        /// Explanation for recommendation.
        /// </summary>
        public string RecommendationExplanation { get; init; } = string.Empty;

        /// <summary>
        /// Overall risk assessment for proceeding (0.0 - 1.0).
        /// </summary>
        public double OverallRisk { get; init; } = 0.5;

        /// <summary>
        /// Confidence in simulation (0.0 - 1.0).
        /// </summary>
        public double Confidence { get; init; } = 0.7;

        /// <summary>
        /// Input features used for simulation (for reproducibility).
        /// </summary>
        public Dictionary<string, object> InputFeatures { get; init; } = new();

        /// <summary>
        /// Operator state at time of simulation.
        /// </summary>
        public Guid? CurrentStateId { get; init; }

        /// <summary>
        /// Related transaction that prompted simulation.
        /// </summary>
        public Guid? TriggeringTransactionId { get; init; }

        /// <summary>
        /// User's actual choice (populated after decision made).
        /// </summary>
        public string? ActualChoice { get; init; }

        /// <summary>
        /// Actual outcome (populated after observation).
        /// </summary>
        public SimulationOutcome? ActualOutcome { get; init; }

        /// <summary>
        /// Simulation accuracy (comparing prediction to actual).
        /// </summary>
        public double? AccuracyScore { get; init; }

        /// <summary>
        /// Tags for categorization.
        /// </summary>
        public string[] Tags { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Algorithm version.
        /// </summary>
        public string AlgorithmVersion { get; init; } = "1.0.0";

        /// <summary>
        /// Model version (if ML-based).
        /// </summary>
        public string? ModelVersion { get; init; }
    }
}
