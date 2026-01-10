using System;
using System.IO;
using System.Linq;
using CSharpHealth.Core.Candidates;
using CSharpHealth.Core.Duplicates;
using CSharpHealth.Core.Normalization;
using CSharpHealth.Core.Parsing;
using CSharpHealth.Core.Scanning;
using CSharpHealth.Core.Similarity;
using Xunit;

namespace CSharpHealth.Tests.Duplicates
{
    public class DuplicateGroupingTests
    {
        [Fact]
        public void GroupStrongDuplicates_FindsMatchingMethodsAcrossFiles()
        {
            var tempRoot = Directory.CreateTempSubdirectory("csharphealth-dup-test-");
            try
            {
                var firstPath = Path.Combine(tempRoot.FullName, "First.cs");
                var secondPath = Path.Combine(tempRoot.FullName, "Second.cs");

                File.WriteAllText(firstPath, @"namespace Sample
{
    public class First
    {
        public int Sum(int a, int b)
        {
            var total = a + b;
            return total;
        }
    }
}
");

                File.WriteAllText(secondPath, @"namespace Sample
{
    public class Second
    {
        public int Sum(int x, int y)
        {
            var result = x + y;
            return result;
        }
    }
}
");

                var scanner = new FileScanner();
                var files = scanner.FindCSharpFiles(tempRoot.FullName);
                var parser = new CSharpParser();
                var results = parser.ParseFiles(files).ToList();
                var extractor = new CandidateExtractor();
                var candidates = extractor.ExtractMany(results);
                var normalizer = new TokenNormalizer();
                var normalizedCandidates = normalizer.NormalizeMany(candidates);
                var signatureComputer = new SignatureComputer();
                var hashedCandidates = signatureComputer.ComputeMany(normalizedCandidates);
                var grouper = new DuplicateGrouper();

                var groups = grouper.GroupStrongDuplicates(hashedCandidates);

                Assert.NotEmpty(groups);

                var methodGroup = groups.FirstOrDefault(group =>
                    group.Occurrences.Count >= 2 &&
                    group.Occurrences.All(occurrence => occurrence.CandidateKind == CandidateKind.Method) &&
                    group.Occurrences.Select(occurrence => occurrence.FilePath).Distinct(StringComparer.Ordinal).Count() == 2);

                Assert.NotNull(methodGroup);
                Assert.True(methodGroup!.GroupSize >= 2);

                var occurrences = methodGroup.Occurrences.ToList();
                Assert.Contains(occurrences, occurrence => occurrence.FilePath == firstPath);
                Assert.Contains(occurrences, occurrence => occurrence.FilePath == secondPath);

                foreach (var occurrence in occurrences)
                {
                    Assert.True(occurrence.StartLine >= 5);
                    Assert.True(occurrence.EndLine >= occurrence.StartLine);
                }
            }
            finally
            {
                tempRoot.Delete(true);
            }
        }
    }
}
