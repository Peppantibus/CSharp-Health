using CSharpHealth.Core.Normalization;

namespace CSharpHealth.Core.Duplicates
{
    public sealed record HashedCandidate(
        NormalizedCandidate Normalized,
        string StrongSignatureHex,
        int TokenCount
    );
}
