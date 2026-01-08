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
Console.WriteLine($"Found {files.Count} .cs files.");
