using System;
using System.Linq;
using System.Threading.Tasks;
using Hecateon.Modules.Prometheon.Models;
using Hecateon.Modules.Prometheon.EventStore;

namespace Hecateon.Modules.Prometheon.Services
{
    /// <summary>
    /// Narrative reframing service.
    /// Provides cognitive restructuring interventions.
    /// Deterministic outputs for given inputs.
    /// </summary>
    public interface INarrativeReframer
    {
        /// <summary>
        /// Generate a cognitive reframe for a narrative.
        /// </summary>
        Task<NarrativeFrame> ReframeNarrativeAsync(
            string originalNarrative,
            OperatorState currentState,
            ReframeType? preferredType = null);

        /// <summary>
        /// Evaluate effectiveness of recent reframes.
        /// </summary>
        Task<double> EvaluateReframeEffectivenessAsync(int recentCount = 10);
    }

    public class NarrativeReframer : INarrativeReframer
    {
        private readonly IPrometheonEventStore _eventStore;
        private const string Version = "1.0.0";

        public NarrativeReframer(IPrometheonEventStore eventStore)
        {
            _eventStore = eventStore;
        }

        public async Task<NarrativeFrame> ReframeNarrativeAsync(
            string originalNarrative,
            OperatorState currentState,
            ReframeType? preferredType = null)
        {
            // Identify cognitive distortions
            var distortions = IdentifyDistortions(originalNarrative);

            // Select reframe type
            var reframeType = preferredType ?? SelectOptimalReframeType(originalNarrative, distortions, currentState);

            // Generate reframed narrative
            var reframedNarrative = GenerateReframe(originalNarrative, reframeType, distortions);

            // Calculate confidence
            var confidence = CalculateReframeConfidence(originalNarrative, reframedNarrative, distortions);

            var reframe = new NarrativeFrame
            {
                OriginalNarrative = originalNarrative,
                ReframedNarrative = reframedNarrative,
                Type = reframeType,
                IdentifiedDistortions = distortions,
                Confidence = confidence,
                BeforeStateId = currentState.Id,
                Explanation = GenerateExplanation(reframeType, distortions),
                AlgorithmVersion = Version
            };

            return await _eventStore.AppendReframeAsync(reframe);
        }

        public async Task<double> EvaluateReframeEffectivenessAsync(int recentCount = 10)
        {
            var reframes = await _eventStore.GetReframeHistoryAsync(limit: recentCount);
            var effectiveReframes = reframes
                .Where(r => r.EffectivenessScore.HasValue && r.EffectivenessScore.Value > 0.6)
                .Count();

            var total = reframes.Count();
            return total > 0 ? (double)effectiveReframes / total : 0.0;
        }

        private string[] IdentifyDistortions(string narrative)
        {
            var distortions = new System.Collections.Generic.List<string>();
            var lower = narrative.ToLowerInvariant();

            // All-or-nothing thinking
            if (lower.Contains("always") || lower.Contains("never") || lower.Contains("everyone") || lower.Contains("no one"))
                distortions.Add("All-or-nothing thinking");

            // Overgeneralization
            if (lower.Contains("every time") || lower.Contains("typical") || lower.Contains("pattern"))
                distortions.Add("Overgeneralization");

            // Catastrophizing
            if (lower.Contains("disaster") || lower.Contains("terrible") || lower.Contains("awful") || lower.Contains("worst"))
                distortions.Add("Catastrophizing");

            // Mind reading
            if (lower.Contains("they think") || lower.Contains("probably thinks") || lower.Contains("must think"))
                distortions.Add("Mind reading");

            // Should statements
            if (lower.Contains("should") || lower.Contains("must") || lower.Contains("ought"))
                distortions.Add("Should statements");

            // Emotional reasoning
            if (lower.Contains("i feel") && (lower.Contains("therefore") || lower.Contains("so")))
                distortions.Add("Emotional reasoning");

            // Labeling
            if (lower.Contains("i am a") || lower.Contains("i'm a") || lower.Contains("they are a"))
                distortions.Add("Labeling");

            return distortions.ToArray();
        }

