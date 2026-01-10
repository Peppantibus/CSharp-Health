using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpHealth.Core
{
    public sealed class DuplicateGrouper
    {
        public IReadOnlyList<DuplicateGroup> GroupStrongDuplicates(
            IReadOnlyList<HashedCandidate> hashedCandidates,
            int minGroupSize = 2)
        {
            if (hashedCandidates is null)
            {
                throw new ArgumentNullException(nameof(hashedCandidates));
            }

            if (minGroupSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minGroupSize), "Minimum group size must be greater than zero.");
            }

            var groups = hashedCandidates
                .GroupBy(candidate => candidate.StrongSignatureHex, StringComparer.Ordinal)
                .Where(group => group.Count() >= minGroupSize)
                .Select(CreateGroup)
                .OrderByDescending(group => group.GroupSize)
                .ThenByDescending(group => group.TokenCount)
                .ToList();

            return groups;
        }

        private static DuplicateGroup CreateGroup(IGrouping<string, HashedCandidate> group)
        {
            var occurrences = group
                .Select(candidate =>
                {
                    var code = candidate.Normalized.Candidate;
                    return new DuplicateOccurrence(
                        code.Kind,
                        code.FilePath,
                        code.StartLine,
                        code.EndLine,
                        code.SpanStart,
                        code.SpanLength);
                })
                .OrderBy(occurrence => occurrence.FilePath, StringComparer.Ordinal)
                .ThenBy(occurrence => occurrence.StartLine)
                .ThenBy(occurrence => occurrence.SpanStart)
                .ToList();

            var groupSize = occurrences.Count;
            var tokenCount = group.Max(candidate => candidate.TokenCount);

            return new DuplicateGroup(
                group.Key,
                100.0,
                occurrences,
                groupSize,
                tokenCount);
        }
    }
}
