using System;
using System.IO;
using System.Linq;
using CSharpHealth.Core;
using Xunit;

namespace CSharpHealth.Tests
{
    public class DuplicateFilteringAndRankingTests
    {
        [Fact]
        public void FilterNormalizedCandidates_AppliesTokenAndLineThresholds()
        {
            var tempRoot = Directory.CreateTempSubdirectory("csharphealth-filter-test-");
            try
            {
                var shortPath = Path.Combine(tempRoot.FullName, "Short.cs");
                var longPath = Path.Combine(tempRoot.FullName, "Long.cs");

                File.WriteAllText(shortPath, @"namespace Sample
{
    public class ShortSample
    {
        public int Add(int a, int b)
        {
            return a + b;
        }
    }
}
");

                File.WriteAllText(longPath, @"namespace Sample
{
    public class LongSample
    {
        public int Compute(int a, int b)
        {
            var total = a + b;
            var delta = total - a;
            var product = delta * b;
            if (product > 10)
            {
                product += 1;
            }

            return product;
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

                var shortCandidate = normalizedCandidates.Single(candidate =>
                    candidate.Candidate.FilePath == shortPath &&
                    candidate.Candidate.Kind == CandidateKind.Method);
                var longCandidate = normalizedCandidates.Single(candidate =>
                    candidate.Candidate.FilePath == longPath &&
                    candidate.Candidate.Kind == CandidateKind.Method);

                Assert.True(longCandidate.TokenCount > shortCandidate.TokenCount);
                var shortLines = shortCandidate.Candidate.EndLine - shortCandidate.Candidate.StartLine + 1;
                var longLines = longCandidate.Candidate.EndLine - longCandidate.Candidate.StartLine + 1;
                Assert.True(longLines > shortLines);

                var filtered = CandidateFilters.FilterNormalizedCandidates(
                    normalizedCandidates,
                    shortCandidate.TokenCount + 1,
                    shortLines + 1,
                    new[] { CandidateKind.Method });

                Assert.Single(filtered);
                Assert.Equal(longPath, filtered[0].Candidate.FilePath);
            }
            finally
            {
                tempRoot.Delete(true);
            }
        }

        [Fact]
        public void GroupStrongDuplicates_SortsByImpactDescending()
        {
            var tempRoot = Directory.CreateTempSubdirectory("csharphealth-impact-test-");
            try
            {
                var longTemplate = @"namespace Sample
{
    public class LongExample
    {
        public int Compute(int a, int b)
        {
            var total = a + b;
            var delta = total - a;
            var product = delta * b;
            if (product > 10)
            {
                product += 1;
            }

            return product;
        }
    }
}
";

                var shortTemplate = @"namespace Sample
{
    public class ShortExample
    {
        public int Compute(int a, int b)
        {
            return a + b;
        }
    }
}
";

                File.WriteAllText(Path.Combine(tempRoot.FullName, "LongOne.cs"), longTemplate);
                File.WriteAllText(Path.Combine(tempRoot.FullName, "LongTwo.cs"), longTemplate);
                File.WriteAllText(Path.Combine(tempRoot.FullName, "ShortOne.cs"), shortTemplate);
                File.WriteAllText(Path.Combine(tempRoot.FullName, "ShortTwo.cs"), shortTemplate);
                File.WriteAllText(Path.Combine(tempRoot.FullName, "ShortThree.cs"), shortTemplate);

                var scanner = new FileScanner();
                var files = scanner.FindCSharpFiles(tempRoot.FullName);
                var parser = new CSharpParser();
                var results = parser.ParseFiles(files).ToList();
                var extractor = new CandidateExtractor();
                var candidates = extractor.ExtractMany(results);
                var normalizer = new TokenNormalizer();
                var normalizedCandidates = normalizer.NormalizeMany(candidates);
                var filteredCandidates = CandidateFilters.FilterNormalizedCandidates(
                    normalizedCandidates,
                    0,
                    0,
                    new[] { CandidateKind.Method });

                var signatureComputer = new SignatureComputer();
                var hashedCandidates = signatureComputer.ComputeMany(filteredCandidates);
                var grouper = new DuplicateGrouper();

                var groups = grouper.GroupStrongDuplicates(hashedCandidates);

                Assert.Equal(2, groups.Count);
                Assert.True(groups[0].Impact > groups[1].Impact);
                Assert.Equal(2, groups[0].GroupSize);
                Assert.Equal(3, groups[1].GroupSize);
            }
            finally
            {
                tempRoot.Delete(true);
            }
        }
    }
}
