using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CSharpHealth.Core.Reporting;

public static class ScanReportFormatter
{
    public static string SerializeJson(IReadOnlyList<DuplicateGroupReport> groups)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(groups, options);
    }

    public static string FormatText(ScanReport report)
    {
        var builder = new StringBuilder();
        AppendSummaryLines(builder, report.Summary);

        for (var i = 0; i < report.Groups.Count; i++)
        {
            var group = report.Groups[i];
            builder.AppendLine($"[group {i + 1}] similarity={group.SimilarityPercent:0}% size={group.GroupSize} tokens={group.TokenCount} impact={group.Impact}");
            foreach (var occurrence in group.Occurrences)
            {
                builder.AppendLine($"- {occurrence.Kind} {occurrence.FilePath}:{occurrence.StartLine}-{occurrence.EndLine}");
                if (occurrence.PreviewLines is not null)
                {
                    foreach (var line in occurrence.PreviewLines)
                    {
                        builder.AppendLine($"  {line}");
                    }
                }
            }
        }

        return builder.ToString();
    }

    public static string FormatMarkdown(ScanReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# CSharpHealth Report");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        AppendSummaryMarkdown(builder, report.Summary);
        builder.AppendLine();
        builder.AppendLine("## Top Duplicate Groups");
        builder.AppendLine();
        builder.AppendLine("| # | Size | Token Count | Impact | Similarity |");
        builder.AppendLine("| - | ---- | ----------- | ------ | ---------- |");
        for (var i = 0; i < report.Groups.Count; i++)
        {
            var group = report.Groups[i];
            builder.AppendLine($"| {i + 1} | {group.GroupSize} | {group.TokenCount} | {group.Impact} | {group.SimilarityPercent:0}% |");
        }

        if (report.Groups.Count > 0)
        {
            builder.AppendLine();
        }

        for (var i = 0; i < report.Groups.Count; i++)
        {
            var group = report.Groups[i];
            builder.AppendLine($"### Group {i + 1}");
            builder.AppendLine();
            builder.AppendLine($"- Similarity: {group.SimilarityPercent:0}%");
            builder.AppendLine($"- Size: {group.GroupSize}");
            builder.AppendLine($"- Token count: {group.TokenCount}");
            builder.AppendLine($"- Impact: {group.Impact}");
            builder.AppendLine($"- Signature: `{group.Signature}`");
            builder.AppendLine();
            builder.AppendLine("Occurrences:");
            builder.AppendLine();

            for (var j = 0; j < group.Occurrences.Count; j++)
            {
                var occurrence = group.Occurrences[j];
                builder.AppendLine($"{j + 1}. `{occurrence.Kind} {occurrence.FilePath}:{occurrence.StartLine}-{occurrence.EndLine}`");
                if (occurrence.PreviewLines is not null)
                {
                    builder.AppendLine();
                    builder.AppendLine("```csharp");
                    foreach (var line in occurrence.PreviewLines)
                    {
                        builder.AppendLine(line);
                    }

                    builder.AppendLine("```");
                }

                builder.AppendLine();
            }
        }

        return builder.ToString();
    }

    private static void AppendSummaryLines(StringBuilder builder, ScanSummary summary)
    {
        builder.AppendLine($"total_files={summary.TotalFiles}");
        builder.AppendLine($"parsed_successfully={summary.ParsedSuccessfully}");
        builder.AppendLine($"parsed_failed={summary.ParsedFailed}");
        builder.AppendLine($"error_diagnostics={summary.ErrorDiagnostics}");
        builder.AppendLine($"candidates_total={summary.CandidatesTotal}");
        builder.AppendLine($"candidates_method={summary.CandidatesMethod}");
        builder.AppendLine($"candidates_lambda={summary.CandidatesLambda}");
        builder.AppendLine($"candidates_block={summary.CandidatesBlock}");
        builder.AppendLine($"normalized_total={summary.NormalizedTotal}");
        builder.AppendLine($"tokens_total={summary.TokensTotal}");
        builder.AppendLine($"tokens_avg={summary.TokensAverage:0.0}");
        builder.AppendLine($"tokens_max={summary.TokensMax}");
        builder.AppendLine($"signatures_total={summary.SignaturesTotal}");
        builder.AppendLine($"strong_duplicates_groups={summary.StrongDuplicatesGroups}");
        builder.AppendLine($"strong_duplicates_items={summary.StrongDuplicatesItems}");
        builder.AppendLine($"strong_duplicates_max_group_size={summary.StrongDuplicatesMaxGroupSize}");
        builder.AppendLine($"duplicates_groups={summary.DuplicateGroups}");
        builder.AppendLine($"duplicates_items={summary.DuplicateItems}");
    }

    private static void AppendSummaryMarkdown(StringBuilder builder, ScanSummary summary)
    {
        builder.AppendLine($"- Total files: {summary.TotalFiles}");
        builder.AppendLine($"- Parsed successfully: {summary.ParsedSuccessfully}");
        builder.AppendLine($"- Parsed failed: {summary.ParsedFailed}");
        builder.AppendLine($"- Error diagnostics: {summary.ErrorDiagnostics}");
        builder.AppendLine($"- Candidates total: {summary.CandidatesTotal}");
        builder.AppendLine($"- Candidates by kind: {summary.CandidatesMethod} methods, {summary.CandidatesLambda} lambdas, {summary.CandidatesBlock} blocks");
        builder.AppendLine($"- Normalized total: {summary.NormalizedTotal}");
        builder.AppendLine($"- Tokens total: {summary.TokensTotal}");
        builder.AppendLine($"- Tokens avg: {summary.TokensAverage:0.0}");
        builder.AppendLine($"- Tokens max: {summary.TokensMax}");
        builder.AppendLine($"- Signatures total: {summary.SignaturesTotal}");
        builder.AppendLine($"- Strong duplicates groups/items: {summary.StrongDuplicatesGroups}/{summary.StrongDuplicatesItems}");
        builder.AppendLine($"- Strong duplicates max group size: {summary.StrongDuplicatesMaxGroupSize}");
        builder.AppendLine($"- Duplicate groups/items: {summary.DuplicateGroups}/{summary.DuplicateItems}");
    }
}
