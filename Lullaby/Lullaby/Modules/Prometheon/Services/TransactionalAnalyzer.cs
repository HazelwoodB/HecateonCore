using System;
using System.Linq;
using System.Threading.Tasks;
using Hecateon.Modules.Prometheon.Models;
using Hecateon.Modules.Prometheon.EventStore;

namespace Hecateon.Modules.Prometheon.Services
{
    /// <summary>
    /// Transactional Analysis service.
    /// Analyzes communication patterns and ego state transactions.
    /// Deterministic - same inputs yield same analysis.
    /// </summary>
    public interface ITransactionalAnalyzer
    {
        /// <summary>
        /// Analyze a communication transaction.
        /// </summary>
        Task<Transaction> AnalyzeTransactionAsync(
            string stimulus,
            string response,
            OperatorState currentState);

        /// <summary>
        /// Detect dysfunctional patterns in recent transactions.
        /// </summary>
        Task<string[]> DetectDysfunctionalPatternsAsync(int recentCount = 10);
    }

    public class TransactionalAnalyzer : ITransactionalAnalyzer
    {
        private readonly IPrometheonEventStore _eventStore;
        private const string Version = "1.0.0";

        public TransactionalAnalyzer(IPrometheonEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<Transaction> AnalyzeTransactionAsync(
            string stimulus,
            string response,
            OperatorState currentState)
        {
            // Deterministic analysis based on content patterns
            var fromState = InferEgoState(stimulus, currentState);
            var toState = InferEgoState(response, currentState);
            var type = DetermineTransactionType(fromState, toState);
            var isFunctional = IsTransactionFunctional(fromState, toState, type);
            var pattern = isFunctional ? null : DetectDysfunctionalPattern(fromState, toState, stimulus, response);
            var intensity = CalculateIntensity(stimulus, response);

            var transaction = new Transaction
            {
                FromState = fromState,
                ToState = toState,
                Type = type,
                Stimulus = stimulus,
                Response = response,
                IsFunctional = isFunctional,
                DysfunctionalPattern = pattern,
                Intensity = intensity,
                BeforeStateId = currentState.Id,
                Explanation = GenerateExplanation(fromState, toState, type, isFunctional, pattern),
                AlgorithmVersion = Version
            };

            return await _eventStore.AppendTransactionAsync(transaction);
        }

        public async Task<string[]> DetectDysfunctionalPatternsAsync(int recentCount = 10)
        {
            var transactions = await _eventStore.GetTransactionHistoryAsync(limit: recentCount);
            var patterns = transactions
                .Where(t => !t.IsFunctional && t.DysfunctionalPattern != null)
                .Select(t => t.DysfunctionalPattern!)
                .Distinct()
                .ToArray();

            return patterns;
        }

        private EgoState InferEgoState(string content, OperatorState currentState)
        {
            // Deterministic inference based on language patterns
            var lower = content.ToLowerInvariant();

            // Child indicators
            if (lower.Contains("want") || lower.Contains("need") || lower.Contains("feel"))
            {
                if (lower.Contains("must") || lower.Contains("should"))
                    return EgoState.AdaptiveChild;
                if (lower.Contains("won't") || lower.Contains("refuse"))
                    return EgoState.RebelliousChild;
                return EgoState.FreeChild;
            }

            // Parent indicators
            if (lower.Contains("should") || lower.Contains("must") || lower.Contains("ought"))
            {
                if (lower.Contains("care") || lower.Contains("support") || lower.Contains("help"))
                    return EgoState.NurturingParent;
                return EgoState.CriticalParent;
            }

            // Default to Adult if rational language
            return EgoState.Adult;
        }

        private TransactionType DetermineTransactionType(EgoState from, EgoState to)
        {
            // Complementary: expected response
            if ((from == EgoState.Adult && to == EgoState.Adult) ||
                (from == EgoState.FreeChild && to == EgoState.NurturingParent) ||
                (from == EgoState.NurturingParent && to == EgoState.FreeChild))
            {
                return TransactionType.Complementary;
            }

            // Crossed: unexpected response
            if ((from == EgoState.Adult && IsChildState(to)) ||
                (from == EgoState.Adult && IsParentState(to)) ||
                (IsChildState(from) && to == EgoState.CriticalParent))
            {
                return TransactionType.Crossed;
            }

            // Default to ulterior if complex
            return TransactionType.Ulterior;
        }

        private bool IsTransactionFunctional(EgoState from, EgoState to, TransactionType type)
        {
            // Functional: Adult-Adult or appropriate Parent-Child
            if (type == TransactionType.Complementary && 
                (from == EgoState.Adult || to == EgoState.Adult))
                return true;

            if (from == EgoState.NurturingParent && IsChildState(to))
                return true;

            // Critical Parent or Rebellious Child usually dysfunctional
            if (from == EgoState.CriticalParent || to == EgoState.CriticalParent ||
                from == EgoState.RebelliousChild || to == EgoState.RebelliousChild)
                return false;

            return type == TransactionType.Complementary;
        }

        private string? DetectDysfunctionalPattern(EgoState from, EgoState to, string stimulus, string response)
        {
            // Drama Triangle patterns
            if (from == EgoState.CriticalParent && to == EgoState.AdaptiveChild)
                return "Persecutor-Victim";

            if (from == EgoState.AdaptiveChild && to == EgoState.NurturingParent)
                return "Victim-Rescuer";

            if (from == EgoState.RebelliousChild && to == EgoState.CriticalParent)
                return "Rebel-Persecutor";

            // Crossed transaction
            if (from == EgoState.Adult && to == EgoState.CriticalParent)
                return "Adult-Crossed-Critical";

            return null;
        }

        private double CalculateIntensity(string stimulus, string response)
        {
            // Simple heuristic based on length and punctuation
            var totalLength = stimulus.Length + response.Length;
            var exclamations = stimulus.Count(c => c == '!') + response.Count(c => c == '!');
            var questions = stimulus.Count(c => c == '?') + response.Count(c => c == '?');

            var baseIntensity = Math.Min(totalLength / 500.0, 0.5);
            var punctuationBoost = (exclamations * 0.1) + (questions * 0.05);

            return Math.Min(baseIntensity + punctuationBoost, 1.0);
        }

        private string GenerateExplanation(EgoState from, EgoState to, TransactionType type, bool functional, string? pattern)
        {
            var explanation = $"Transaction from {from} to {to} is {type}. ";
            explanation += functional ? "This is a functional transaction." : "This is dysfunctional.";
            
            if (pattern != null)
                explanation += $" Pattern detected: {pattern}.";

            return explanation;
        }

        private bool IsChildState(EgoState state) =>
            state == EgoState.FreeChild || state == EgoState.AdaptiveChild || state == EgoState.RebelliousChild;

        private bool IsParentState(EgoState state) =>
            state == EgoState.NurturingParent || state == EgoState.CriticalParent;
    }
}
