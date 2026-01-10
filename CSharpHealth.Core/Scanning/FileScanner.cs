using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpHealth.Core.Scanning
{
    public class FileScanner
    {
        private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
        {
            "bin",
            "obj",
            ".git",
            ".vs"
        };

        public IReadOnlyList<string> FindCSharpFiles(string rootPath)
        {
            var results = new List<string>();
            var pending = new Stack<string>();
            pending.Push(rootPath);

            while (pending.Count > 0)
            {
                var current = pending.Pop();

                IEnumerable<string> entries;
                try
                {
                    entries = Directory.EnumerateFileSystemEntries(current);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var entry in entries)
                {
                    try
                    {
                        if (Directory.Exists(entry))
                        {
                            var name = Path.GetFileName(entry);
                            if (!ExcludedDirectories.Contains(name))
                            {
                                pending.Push(entry);
                            }

                            continue;
                        }

                        if (string.Equals(Path.GetExtension(entry), ".cs", StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(entry);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return results;
        }
    }
}
