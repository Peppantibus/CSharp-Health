using System;
using System.IO;
using System.Linq;
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
        var summary = report.Summary;
        builder.AppendLine("CSharpHealth scan summary");
        builder.AppendLine($"Files: total={summary.TotalFiles} parsed_ok={summary.ParsedSuccessfully} parsed_failed={summary.ParsedFailed} diagnostics={summary.ErrorDiagnostics}");
        builder.AppendLine($"Candidates: total={summary.CandidatesTotal} methods={summary.CandidatesMethod} lambdas={summary.CandidatesLambda} blocks={summary.CandidatesBlock}");
        builder.AppendLine($"Tokens: total={summary.TokensTotal} avg={summary.TokensAverage:0.0} max={summary.TokensMax}");
        builder.AppendLine($"Signatures: total={summary.SignaturesTotal} strong_groups={summary.StrongDuplicatesGroups} strong_items={summary.StrongDuplicatesItems} strong_max_group={summary.StrongDuplicatesMaxGroupSize}");
        builder.AppendLine($"Duplicates: groups={summary.DuplicateGroups} items={summary.DuplicateItems}");

        var topCount = Math.Min(3, report.Groups.Count);
        if (topCount > 0)
        {
            builder.AppendLine("Top impacts:");
            for (var i = 0; i < topCount; i++)
            {
                var group = report.Groups[i];
                builder.AppendLine($"{i + 1}) impact={group.Impact} size={group.GroupSize} tokens={group.TokenCount} similarity={group.SimilarityPercent:0}%");
            }
        }
        else
        {
            builder.AppendLine("Top impacts: none");
        }

        return builder.ToString();
    }

    public static string FormatMarkdown(ScanReport report)
    {
        var builder = new StringBuilder();
        var summary = report.Summary;
        builder.AppendLine("# CSharpHealth Report");
        builder.AppendLine();
        builder.AppendLine("## Key findings");
        builder.AppendLine();
        builder.AppendLine($"- Files scanned: {summary.TotalFiles} (parsed ok {summary.ParsedSuccessfully}, failed {summary.ParsedFailed}, diagnostics {summary.ErrorDiagnostics})");
        builder.AppendLine($"- Duplicate groups: {summary.DuplicateGroups} (items {summary.DuplicateItems}); strong groups {summary.StrongDuplicatesGroups} (items {summary.StrongDuplicatesItems})");
        if (report.Groups.Count > 0)
        {
            var topGroup = report.Groups[0];
            builder.AppendLine($"- Highest impact group: impact {topGroup.Impact} (size {topGroup.GroupSize}, tokens {topGroup.TokenCount}, similarity {topGroup.SimilarityPercent:0}%)");
        }
        else
        {
            builder.AppendLine("- Highest impact group: none");
        }

        builder.AppendLine("- Impact measures duplicated volume: token_count × (group_size − 1).");
        builder.AppendLine();
        builder.AppendLine("## Summary");
        builder.AppendLine();
        AppendSummaryMarkdown(builder, summary);
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
            builder.AppendLine($"- Impact: {group.Impact} (token_count × (group_size − 1))");
            builder.AppendLine($"- Signature: `{group.Signature}`");
            builder.AppendLine();
            builder.AppendLine("Occurrences:");
            builder.AppendLine();

            for (var j = 0; j < group.Occurrences.Count; j++)
            {
                var occurrence = group.Occurrences[j];
                var relativePath = GetRelativePath(occurrence.FilePath);
                builder.AppendLine($"{j + 1}. `{occurrence.Kind} {relativePath}:{occurrence.StartLine}-{occurrence.EndLine}`");
            }

            var previewOccurrence = group.Occurrences.FirstOrDefault(occurrence => occurrence.PreviewLines is { Count: > 0 });
            if (previewOccurrence?.PreviewLines is not null)
            {
                builder.AppendLine();
                builder.AppendLine("Preview (sample):");
                builder.AppendLine();
                builder.AppendLine("```csharp");
                foreach (var line in previewOccurrence.PreviewLines)
                {
                    builder.AppendLine(line);
                }

                builder.AppendLine("```");
            }

            builder.AppendLine();
        }

        return builder.ToString();
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

    private static string GetRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        try
        {
            var currentDirectory = Environment.CurrentDirectory;
            var relativePath = Path.GetRelativePath(currentDirectory, path);
            return string.IsNullOrWhiteSpace(relativePath) ? path : relativePath;
        }
        catch (ArgumentException)
        {
            return path;
        }
    }
}
