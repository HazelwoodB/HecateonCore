using System;
using System.Linq;
using System.Threading.Tasks;
using Hecateon.Modules.Prometheon.Models;
using Hecateon.Modules.Prometheon.EventStore;

namespace Hecateon.Modules.Prometheon.Services
{
    /// <summary>
    /// Main Prometheon cognitive operator engine.
    /// Orchestrates all Prometheon services and maintains operator state.
    /// Deterministic, append-only, explainable.
    /// </summary>
    public interface IPrometheronEngine
    {
        /// <summary>
        /// Get current operator state.
        /// </summary>
        Task<OperatorState> GetCurrentStateAsync();

        /// <summary>
        /// Assess and update operator state based on new data.
        /// </summary>
        Task<OperatorState> AssessStateAsync(
            double? childEnergy = null,
            double? adultEnergy = null,
            double? parentEnergy = null);

        /// <summary>
        /// Process a communication transaction.
        /// </summary>
        Task<Transaction> ProcessTransactionAsync(string stimulus, string response);

        /// <summary>
        /// Request a narrative reframe.
        /// </summary>
        Task<NarrativeFrame> RequestReframeAsync(
            string narrative,
            ReframeType? preferredType = null);

        /// <summary>
        /// Simulate a decision.
        /// </summary>
        Task<ForesightSimulation> SimulateDecisionAsync(string decision, string context);

        /// <summary>
        /// Get unified cognitive risk score (for safety ladder integration).
        /// </summary>
        Task<double> GetCognitiveRiskScoreAsync();

        /// <summary>
        /// Get system health metrics.
        /// </summary>
        Task<PrometheronHealthMetrics> GetHealthMetricsAsync();
    }

    public record PrometheronHealthMetrics
    {
        public int TotalStates { get; init; }
        public int TotalTransactions { get; init; }
        public int TotalReframes { get; init; }
        public int TotalSimulations { get; init; }
        public double ReframeEffectiveness { get; init; }
        public double SimulationAccuracy { get; init; }
        public double AverageConfidence { get; init; }
        public string EngineVersion { get; init; } = "1.0.0";
        public DateTime LastActivity { get; init; } = DateTime.UtcNow;
    }

    public class PrometheronEngine : IPrometheronEngine
    {
        private readonly IPrometheonEventStore _eventStore;
        private readonly ITransactionalAnalyzer _transactionalAnalyzer;
        private readonly INarrativeReframer _narrativeReframer;
        private readonly IForesightSimulator _foresightSimulator;
        private const string Version = "1.0.0";

        public PrometheronEngine(
            IPrometheonEventStore eventStore,
            ITransactionalAnalyzer transactionalAnalyzer,
            INarrativeReframer narrativeReframer,
            IForesightSimulator foresightSimulator)
        {
            _eventStore = eventStore;
            _transactionalAnalyzer = transactionalAnalyzer;
            _narrativeReframer = narrativeReframer;
            _foresightSimulator = foresightSimulator;
        }

        public async Task<OperatorState> GetCurrentStateAsync()
        {
            var state = await _eventStore.GetLatestStateAsync();
            
            // Return default Adult state if no history
            if (state == null)
            {
                state = new OperatorState
                {
                    DominantState = EgoState.Adult,
                    ChildEnergy = 0.33,
                    AdultEnergy = 0.34,
                    ParentEnergy = 0.33,
                    OverloadIndex = 0.0,
                    DysregulationIndex = 0.0,
                    CognitiveRiskScore = 0.0,
                    Confidence = 1.0,
                    Explanation = "Initial balanced state - no history available.",
                    AlgorithmVersion = Version
                };

                await _eventStore.AppendStateAsync(state);
            }

            return state;
        }

