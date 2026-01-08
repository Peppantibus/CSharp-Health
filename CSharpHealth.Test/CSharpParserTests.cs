using System;
using System.IO;
using System.Linq;
using CSharpHealth.Core;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CSharpHealth.Test
{
    public class CSharpParserTests
    {
        [Fact]
        public void ParseFile_WithValidSyntax_ReturnsSuccessfulResult()
        {
            var filePath = CreateTemporaryFile("Valid.cs", "namespace Sample { public class Test { } }");
            var parser = new CSharpParser();

            try
            {
                var result = parser.ParseFile(filePath);

                Assert.True(result.Success);
                Assert.NotNull(result.SyntaxTree);
                Assert.NotNull(result.Root);
                Assert.Empty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithInvalidSyntax_ReturnsFailureWithDiagnostics()
        {
            var filePath = CreateTemporaryFile("Invalid.cs", "namespace Sample { public class Test { ");
            var parser = new CSharpParser();

            try
            {
                var result = parser.ParseFile(filePath);

                Assert.False(result.Success);
                Assert.NotEmpty(result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            }
            finally
            {
                File.Delete(filePath);
            }
        }

        [Fact]
        public void ParseFile_WithMissingFile_ReturnsFailureWithErrorMessage()
        {
            var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Missing.cs");
            var parser = new CSharpParser();

            var result = parser.ParseFile(filePath);

            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        private static string CreateTemporaryFile(string fileName, string contents)
        {
            var root = Path.Combine(Path.GetTempPath(), "CSharpHealthParserTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);

            var filePath = Path.Combine(root, fileName);
            File.WriteAllText(filePath, contents);

            return filePath;
        }
    }
}
