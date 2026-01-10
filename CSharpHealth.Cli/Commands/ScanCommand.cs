using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpHealth.Core.Candidates;
using CSharpHealth.Core.Duplicates;
using CSharpHealth.Core.Normalization;
using CSharpHealth.Core.Parsing;
using CSharpHealth.Core.Reporting;
using CSharpHealth.Core.Scanning;
using CSharpHealth.Core.Similarity;

namespace CSharpHealth.Cli.Commands;

internal static class ScanCommand
{
    public static int Run(string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length < 2 || !string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
        {
            stderr.WriteLine("Usage: scan <path> [--top <K>] [--min-group-size <N>] [--min-tokens <N>] [--min-lines <N>] [--kinds Method|Lambda|Block] [--preview-lines <N>] [--format <text|json|markdown>] [--out <path>]");
            return 1;
        }

        var path = args[1];
        if (!Directory.Exists(path))
        {
            stderr.WriteLine($"Error: path '{path}' does not exist or is not a directory.");
            return 1;
        }

        var top = 10;
        var minGroupSize = 2;
        var minTokens = 50;
        var minLines = 6;
        var previewLines = 3;
        var format = "text";
        var formatSpecified = false;
        string? outPath = null;
        HashSet<CandidateKind>? kindFilter = null;

        for (var i = 2; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--top":
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out top) || top <= 0)
                    {
                        stderr.WriteLine("Error: --top expects a positive integer.");
                        return 1;
                    }

