using System.Collections.Generic;
using CSharpHealth.Core.Candidates;

namespace CSharpHealth.Core.Normalization
{
    public sealed record NormalizedCandidate(
        CodeCandidate Candidate,
        IReadOnlyList<string> Tokens,
        int TokenCount
    );
}
