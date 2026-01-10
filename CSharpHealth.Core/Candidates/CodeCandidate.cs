using Microsoft.CodeAnalysis;

namespace CSharpHealth.Core.Candidates
{
    public sealed record CodeCandidate(
        CandidateKind Kind,
        string FilePath,
        int StartLine,
        int EndLine,
        int SpanStart,
        int SpanLength,
        SyntaxNode Node
    );
}