        private ReframeType SelectOptimalReframeType(string narrative, string[] distortions, OperatorState state)
        {
            // Select based on distortions
            if (distortions.Contains("Catastrophizing"))
                return ReframeType.Decatastrophizing;

            if (distortions.Contains("All-or-nothing thinking"))
                return ReframeType.PerspectiveShift;

            if (distortions.Contains("Should statements"))
                return ReframeType.CognitiveRestructuring;

            // Based on operator state
            if (state.DominantState == EgoState.CriticalParent)
                return ReframeType.CognitiveRestructuring;

            if (state.OverloadIndex > 0.7)
                return ReframeType.ContextReframe;

            return ReframeType.MeaningReframe;
        }

        private string GenerateReframe(string original, ReframeType type, string[] distortions)
        {
            switch (type)
            {
                case ReframeType.Decatastrophizing:
                    return Decatastrophize(original);

                case ReframeType.PerspectiveShift:
                    return ShiftPerspective(original);

                case ReframeType.CognitiveRestructuring:
                    return CognitivelyRestructure(original);

                case ReframeType.ContextReframe:
                    return ReframeContext(original);

                case ReframeType.TemporalReframe:
                    return TemporallyReframe(original);

                case ReframeType.MeaningReframe:
                    return ReframeMeaning(original);

                default:
                    return original;
            }
        }

        private string Decatastrophize(string narrative)
        {
            var reframed = narrative
                .Replace("disaster", "challenge")
                .Replace("terrible", "difficult")
                .Replace("awful", "uncomfortable")
                .Replace("worst", "less than ideal");

            if (!reframed.Contains("?"))
                reframed += " What evidence do I have that this is catastrophic? What's a more realistic assessment?";

            return reframed;
        }

        private string ShiftPerspective(string narrative)
        {
            var lower = narrative.ToLowerInvariant();
            
            if (lower.Contains("always") || lower.Contains("never"))
            {
                return narrative + " Are there exceptions? When has it been different? What would a friend say about this situation?";
            }

            return "Looking at this from another angle: " + narrative + " What's another way to view this?";
        }

        private string CognitivelyRestructure(string narrative)
        {
            var reframed = narrative
                .Replace("should", "could")
                .Replace("must", "might")
                .Replace("ought to", "may choose to");

            reframed += " What's the evidence for and against this thought? Is this thought helpful?";
            return reframed;
        }

        private string ReframeContext(string narrative)
        {
            return $"In the broader context: {narrative} How does this fit into the bigger picture? What's the full situation?";
        }

        private string TemporallyReframe(string narrative)
        {
            return $"Past: What led to this? Present: {narrative} Future: How might this look in a year? What's temporary here?";
        }

        private string ReframeMeaning(string narrative)
        {
            return $"{narrative} What could this mean instead? What's the opportunity here? How could this be reinterpreted constructively?";
        }

        private double CalculateReframeConfidence(string original, string reframed, string[] distortions)
        {
            // More distortions identified = higher confidence in reframe
            var distortionFactor = Math.Min(distortions.Length * 0.15, 0.4);

            // Significant change = higher confidence
            var changeFactor = original != reframed ? 0.3 : 0.0;

            // Base confidence
            var baseConfidence = 0.5;

            return Math.Min(baseConfidence + distortionFactor + changeFactor, 1.0);
        }

        private string GenerateExplanation(ReframeType type, string[] distortions)
        {
            var explanation = $"Applied {type} reframe. ";
            
            if (distortions.Length > 0)
                explanation += $"Identified distortions: {string.Join(", ", distortions)}. ";

            explanation += "This reframe aims to provide a more balanced perspective.";

            return explanation;
        }
    }
}
