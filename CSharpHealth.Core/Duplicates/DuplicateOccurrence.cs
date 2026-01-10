using CSharpHealth.Core.Candidates;

namespace CSharpHealth.Core.Duplicates
{
    public sealed record DuplicateOccurrence(
        CandidateKind CandidateKind,
        string FilePath,
        int StartLine,
        int EndLine,
        int SpanStart,
        int SpanLength
    );
}
