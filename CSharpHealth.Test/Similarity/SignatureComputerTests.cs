using System.Linq;
using CSharpHealth.Core.Candidates;
using CSharpHealth.Core.Normalization;
using CSharpHealth.Core.Similarity;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CSharpHealth.Tests.Similarity
{
    public class SignatureComputerTests
    {
        [Fact]
        public void Compute_ReturnsSameSignatureForIdenticalTokens()
        {
            var first = CreateMethodCandidate("int Sum(int a, int b) { var x = a + b; return x; }");
            var second = CreateMethodCandidate("int Sum(int a, int b) { var y = a + b; return y; }");

            var normalizer = new TokenNormalizer();
            var signatureComputer = new SignatureComputer();

            var firstHash = signatureComputer.Compute(normalizer.Normalize(first));
            var secondHash = signatureComputer.Compute(normalizer.Normalize(second));

            Assert.Equal(firstHash.StrongSignatureHex, secondHash.StrongSignatureHex);
        }

        [Fact]
        public void Compute_ReturnsDifferentSignatureForDifferentTokens()
        {
            var first = CreateMethodCandidate("int Sum(int a, int b) { return a + b; }");
            var second = CreateMethodCandidate("int Sum(int a, int b) { return a - b; }");

            var normalizer = new TokenNormalizer();
            var signatureComputer = new SignatureComputer();

            var firstHash = signatureComputer.Compute(normalizer.Normalize(first));
            var secondHash = signatureComputer.Compute(normalizer.Normalize(second));

            Assert.NotEqual(firstHash.StrongSignatureHex, secondHash.StrongSignatureHex);
        }

        private static CodeCandidate CreateMethodCandidate(string methodDeclaration)
        {
            var code = $"public class Sample {{ {methodDeclaration} }}";
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var root = syntaxTree.GetCompilationUnitRoot();
            var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
            var lineSpan = syntaxTree.GetLineSpan(method.Span);

            return new CodeCandidate(
                CandidateKind.Method,
                "Test.cs",
                lineSpan.StartLinePosition.Line + 1,
                lineSpan.EndLinePosition.Line + 1,
                method.SpanStart,
                method.Span.Length,
                method);
        }
    }
}
