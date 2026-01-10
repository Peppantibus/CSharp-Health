using CSharpHealth.Cli;

namespace CSharpHealth.Tests;

public class ReportExportTests
{
    [Fact]
    public void ScanCommand_WritesReportToOutFile()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"csharphealth-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var sourcePath = Path.Combine(tempRoot, "Sample.cs");
            File.WriteAllText(sourcePath, """
                namespace Sample;

                public class Greeter
                {
                    public string Hello(string name)
                    {
                        var message = $"Hello, {name}!";
                        return message;
                    }
                }
                """);

            var outputPath = Path.Combine(tempRoot, "report.md");
            var args = new[]
            {
                "scan",
                tempRoot,
                "--out",
                outputPath,
                "--min-tokens",
                "0",
                "--min-lines",
                "0",
                "--top",
                "1"
            };

            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var exitCode = ScanCommand.Run(args, stdout, stderr);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));
            var content = File.ReadAllText(outputPath);
            Assert.False(string.IsNullOrWhiteSpace(content));
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, true);
            }
        }
    }
}
