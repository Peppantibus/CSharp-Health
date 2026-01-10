using System.Linq;
using CSharpHealth.Core.Candidates;
using CSharpHealth.Core.Normalization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CSharpHealth.Tests.Normalization
{
    public class TokenNormalizerTests
    {
        [Fact]
        public void Normalize_ReplacesIdentifiersByFirstSeenOrder()
        {
            var first = CreateMethodCandidate("int Sum(int a, int b) { var x = a + b; return x; }");
            var second = CreateMethodCandidate("int Sum(int a, int b) { var y = a + b; return y; }");

            var normalizer = new TokenNormalizer();

            var firstTokens = normalizer.Normalize(first).Tokens;
            var secondTokens = normalizer.Normalize(second).Tokens;

            Assert.Equal(firstTokens, secondTokens);
        }

        [Fact]
        public void Normalize_ReplacesLiteralValues()
        {
            var first = CreateMethodCandidate("void Run(int x) { if (x == 10) Console.WriteLine(\"hi\"); }");
            var second = CreateMethodCandidate("void Run(int x) { if (x == 99) Console.WriteLine(\"bye\"); }");

            var normalizer = new TokenNormalizer();

            var firstTokens = normalizer.Normalize(first).Tokens;
            var secondTokens = normalizer.Normalize(second).Tokens;

            Assert.Equal(firstTokens, secondTokens);
        }

        [Fact]
        public void Normalize_IgnoresCommentsAndWhitespace()
        {
            var withComments = CreateMethodCandidate(@"int Sum(int a, int b)
            {
                // comment
                var x = a + b; /* multi-line */
                return x;
            }");

            var withoutComments = CreateMethodCandidate(@"int Sum(int a, int b)
            {
                var x = a + b;
                return x;
            }");

            var normalizer = new TokenNormalizer();

            var withCommentTokens = normalizer.Normalize(withComments).Tokens;
            var withoutCommentTokens = normalizer.Normalize(withoutComments).Tokens;

            Assert.Equal(withCommentTokens, withoutCommentTokens);
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
