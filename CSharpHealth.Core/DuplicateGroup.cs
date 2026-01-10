using System.Collections.Generic;

namespace CSharpHealth.Core
{
    public sealed record DuplicateGroup(
        string Signature,
        double SimilarityPercent,
        IReadOnlyList<DuplicateOccurrence> Occurrences,
        int GroupSize,
        int TokenCount
    );
}
