using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpHealth.Core
{
    public sealed record ParseResult(
        string FilePath,
        bool Success,
        SyntaxTree? SyntaxTree,
        CompilationUnitSyntax? Root,
        IReadOnlyList<Diagnostic> Diagnostics,
        string? ErrorMessage
    );
}
