using CSharpHealth.Core.Reporting;

namespace CSharpHealth.Desktop.Services;

public sealed class ScanRunner
{
    public string Run(IReadOnlyList<string> files, ScanSettings settings, OutputFormatOption outputFormat)
    {
        var builder = new ScanReportBuilder();
        var result = builder.BuildReport(files, settings, kindFilter: null);

        return outputFormat switch
        {
            OutputFormatOption.Json => ScanReportFormatter.SerializeJson(result.AllGroups),
            OutputFormatOption.Markdown => ScanReportFormatter.FormatMarkdown(result.Report),
            _ => ScanReportFormatter.FormatText(result.Report)
        };
    }
}
