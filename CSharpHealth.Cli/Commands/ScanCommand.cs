using CSharpHealth.Core.Candidates;
using CSharpHealth.Core.Reporting;
using CSharpHealth.Core.Scanning;

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
            "json" => ScanReportFormatter.SerializeJson(report.AllGroups),
            "markdown" => ScanReportFormatter.FormatMarkdown(report.Report),
            _ => ScanReportFormatter.FormatText(report.Report)
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

    private static ScanReportResult BuildReport(
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
        var settings = new ScanSettings(top, minGroupSize, minTokens, minLines, previewLines);
        var builder = new ScanReportBuilder();
        return builder.BuildReport(files, settings, kindFilter);
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