        public async Task<OperatorState> AssessStateAsync(
            double? childEnergy = null,
            double? adultEnergy = null,
            double? parentEnergy = null)
        {
            var currentState = await GetCurrentStateAsync();

            // Update energies if provided
            var newChildEnergy = childEnergy ?? currentState.ChildEnergy;
            var newAdultEnergy = adultEnergy ?? currentState.AdultEnergy;
            var newParentEnergy = parentEnergy ?? currentState.ParentEnergy;

            // Normalize energies to sum to 1.0
            var total = newChildEnergy + newAdultEnergy + newParentEnergy;
            if (total > 0)
            {
                newChildEnergy /= total;
                newAdultEnergy /= total;
                newParentEnergy /= total;
            }

            // Determine dominant state
            var dominantState = DetermineDominantState(newChildEnergy, newAdultEnergy, newParentEnergy);

            // Calculate overload index (based on recent transaction intensity)
            var overloadIndex = await CalculateOverloadIndexAsync();

            // Calculate dysregulation (distance from balanced Adult state)
            var dysregulationIndex = CalculateDysregulation(newChildEnergy, newAdultEnergy, newParentEnergy);

            // Calculate cognitive risk score
            var cognitiveRiskScore = CalculateCognitiveRisk(overloadIndex, dysregulationIndex, dominantState);

            // Calculate confidence
            var confidence = CalculateAssessmentConfidence(currentState);

            var newState = new OperatorState
            {
                DominantState = dominantState,
                ChildEnergy = newChildEnergy,
                AdultEnergy = newAdultEnergy,
                ParentEnergy = newParentEnergy,
                OverloadIndex = overloadIndex,
                DysregulationIndex = dysregulationIndex,
                CognitiveRiskScore = cognitiveRiskScore,
                Confidence = confidence,
                Explanation = GenerateStateExplanation(dominantState, overloadIndex, dysregulationIndex, cognitiveRiskScore),
                PreviousStateId = currentState.Id,
                AlgorithmVersion = Version
            };

            return await _eventStore.AppendStateAsync(newState);
        }

        public async Task<Transaction> ProcessTransactionAsync(string stimulus, string response)
        {
            var currentState = await GetCurrentStateAsync();
            var transaction = await _transactionalAnalyzer.AnalyzeTransactionAsync(stimulus, response, currentState);

            // Update state based on transaction impact
            var stateUpdate = CalculateStateImpact(transaction, currentState);
            await AssessStateAsync(stateUpdate.ChildEnergy, stateUpdate.AdultEnergy, stateUpdate.ParentEnergy);

            return transaction;
        }

        public async Task<NarrativeFrame> RequestReframeAsync(
            string narrative,
            ReframeType? preferredType = null)
        {
            var currentState = await GetCurrentStateAsync();
            return await _narrativeReframer.ReframeNarrativeAsync(narrative, currentState, preferredType);
        }

        public async Task<ForesightSimulation> SimulateDecisionAsync(string decision, string context)
        {
            var currentState = await GetCurrentStateAsync();
            return await _foresightSimulator.SimulateDecisionAsync(decision, context, currentState);
        }

        public async Task<double> GetCognitiveRiskScoreAsync()
        {
            var state = await GetCurrentStateAsync();
            return state.CognitiveRiskScore;
        }

        public async Task<PrometheronHealthMetrics> GetHealthMetricsAsync()
        {
            var states = await _eventStore.GetStateHistoryAsync();
            var transactions = await _eventStore.GetTransactionHistoryAsync();
            var reframes = await _eventStore.GetReframeHistoryAsync();
            var simulations = await _eventStore.GetSimulationHistoryAsync();

            var reframeEffectiveness = await _narrativeReframer.EvaluateReframeEffectivenessAsync();
            var simulationAccuracy = await _foresightSimulator.EvaluateSimulationAccuracyAsync();

            var avgConfidence = states.Any() ? states.Average(s => s.Confidence) : 1.0;

            var lastActivity = new[] {
                states.Any() ? states.Max(s => s.Timestamp) : DateTime.MinValue,
                transactions.Any() ? transactions.Max(t => t.Timestamp) : DateTime.MinValue,
                reframes.Any() ? reframes.Max(r => r.Timestamp) : DateTime.MinValue,
                simulations.Any() ? simulations.Max(s => s.Timestamp) : DateTime.MinValue
            }.Max();

            return new PrometheronHealthMetrics
            {
                TotalStates = states.Count(),
                TotalTransactions = transactions.Count(),
                TotalReframes = reframes.Count(),
                TotalSimulations = simulations.Count(),
                ReframeEffectiveness = reframeEffectiveness,
                SimulationAccuracy = simulationAccuracy,
                AverageConfidence = avgConfidence,
                EngineVersion = Version,
                LastActivity = lastActivity == DateTime.MinValue ? DateTime.UtcNow : lastActivity
            };
        }

