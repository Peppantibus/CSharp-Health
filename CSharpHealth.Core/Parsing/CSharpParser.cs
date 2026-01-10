using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpHealth.Core.Parsing
{
    public class CSharpParser
    {
        public ParseResult ParseFile(string filePath, CancellationToken ct = default)
        {
            string fileText;
            try
            {
                fileText = File.ReadAllText(filePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                return new ParseResult(
                    filePath,
                    false,
                    null,
                    null,
                    Array.Empty<Diagnostic>(),
                    ex.Message);
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(fileText, path: filePath);
            var diagnostics = syntaxTree.GetDiagnostics().ToList();
            var hasErrors = diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            CompilationUnitSyntax? root = null;

            if (!hasErrors)
            {
                root = syntaxTree.GetCompilationUnitRoot(ct);
            }

            return new ParseResult(
                filePath,
                !hasErrors,
                hasErrors ? null : syntaxTree,
                root,
                diagnostics,
                null);
        }

        public IEnumerable<ParseResult> ParseFiles(IEnumerable<string> filePaths, CancellationToken ct = default)
        {
            foreach (var filePath in filePaths)
            {
                ct.ThrowIfCancellationRequested();
                yield return ParseFile(filePath, ct);
            }
        }
    }
}
