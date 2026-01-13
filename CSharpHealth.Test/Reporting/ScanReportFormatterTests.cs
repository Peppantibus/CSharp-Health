using System.IO;
using System.Linq;
using CSharpHealth.Core.Reporting;

namespace CSharpHealth.Tests.Reporting;

public class ScanReportFormatterTests
{
    [Fact]
    public void FormatText_ProducesCompactSummaryWithoutSnippetsOrAbsolutePaths()
    {
        var fullPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "csharphealth", "Sample.cs"));
        var report = BuildReport(fullPath);

        var text = ScanReportFormatter.FormatText(report);

        Assert.DoesNotContain(fullPath, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("public void", text, StringComparison.Ordinal);
        Assert.True(text.Split(Environment.NewLine, StringSplitOptions.None).Length <= 20);
    }

    [Fact]
    public void FormatMarkdown_IncludesKeyFindingsAndRelativePathsWithSinglePreview()
    {
        var fullPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "csharphealth", "Sample.cs"));
        var report = BuildReport(fullPath);

        var markdown = ScanReportFormatter.FormatMarkdown(report);
        var relativePath = Path.GetRelativePath(Environment.CurrentDirectory, fullPath);

        Assert.Contains("Key findings", markdown, StringComparison.Ordinal);
        Assert.Contains(relativePath, markdown, StringComparison.Ordinal);
        Assert.Single(markdown.Split("```csharp", StringSplitOptions.None).Skip(1));
    }

    [Fact]
    public void SerializeJson_KeepsStructureAndContentStable()
    {
        var fullPath = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "csharphealth", "Sample.cs"));
        var report = BuildReport(fullPath);

        var json = ScanReportFormatter.SerializeJson(report.Groups);
        var expected = """
            [
              {
                "Signature": "signature",
                "SimilarityPercent": 87.5,
                "GroupSize": 3,
                "TokenCount": 120,
                "Impact": 240,
                "Occurrences": [
                  {
                    "Kind": "Method",
                    "FilePath": "PATH",
                    "StartLine": 10,
                    "EndLine": 20,
                    "PreviewLines": [
                      "public void Hello()",
                      "return;"
                    ]
                  }
                ]
              }
            ]
            """;

        expected = expected.Replace("PATH", fullPath);

        Assert.Equal(expected.ReplaceLineEndings("\n"), json.ReplaceLineEndings("\n"));
    }

    private static ScanReport BuildReport(string filePath)
    {
        var summary = new ScanSummary(
            5,
            4,
            1,
            2,
            12,
            7,
            3,
            2,
            12,
            240,
            20.0,
            120,
            10,
            2,
            6,
            4,
            2,
            6);

        var occurrences = new List<DuplicateOccurrenceReport>
        {
            new(
                "Method",
                filePath,
                10,
                20,
                new List<string>
                {
                    "public void Hello()",
                    "return;"
                })
        };

        var groups = new List<DuplicateGroupReport>
        {
            new("signature", 87.5, 3, 120, 240, occurrences)
        };

        return new ScanReport(summary, groups);
    }
}
