using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hecateon.Modules.Prometheon.Models;
using Hecateon.Modules.Prometheon.EventStore;

namespace Hecateon.Modules.Prometheon.Services
{
    /// <summary>
    /// Foresight simulation service.
    /// Projects potential outcomes of decisions.
    /// Deterministic projections based on historical patterns.
    /// </summary>
    public interface IForesightSimulator
    {
        /// <summary>
        /// Simulate decision outcomes.
        /// </summary>
        Task<ForesightSimulation> SimulateDecisionAsync(
            string decision,
            string context,
            OperatorState currentState);

        /// <summary>
        /// Evaluate accuracy of past simulations.
        /// </summary>
        Task<double> EvaluateSimulationAccuracyAsync(int recentCount = 20);
    }

    public class ForesightSimulator : IForesightSimulator
    {
        private readonly IPrometheonEventStore _eventStore;
        private const string Version = "1.0.0";

        public ForesightSimulator(IPrometheonEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<ForesightSimulation> SimulateDecisionAsync(
            string decision,
            string context,
            OperatorState currentState)
        {
            // Generate decision paths
            var paths = GenerateDecisionPaths(decision, context, currentState);

            // Calculate overall risk
            var overallRisk = CalculateOverallRisk(paths, currentState);

            // Select recommended path
            var recommendedPath = SelectOptimalPath(paths);

            // Generate input features for reproducibility
            var inputFeatures = new Dictionary<string, object>
            {
                ["decision"] = decision,
                ["context"] = context,
                ["operatorState"] = currentState.DominantState.ToString(),
                ["overloadIndex"] = currentState.OverloadIndex,
                ["cognitiveRisk"] = currentState.CognitiveRiskScore,
                ["timestamp"] = DateTime.UtcNow.ToString("O")
            };

            var simulation = new ForesightSimulation
            {
                Decision = decision,
                Context = context,
                Paths = paths,
                RecommendedPathId = recommendedPath?.PathId,
                RecommendationExplanation = GenerateRecommendationExplanation(recommendedPath, paths),
                OverallRisk = overallRisk,
                Confidence = CalculateSimulationConfidence(paths, currentState),
                InputFeatures = inputFeatures,
                CurrentStateId = currentState.Id,
                AlgorithmVersion = Version
            };

            return await _eventStore.AppendSimulationAsync(simulation);
        }

        public async Task<double> EvaluateSimulationAccuracyAsync(int recentCount = 20)
        {
            var simulations = await _eventStore.GetSimulationHistoryAsync(limit: recentCount);
            
            var scored = simulations
                .Where(s => s.AccuracyScore.HasValue)
                .ToList();

            if (!scored.Any())
                return 0.0;

            return scored.Average(s => s.AccuracyScore!.Value);
        }

        private DecisionPath[] GenerateDecisionPaths(string decision, string context, OperatorState state)
        {
            var paths = new List<DecisionPath>();

            // Path 1: Immediate action
            paths.Add(new DecisionPath
            {
                PathId = "immediate",
                Description = "Act immediately on this decision",
                Outcome = PredictOutcome(decision, TimeSpan.FromHours(1), state),
                Probability = CalculatePathProbability(decision, "immediate", state),
                ImpactMagnitude = CalculateImpactMagnitude(decision, "immediate"),
                TimeHorizon = TimeSpan.FromHours(1),
                RiskFactors = IdentifyRiskFactors(decision, state, "immediate"),
                ProtectiveFactors = IdentifyProtectiveFactors(decision, state, "immediate"),
                Consequences = PredictConsequences(decision, "immediate", state)
            });

            // Path 2: Delayed action
            paths.Add(new DecisionPath
            {
                PathId = "delayed",
                Description = "Wait and reflect before acting",
                Outcome = PredictOutcome(decision, TimeSpan.FromDays(1), state),
                Probability = CalculatePathProbability(decision, "delayed", state),
                ImpactMagnitude = CalculateImpactMagnitude(decision, "delayed"),
                TimeHorizon = TimeSpan.FromDays(1),
                RiskFactors = IdentifyRiskFactors(decision, state, "delayed"),
                ProtectiveFactors = IdentifyProtectiveFactors(decision, state, "delayed"),
                Consequences = PredictConsequences(decision, "delayed", state)
            });

            // Path 3: Alternative approach
            paths.Add(new DecisionPath
            {
                PathId = "alternative",
                Description = "Consider an alternative approach",
                Outcome = PredictOutcome(decision, TimeSpan.FromHours(6), state),
                Probability = CalculatePathProbability(decision, "alternative", state),
                ImpactMagnitude = CalculateImpactMagnitude(decision, "alternative"),
                TimeHorizon = TimeSpan.FromHours(6),
                RiskFactors = IdentifyRiskFactors(decision, state, "alternative"),
                ProtectiveFactors = IdentifyProtectiveFactors(decision, state, "alternative"),
                Consequences = PredictConsequences(decision, "alternative", state)
            });

            // Path 4: No action
            paths.Add(new DecisionPath
            {
                PathId = "no_action",
                Description = "Take no action at this time",
                Outcome = PredictOutcome(decision, TimeSpan.FromDays(7), state),
                Probability = CalculatePathProbability(decision, "no_action", state),
                ImpactMagnitude = CalculateImpactMagnitude(decision, "no_action"),
                TimeHorizon = TimeSpan.FromDays(7),
                RiskFactors = IdentifyRiskFactors(decision, state, "no_action"),
                ProtectiveFactors = IdentifyProtectiveFactors(decision, state, "no_action"),
                Consequences = PredictConsequences(decision, "no_action", state)
            });

            return paths.ToArray();
        }

        private SimulationOutcome PredictOutcome(string decision, TimeSpan horizon, OperatorState state)
        {
            // Deterministic prediction based on state and decision characteristics
            var decisionLower = decision.ToLowerInvariant();

            // High overload = likely negative if immediate
            if (state.OverloadIndex > 0.7 && horizon.TotalHours < 2)
                return SimulationOutcome.Negative;

            // Adult state + delayed = likely positive
            if (state.DominantState == EgoState.Adult && horizon.TotalHours > 12)
                return SimulationOutcome.Positive;

            // Critical parent state = likely negative
            if (state.DominantState == EgoState.CriticalParent)
                return SimulationOutcome.Negative;

            // Positive keywords
            if (decisionLower.Contains("help") || decisionLower.Contains("support") || decisionLower.Contains("care"))
                return SimulationOutcome.Positive;

            // Negative keywords
            if (decisionLower.Contains("quit") || decisionLower.Contains("give up") || decisionLower.Contains("avoid"))
                return SimulationOutcome.Negative;

            return SimulationOutcome.Neutral;
        }

        private double CalculatePathProbability(string decision, string pathType, OperatorState state)
        {
            var baseProbability = 0.5;

            // Immediate action more likely if Child state
            if (pathType == "immediate" && 
                (state.DominantState == EgoState.FreeChild || state.DominantState == EgoState.RebelliousChild))
                baseProbability += 0.3;

            // Delayed action more likely if Adult state
            if (pathType == "delayed" && state.DominantState == EgoState.Adult)
                baseProbability += 0.3;

            // No action more likely if overloaded
            if (pathType == "no_action" && state.OverloadIndex > 0.7)
                baseProbability += 0.2;

            return Math.Min(baseProbability, 1.0);
        }

        private double CalculateImpactMagnitude(string decision, string pathType)
        {
            // Immediate actions have higher impact
            if (pathType == "immediate")
                return 0.8;

            // No action has lower impact
            if (pathType == "no_action")
                return 0.3;

            return 0.6;
        }

        private string[] IdentifyRiskFactors(string decision, OperatorState state, string pathType)
        {
            var risks = new List<string>();

            if (state.OverloadIndex > 0.7)
                risks.Add("High cognitive overload");

            if (state.DysregulationIndex > 0.7)
                risks.Add("Emotional dysregulation");

            if (state.DominantState == EgoState.RebelliousChild)
                risks.Add("Rebellious state may lead to impulsive action");

            if (state.DominantState == EgoState.CriticalParent)
                risks.Add("Critical state may lead to harsh judgment");

            if (pathType == "immediate" && state.CognitiveRiskScore > 0.6)
                risks.Add("Immediate action under cognitive stress");

            return risks.ToArray();
        }

        private string[] IdentifyProtectiveFactors(string decision, OperatorState state, string pathType)
        {
            var protective = new List<string>();

            if (state.DominantState == EgoState.Adult)
                protective.Add("Rational Adult state active");

            if (state.OverloadIndex < 0.3)
                protective.Add("Low cognitive load");

            if (pathType == "delayed")
                protective.Add("Time for reflection");

            if (state.DominantState == EgoState.NurturingParent)
                protective.Add("Nurturing state provides care");

            return protective.ToArray();
        }

        private string[] PredictConsequences(string decision, string pathType, OperatorState state)
        {
            var consequences = new List<string>();

            if (pathType == "immediate")
            {
                consequences.Add("Quick resolution");
                if (state.OverloadIndex > 0.7)
                    consequences.Add("Potential regret if rushed");
            }

            if (pathType == "delayed")
            {
                consequences.Add("Time to process");
                consequences.Add("Clearer perspective");
                consequences.Add("May miss time-sensitive opportunity");
            }

            if (pathType == "no_action")
            {
                consequences.Add("Situation may resolve itself");
                consequences.Add("Opportunity may be lost");
                consequences.Add("Stress may continue");
            }

            return consequences.ToArray();
        }

        private double CalculateOverallRisk(DecisionPath[] paths, OperatorState state)
        {
            var negativePathsWeight = paths
                .Where(p => p.Outcome == SimulationOutcome.Negative)
                .Sum(p => p.Probability * p.ImpactMagnitude);

            var stateRisk = state.CognitiveRiskScore * 0.3;

            return Math.Min(negativePathsWeight + stateRisk, 1.0);
        }

        private DecisionPath? SelectOptimalPath(DecisionPath[] paths)
        {
            // Select path with best outcome, highest probability, and manageable risk
            return paths
                .OrderByDescending(p => p.Outcome == SimulationOutcome.Positive ? 1 : 0)
                .ThenByDescending(p => p.Probability)
                .ThenBy(p => p.RiskFactors.Length)
                .FirstOrDefault();
        }

        private string GenerateRecommendationExplanation(DecisionPath? recommended, DecisionPath[] allPaths)
        {
            if (recommended == null)
                return "Unable to determine optimal path.";

            var explanation = $"Recommended: {recommended.Description}. ";
            explanation += $"Predicted outcome: {recommended.Outcome}. ";
            explanation += $"Probability: {recommended.Probability:P0}. ";
            
            if (recommended.ProtectiveFactors.Length > 0)
                explanation += $"Protective factors: {string.Join(", ", recommended.ProtectiveFactors)}. ";

            if (recommended.RiskFactors.Length > 0)
                explanation += $"Risk factors to consider: {string.Join(", ", recommended.RiskFactors)}.";

            return explanation;
        }

        private double CalculateSimulationConfidence(DecisionPath[] paths, OperatorState state)
        {
            // Higher confidence if Adult state (rational)
            var stateConfidence = state.DominantState == EgoState.Adult ? 0.8 : 0.6;

            // Lower confidence if high overload
            if (state.OverloadIndex > 0.7)
                stateConfidence -= 0.2;

            // More paths analyzed = higher confidence
            var pathConfidence = Math.Min(paths.Length * 0.05, 0.2);

            return Math.Min(stateConfidence + pathConfidence, 1.0);
        }
    }
}
