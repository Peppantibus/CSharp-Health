using System.Collections.Generic;

namespace CSharpHealth.Core
{
    public sealed record NormalizedCandidate(
        CodeCandidate Candidate,
        IReadOnlyList<string> Tokens,
        int TokenCount
    );
}
