using CSharpHealth.Core.Candidates;
using CSharpHealth.Core.Duplicates;
using CSharpHealth.Core.Normalization;
using CSharpHealth.Core.Parsing;
using CSharpHealth.Core.Similarity;

namespace CSharpHealth.Core.Reporting;

public sealed class ScanReportBuilder
{
    public ScanReportResult BuildReport(
        IReadOnlyList<string> files,
        ScanSettings settings,
        HashSet<CandidateKind>? kindFilter)
    {
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
            settings.MinTokens,
            settings.MinLines,
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
        var duplicateGroups = grouper.GroupStrongDuplicates(hashedCandidates, settings.MinGroupSize);
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

        var topGroups = duplicateGroups.Take(settings.Top).ToList();
        var reportGroups = BuildGroupReports(topGroups, settings.PreviewLines);
        var jsonGroups = BuildGroupReports(duplicateGroups, settings.PreviewLines);

        return new ScanReportResult(new ScanReport(summary, reportGroups), jsonGroups);
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
}

public sealed record ScanSettings(
    int Top,
    int MinGroupSize,
    int MinTokens,
    int MinLines,
    int PreviewLines);

public sealed record ScanReportResult(
    ScanReport Report,
    IReadOnlyList<DuplicateGroupReport> AllGroups);

public sealed record ScanSummary(
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

public sealed record ScanReport(
    ScanSummary Summary,
    IReadOnlyList<DuplicateGroupReport> Groups);

public sealed record DuplicateGroupReport(
    string Signature,
    double SimilarityPercent,
    int GroupSize,
    int TokenCount,
    int Impact,
    IReadOnlyList<DuplicateOccurrenceReport> Occurrences);

public sealed record DuplicateOccurrenceReport(
    string Kind,
    string FilePath,
    int StartLine,
    int EndLine,
    IReadOnlyList<string>? PreviewLines);
