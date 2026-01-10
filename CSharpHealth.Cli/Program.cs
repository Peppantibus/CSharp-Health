using System.Linq;
using System.Text.Json;
using CSharpHealth.Core;

if (args.Length < 2 || !string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: scan <path> [--top <K>] [--min-group-size <N>] [--format <text|json>]");
    Environment.ExitCode = 1;
    return;
}

var path = args[1];
if (!Directory.Exists(path))
{
    Console.Error.WriteLine($"Error: path '{path}' does not exist or is not a directory.");
    Environment.ExitCode = 1;
    return;
}

var top = 10;
var minGroupSize = 2;
var format = "text";

for (var i = 2; i < args.Length; i++)
{
    var arg = args[i];
    switch (arg)
    {
        case "--top":
            if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out top) || top <= 0)
            {
                Console.Error.WriteLine("Error: --top expects a positive integer.");
                Environment.ExitCode = 1;
                return;
            }

            i++;
            break;
        case "--min-group-size":
            if (i + 1 >= args.Length || !int.TryParse(args[i + 1], out minGroupSize) || minGroupSize <= 0)
            {
                Console.Error.WriteLine("Error: --min-group-size expects a positive integer.");
                Environment.ExitCode = 1;
                return;
            }

            i++;
            break;
        case "--format":
            if (i + 1 >= args.Length)
            {
                Console.Error.WriteLine("Error: --format expects 'text' or 'json'.");
                Environment.ExitCode = 1;
                return;
            }

            format = args[i + 1].Trim().ToLowerInvariant();
            if (format != "text" && format != "json")
            {
                Console.Error.WriteLine("Error: --format expects 'text' or 'json'.");
                Environment.ExitCode = 1;
                return;
            }

            i++;
            break;
        default:
            Console.Error.WriteLine($"Error: unknown option '{arg}'.");
            Environment.ExitCode = 1;
            return;
    }
}

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
var tokensTotal = normalizedCandidates.Sum(candidate => candidate.TokenCount);
var tokensMax = normalizedCandidates.Count > 0 ? normalizedCandidates.Max(candidate => candidate.TokenCount) : 0;
var tokensAverage = normalizedCandidates.Count > 0
    ? Math.Round(tokensTotal / (double)normalizedCandidates.Count, 1)
    : 0;

var signatureComputer = new SignatureComputer();
var hashedCandidates = signatureComputer.ComputeMany(normalizedCandidates);
var signatureGroups = hashedCandidates
    .GroupBy(candidate => candidate.StrongSignatureHex, StringComparer.Ordinal)
    .ToList();
var strongDuplicateGroups = signatureGroups.Where(group => group.Count() >= 2).ToList();
var strongDuplicateItems = strongDuplicateGroups.Sum(group => group.Count());
var maxGroupSize = strongDuplicateGroups.Count > 0 ? strongDuplicateGroups.Max(group => group.Count()) : 0;

var grouper = new DuplicateGrouper();
var duplicateGroups = grouper.GroupStrongDuplicates(hashedCandidates, minGroupSize);
var duplicateItems = duplicateGroups.Sum(group => group.GroupSize);

if (format == "json")
{
    var options = new JsonSerializerOptions
    {
        WriteIndented = true
    };

    Console.WriteLine(JsonSerializer.Serialize(duplicateGroups, options));
    return;
}

Console.WriteLine($"total_files={files.Count}");
Console.WriteLine($"parsed_successfully={successCount}");
Console.WriteLine($"parsed_failed={failedCount}");
Console.WriteLine($"error_diagnostics={errorDiagnostics}");
Console.WriteLine($"candidates_total={candidates.Count}");
Console.WriteLine($"candidates_method={candidates.Count(candidate => candidate.Kind == CandidateKind.Method)}");
Console.WriteLine($"candidates_lambda={candidates.Count(candidate => candidate.Kind == CandidateKind.Lambda)}");
Console.WriteLine($"candidates_block={candidates.Count(candidate => candidate.Kind == CandidateKind.Block)}");
Console.WriteLine($"normalized_total={normalizedCandidates.Count}");
Console.WriteLine($"tokens_total={tokensTotal}");
Console.WriteLine($"tokens_avg={tokensAverage:0.0}");
Console.WriteLine($"tokens_max={tokensMax}");
Console.WriteLine($"signatures_total={hashedCandidates.Count}");
Console.WriteLine($"strong_duplicates_groups={strongDuplicateGroups.Count}");
Console.WriteLine($"strong_duplicates_items={strongDuplicateItems}");
Console.WriteLine($"strong_duplicates_max_group_size={maxGroupSize}");
Console.WriteLine($"duplicates_groups={duplicateGroups.Count}");
Console.WriteLine($"duplicates_items={duplicateItems}");

var groupsToShow = duplicateGroups
    .Take(top)
    .ToList();

for (var i = 0; i < groupsToShow.Count; i++)
{
    var group = groupsToShow[i];
    Console.WriteLine($"[group {i + 1}] similarity={group.SimilarityPercent:0}% size={group.GroupSize} tokens={group.TokenCount}");
    foreach (var occurrence in group.Occurrences)
    {
        Console.WriteLine($"- {occurrence.CandidateKind} {occurrence.FilePath}:{occurrence.StartLine}-{occurrence.EndLine}");
    }
}
