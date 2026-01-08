using System.Linq;
using CSharpHealth.Core;

if (args.Length != 2 || !string.Equals(args[0], "scan", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: scan <path>");
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

Console.WriteLine($"total_files={files.Count}");
Console.WriteLine($"parsed_successfully={successCount}");
Console.WriteLine($"parsed_failed={failedCount}");
Console.WriteLine($"error_diagnostics={errorDiagnostics}");

var extractor = new CandidateExtractor();
var candidates = extractor.ExtractMany(results);

Console.WriteLine($"candidates_total={candidates.Count}");
Console.WriteLine($"candidates_method={candidates.Count(candidate => candidate.Kind == CandidateKind.Method)}");
Console.WriteLine($"candidates_lambda={candidates.Count(candidate => candidate.Kind == CandidateKind.Lambda)}");
Console.WriteLine($"candidates_block={candidates.Count(candidate => candidate.Kind == CandidateKind.Block)}");

var normalizer = new TokenNormalizer();
var normalizedCandidates = normalizer.NormalizeMany(candidates);
var tokensTotal = normalizedCandidates.Sum(candidate => candidate.TokenCount);
var tokensMax = normalizedCandidates.Count > 0 ? normalizedCandidates.Max(candidate => candidate.TokenCount) : 0;
var tokensAverage = normalizedCandidates.Count > 0
    ? Math.Round(tokensTotal / (double)normalizedCandidates.Count, 1)
    : 0;

Console.WriteLine($"normalized_total={normalizedCandidates.Count}");
Console.WriteLine($"tokens_total={tokensTotal}");
Console.WriteLine($"tokens_avg={tokensAverage:0.0}");
Console.WriteLine($"tokens_max={tokensMax}");
