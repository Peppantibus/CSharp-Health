using System;
using System.IO;
using System.Linq;
using CSharpHealth.Core;
using Xunit;

namespace CSharpHealth.Test
{
    public class FileScannerTests
    {
        [Fact]
        public void FindCSharpFiles_ExcludesBinAndObjDirectories()
        {
            var root = CreateTemporaryDirectory();

            try
            {
                var srcPath = Path.Combine(root, "src");
                Directory.CreateDirectory(srcPath);
                File.WriteAllText(Path.Combine(srcPath, "App.cs"), "// test");

                var binPath = Path.Combine(root, "bin");
                Directory.CreateDirectory(binPath);
                File.WriteAllText(Path.Combine(binPath, "Ignore.cs"), "// ignore");

                var objPath = Path.Combine(root, "obj");
                Directory.CreateDirectory(objPath);
                File.WriteAllText(Path.Combine(objPath, "Ignore.cs"), "// ignore");

                var scanner = new FileScanner();

                var results = scanner.FindCSharpFiles(root);

                Assert.Single(results);
                Assert.Contains(results, path => path.EndsWith("App.cs", StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(results, path => path.Contains(Path.Combine(root, "bin"), StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(results, path => path.Contains(Path.Combine(root, "obj"), StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        [Fact]
        public void FindCSharpFiles_ReturnsOnlyCSharpFiles()
        {
            var root = CreateTemporaryDirectory();

            try
            {
                File.WriteAllText(Path.Combine(root, "One.cs"), "// test");
                File.WriteAllText(Path.Combine(root, "Two.txt"), "ignore");

                var scanner = new FileScanner();

                var results = scanner.FindCSharpFiles(root);

                Assert.Single(results);
                Assert.True(results.All(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }

        private static string CreateTemporaryDirectory()
        {
            var root = Path.Combine(Path.GetTempPath(), "CSharpHealthTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }
    }
}