        private EgoState DetermineDominantState(double childEnergy, double adultEnergy, double parentEnergy)
        {
            if (adultEnergy >= childEnergy && adultEnergy >= parentEnergy)
                return EgoState.Adult;

            if (childEnergy > parentEnergy)
            {
                // Default to Free Child if Child is dominant
                return EgoState.FreeChild;
            }

            // Default to Nurturing Parent if Parent is dominant
            return EgoState.NurturingParent;
        }

        private async Task<double> CalculateOverloadIndexAsync()
        {
            var recentTransactions = await _eventStore.GetTransactionHistoryAsync(limit: 10);
            
            if (!recentTransactions.Any())
                return 0.0;

            var avgIntensity = recentTransactions.Average(t => t.Intensity);
            var dysfunctionalRatio = recentTransactions.Count(t => !t.IsFunctional) / (double)recentTransactions.Count();

            return Math.Min((avgIntensity + dysfunctionalRatio) / 2.0, 1.0);
        }

        private double CalculateDysregulation(double childEnergy, double adultEnergy, double parentEnergy)
        {
            // Ideal balanced state: Adult = 0.5, Child = 0.25, Parent = 0.25
            var idealAdult = 0.5;
            var idealChild = 0.25;
            var idealParent = 0.25;

            var adultDelta = Math.Abs(adultEnergy - idealAdult);
            var childDelta = Math.Abs(childEnergy - idealChild);
            var parentDelta = Math.Abs(parentEnergy - idealParent);

            return (adultDelta + childDelta + parentDelta) / 3.0;
        }

        private double CalculateCognitiveRisk(double overloadIndex, double dysregulationIndex, EgoState dominantState)
        {
            var baseRisk = (overloadIndex + dysregulationIndex) / 2.0;

            // Critical Parent or Rebellious Child = higher risk
            if (dominantState == EgoState.CriticalParent || dominantState == EgoState.RebelliousChild)
                baseRisk += 0.2;

            // Adaptive Child = moderate risk
            if (dominantState == EgoState.AdaptiveChild)
                baseRisk += 0.1;

            // Adult = protective
            if (dominantState == EgoState.Adult)
                baseRisk -= 0.1;

            return Math.Max(0.0, Math.Min(baseRisk, 1.0));
        }

        private double CalculateAssessmentConfidence(OperatorState previousState)
        {
            // Higher confidence with more historical data
            return 0.85; // Simplified for now
        }

        private string GenerateStateExplanation(EgoState dominant, double overload, double dysregulation, double risk)
        {
            var explanation = $"Dominant state: {dominant}. ";
            
            if (overload > 0.7)
                explanation += "High cognitive overload detected. ";
            else if (overload > 0.4)
                explanation += "Moderate cognitive load. ";
            else
                explanation += "Low cognitive load. ";

            if (dysregulation > 0.7)
                explanation += "Significant dysregulation from balanced state. ";
            else if (dysregulation > 0.4)
                explanation += "Moderate dysregulation. ";
            else
                explanation += "Well-balanced state. ";

            if (risk > 0.7)
                explanation += "High cognitive risk - intervention recommended.";
            else if (risk > 0.4)
                explanation += "Moderate cognitive risk - monitoring advised.";
            else
                explanation += "Low cognitive risk - state is stable.";

            return explanation;
        }

        private (double ChildEnergy, double AdultEnergy, double ParentEnergy) CalculateStateImpact(
            Transaction transaction,
            OperatorState currentState)
        {
            var childEnergy = currentState.ChildEnergy;
            var adultEnergy = currentState.AdultEnergy;
            var parentEnergy = currentState.ParentEnergy;

            // Adjust energies based on transaction
            var intensity = transaction.Intensity * 0.1;

            if (transaction.FromState == EgoState.FreeChild || transaction.FromState == EgoState.AdaptiveChild || transaction.FromState == EgoState.RebelliousChild)
                childEnergy += intensity;
            else if (transaction.FromState == EgoState.Adult)
                adultEnergy += intensity;
            else
                parentEnergy += intensity;

            // Normalize
            var total = childEnergy + adultEnergy + parentEnergy;
            if (total > 0)
            {
                childEnergy /= total;
                adultEnergy /= total;
                parentEnergy /= total;
            }

            return (childEnergy, adultEnergy, parentEnergy);
        }
    }
}
