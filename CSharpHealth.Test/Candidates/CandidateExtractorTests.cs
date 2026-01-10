using System;
using System.IO;
using System.Linq;
using CSharpHealth.Core.Candidates;
using CSharpHealth.Core.Parsing;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace CSharpHealth.Tests.Candidates // <-- match your test project namespace if needed
{
    public class CandidateExtractorTests
    {
        [Fact]
        public void ExtractMany_FindsMethodsLambdasAndBlocks()
        {
            var filePath = CreateTemporaryFile("Sample.cs", @"
            using System;

            namespace Sample
            {
                public class Example
                {
                    public void Run()
                    {
                        if (true)
                        {
                            Console.WriteLine(""hi"");
                        }

                        try
                        {
                            Console.WriteLine(""try"");
                        }
                        catch (Exception)
                        {
                            Console.WriteLine(""catch"");
                        }

                        Func<int, int> f1 = x => x + 1;
                        Func<int, int> f2 = (a) =>
                        {
                            return a + 2;
                        };
                    }
                }
            }
            ");

            var parser = new CSharpParser();
            var parseResults = parser.ParseFiles(new[] { filePath }).ToList();
            var extractor = new CandidateExtractor();

            try
            {
                var candidates = extractor.ExtractMany(parseResults);

                Assert.Contains(candidates, c => c.Kind == CandidateKind.Method);
                Assert.Contains(candidates, c => c.Kind == CandidateKind.Lambda);

                var blockCandidates = candidates.Where(c => c.Kind == CandidateKind.Block).ToList();

                Assert.Contains(blockCandidates, c => c.Node is BlockSyntax block
                    && block.Parent is IfStatementSyntax);

                Assert.Contains(blockCandidates, c => c.Node is BlockSyntax block
                    && block.Parent is TryStatementSyntax);

                Assert.Contains(blockCandidates, c => c.Node is BlockSyntax block
                    && block.Parent is CatchClauseSyntax);

                Assert.All(candidates, c =>
                {
                    Assert.True(c.StartLine > 0);
                    Assert.True(c.EndLine >= c.StartLine);
                });
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void ExtractMany_DoesNotIncludeMethodBodyBlocks()
        {
            var filePath = CreateTemporaryFile("BodyOnly.cs", @"
            namespace Sample
            {
                public class Example
                {
                    public void Run()
                    {
                        var value = 1;
                    }
                }
            }
            ");

            var parser = new CSharpParser();
            var parseResults = parser.ParseFiles(new[] { filePath }).ToList();
            var extractor = new CandidateExtractor();

            try
            {
                var candidates = extractor.ExtractMany(parseResults);
                var blockCandidates = candidates.Where(c => c.Kind == CandidateKind.Block).ToList();

                Assert.Empty(blockCandidates);
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        private static string CreateTemporaryFile(string fileName, string contents)
        {
            var root = Path.Combine(Path.GetTempPath(), "CSharpHealthCandidateTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var filePath = Path.Combine(root, fileName);
            File.WriteAllText(filePath, contents);

            return filePath;
        }
    }
}
