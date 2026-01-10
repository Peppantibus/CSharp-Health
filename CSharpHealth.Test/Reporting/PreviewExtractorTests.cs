using System.IO;
using CSharpHealth.Core.Reporting;
using Xunit;

namespace CSharpHealth.Tests.Reporting
{
    public class PreviewExtractorTests
    {
        [Fact]
        public void GetPreviewLines_SkipsEmptyLinesAndHonorsMax()
        {
            var tempRoot = Directory.CreateTempSubdirectory("csharphealth-preview-test-");
            try
            {
                var filePath = Path.Combine(tempRoot.FullName, "Sample.cs");
                File.WriteAllText(filePath, @"namespace Sample
{

    public class Example
    {
        public void Demo()
        {

            var value = 1;

            var total = value + 2;

            return;
        }
    }
}
");

                var preview = PreviewExtractor.GetPreviewLines(filePath, 1, 20, 3);

                Assert.Equal(3, preview.Count);
                Assert.DoesNotContain(preview, line => string.IsNullOrWhiteSpace(line));
                Assert.Equal("namespace Sample", preview[0]);
            }
            finally
            {
                tempRoot.Delete(true);
            }
        }
    }
}
