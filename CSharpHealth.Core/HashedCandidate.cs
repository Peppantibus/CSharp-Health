namespace CSharpHealth.Core
{
    public sealed record HashedCandidate(
        NormalizedCandidate Normalized,
        string StrongSignatureHex,
        int TokenCount
    );
}