                    i++;
                    break;
                case "--min-group-size":
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out minGroupSize) || minGroupSize <= 0)
                    {
                        stderr.WriteLine("Error: --min-group-size expects a positive integer.");
                        return 1;
                    }

                    i++;
                    break;
                case "--min-tokens":
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out minTokens) || minTokens < 0)
                    {
                        stderr.WriteLine("Error: --min-tokens expects a non-negative integer.");
                        return 1;
                    }

                    i++;
                    break;
                case "--min-lines":
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out minLines) || minLines < 0)
                    {
                        stderr.WriteLine("Error: --min-lines expects a non-negative integer.");
                        return 1;
                    }

                    i++;
                    break;
                case "--kinds":
                    if (i + 1 >= args.Length)
                    {
                        stderr.WriteLine("Error: --kinds expects a comma-separated list (Method,Lambda,Block).");
                        return 1;
                    }

                    kindFilter = ParseKinds(args[i + 1]);
                    if (kindFilter.Count == 0)
                    {
                        stderr.WriteLine("Error: --kinds expects a comma-separated list (Method,Lambda,Block).");
                        return 1;
                    }

                    i++;
                    break;
                case "--preview-lines":
                    if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out previewLines) || previewLines < 0)
                    {
                        stderr.WriteLine("Error: --preview-lines expects a non-negative integer.");
                        return 1;
                    }

                    i++;
                    break;
                case "--format":
                    if (i + 1 >= args.Length)
                    {
                        stderr.WriteLine("Error: --format expects 'text', 'json', or 'markdown'.");
                        return 1;
                    }

                    if (!TryNormalizeFormat(args[i + 1], out format))
                    {
                        stderr.WriteLine("Error: --format expects 'text', 'json', or 'markdown'.");
                        return 1;
                    }

                    formatSpecified = true;
                    i++;
                    break;
                case "--out":
                    if (i + 1 >= args.Length)
                    {
                        stderr.WriteLine("Error: --out expects a file path.");
                        return 1;
                    }

                    outPath = args[i + 1];
                    i++;
                    break;
                default:
                    stderr.WriteLine($"Error: unknown option '{arg}'.");
                    return 1;
            }
        }

        if (outPath is not null && !formatSpecified)
        {
            if (!TryInferFormatFromPath(outPath, out format))
            {
                stderr.WriteLine("Error: unable to infer format from --out path. Use .json, .md, or .txt, or set --format explicitly.");
                return 1;
            }
        }

        var report = BuildReport(path, top, minGroupSize, minTokens, minLines, previewLines, kindFilter);
        var output = format switch
        {
            "json" => SerializeJson(report.JsonGroups),
            "markdown" => FormatMarkdown(report.Report),
            _ => FormatText(report.Report)
        };

        stdout.Write(output);

        if (outPath is not null)
        {
            try
            {
                var directory = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(outPath, output);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                stderr.WriteLine($"Error: failed to write report to '{outPath}'. {ex.Message}");
                return 1;
            }
        }

        return 0;
    }

    private static (ScanReport Report, IReadOnlyList<DuplicateGroupReport> JsonGroups) BuildReport(
        string path,
        int top,
        int minGroupSize,
        int minTokens,
        int minLines,
        int previewLines,
        HashSet<CandidateKind>? kindFilter)
    {
        var scanner = new FileScanner();
        var files = scanner.FindCSharpFiles(path);

        var parser = new CSharpParser();
        var results = parser.ParseFiles(files).ToList();

        var successCount = 0;
        var failedCount = 0;
        var errorDiagnostics = 0;

        foreach (var result in results)
        {
            if (result.Success)
            {
                successCount++;
            }
            else
            {
                failedCount++;
            }

            errorDiagnostics += result.Diagnostics.Count(diagnostic => diagnostic.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        }

        var extractor = new CandidateExtractor();
        var candidates = extractor.ExtractMany(results);

        var normalizer = new TokenNormalizer();
        var normalizedCandidates = normalizer.NormalizeMany(candidates);
        var filteredCandidates = CandidateFilters.FilterNormalizedCandidates(
            normalizedCandidates,
            minTokens,
            minLines,
            kindFilter);
        var tokensTotal = filteredCandidates.Sum(candidate => candidate.TokenCount);
        var tokensMax = filteredCandidates.Count > 0 ? filteredCandidates.Max(candidate => candidate.TokenCount) : 0;
        var tokensAverage = filteredCandidates.Count > 0
            ? Math.Round(tokensTotal / (double)filteredCandidates.Count, 1)
            : 0;

        var signatureComputer = new SignatureComputer();
        var hashedCandidates = signatureComputer.ComputeMany(filteredCandidates);
        var signatureGroups = hashedCandidates
            .GroupBy(candidate => candidate.StrongSignatureHex, StringComparer.Ordinal)
            .ToList();
        var strongDuplicateGroups = signatureGroups.Where(group => group.Count() >= 2).ToList();
        var strongDuplicateItems = strongDuplicateGroups.Sum(group => group.Count());
        var maxGroupSize = strongDuplicateGroups.Count > 0 ? strongDuplicateGroups.Max(group => group.Count()) : 0;

        var grouper = new DuplicateGrouper();
        var duplicateGroups = grouper.GroupStrongDuplicates(hashedCandidates, minGroupSize);
        var duplicateItems = duplicateGroups.Sum(group => group.GroupSize);

        var summary = new ScanSummary(
            files.Count,
            successCount,
            failedCount,
            errorDiagnostics,
            filteredCandidates.Count,
            filteredCandidates.Count(candidate => candidate.Candidate.Kind == CandidateKind.Method),
            filteredCandidates.Count(candidate => candidate.Candidate.Kind == CandidateKind.Lambda),
            filteredCandidates.Count(candidate => candidate.Candidate.Kind == CandidateKind.Block),
            filteredCandidates.Count,
            tokensTotal,
            tokensAverage,
            tokensMax,
            hashedCandidates.Count,
            strongDuplicateGroups.Count,
            strongDuplicateItems,
            maxGroupSize,
            duplicateGroups.Count,
            duplicateItems);

        var topGroups = duplicateGroups.Take(top).ToList();
        var reportGroups = BuildGroupReports(topGroups, previewLines);
        var jsonGroups = BuildGroupReports(duplicateGroups, previewLines);

        return (new ScanReport(summary, reportGroups), jsonGroups);
    }

    private static IReadOnlyList<DuplicateGroupReport> BuildGroupReports(
        IReadOnlyList<DuplicateGroup> groups,
        int previewLines)
    {
        return groups
            .Select(group => new DuplicateGroupReport(
                group.Signature,
                group.SimilarityPercent,
                group.GroupSize,
                group.TokenCount,
                group.Impact,
                group.Occurrences
                    .Select(occurrence => new DuplicateOccurrenceReport(
                        occurrence.CandidateKind.ToString(),
                        occurrence.FilePath,
                        occurrence.StartLine,
                        occurrence.EndLine,
                        previewLines > 0
                            ? PreviewExtractor.GetPreviewLines(occurrence.FilePath, occurrence.StartLine, occurrence.EndLine, previewLines)
                            : null))
                    .ToList()))
            .ToList();
    }

    private static string SerializeJson(IReadOnlyList<DuplicateGroupReport> groups)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        return JsonSerializer.Serialize(groups, options);
    }

    private static string FormatText(ScanReport report)
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

    private static string FormatMarkdown(ScanReport report)
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

    private static HashSet<CandidateKind> ParseKinds(string rawKinds)
    {
        var kinds = new HashSet<CandidateKind>();
        var entries = rawKinds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var entry in entries)
        {
            if (!Enum.TryParse(entry, true, out CandidateKind kind))
            {
                return new HashSet<CandidateKind>();
            }

            kinds.Add(kind);
        }

        return kinds;
    }

    private static bool TryNormalizeFormat(string raw, out string format)
    {
        format = raw.Trim().ToLowerInvariant();
        if (format == "md")
        {
            format = "markdown";
        }

        return format is "text" or "json" or "markdown";
    }

    private static bool TryInferFormatFromPath(string outPath, out string format)
    {
        format = "text";
        var extension = Path.GetExtension(outPath).ToLowerInvariant();
        switch (extension)
        {
            case ".json":
                format = "json";
                return true;
            case ".md":
            case ".markdown":
                format = "markdown";
                return true;
            case ".txt":
                format = "text";
                return true;
            default:
                return false;
        }
    }
}

internal sealed record ScanSummary(
    int TotalFiles,
    int ParsedSuccessfully,
    int ParsedFailed,
    int ErrorDiagnostics,
    int CandidatesTotal,
    int CandidatesMethod,
    int CandidatesLambda,
    int CandidatesBlock,
    int NormalizedTotal,
    int TokensTotal,
    double TokensAverage,
    int TokensMax,
    int SignaturesTotal,
    int StrongDuplicatesGroups,
    int StrongDuplicatesItems,
    int StrongDuplicatesMaxGroupSize,
    int DuplicateGroups,
    int DuplicateItems);

internal sealed record ScanReport(
    ScanSummary Summary,
    IReadOnlyList<DuplicateGroupReport> Groups);

internal sealed record DuplicateGroupReport(
    string Signature,
    double SimilarityPercent,
    int GroupSize,
    int TokenCount,
    int Impact,
    IReadOnlyList<DuplicateOccurrenceReport> Occurrences);

internal sealed record DuplicateOccurrenceReport(
    string Kind,
    string FilePath,
    int StartLine,
    int EndLine,
    IReadOnlyList<string>? PreviewLines);
