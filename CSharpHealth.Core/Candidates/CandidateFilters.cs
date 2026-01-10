using System;
using System.Collections.Generic;
using System.Linq;
using CSharpHealth.Core.Normalization;

namespace CSharpHealth.Core.Candidates
{
    public static class CandidateFilters
    {
        public static IReadOnlyList<NormalizedCandidate> FilterNormalizedCandidates(
            IReadOnlyList<NormalizedCandidate> normalizedCandidates,
            int minTokens,
            int minLines,
            IReadOnlyCollection<CandidateKind>? kinds)
        {
            if (normalizedCandidates is null)
            {
                throw new ArgumentNullException(nameof(normalizedCandidates));
            }

            if (minTokens < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minTokens), "Minimum tokens must be zero or greater.");
            }

            if (minLines < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minLines), "Minimum lines must be zero or greater.");
            }

            HashSet<CandidateKind>? kindSet = null;
            if (kinds is { Count: > 0 })
            {
                kindSet = new HashSet<CandidateKind>(kinds);
            }

            return normalizedCandidates
                .Where(candidate =>
                    candidate.TokenCount >= minTokens &&
                    GetLineCount(candidate.Candidate) >= minLines &&
                    (kindSet is null || kindSet.Contains(candidate.Candidate.Kind)))
                .ToList();
        }

        private static int GetLineCount(CodeCandidate candidate)
        {
            return Math.Max(0, candidate.EndLine - candidate.StartLine + 1);
        }
    }
}
