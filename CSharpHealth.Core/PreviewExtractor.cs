using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpHealth.Core
{
    public static class PreviewExtractor
    {
        public static IReadOnlyList<string> GetPreviewLines(string filePath, int startLine, int endLine, int maxLines)
        {
            if (maxLines <= 0 || startLine <= 0 || endLine < startLine)
            {
                return Array.Empty<string>();
            }

            var lines = new List<string>();

            try
            {
                using var stream = File.OpenRead(filePath);
                using var reader = new StreamReader(stream);
                var currentLine = 0;

                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    currentLine++;

                    if (currentLine < startLine)
                    {
                        continue;
                    }

                    if (currentLine > endLine)
                    {
                        break;
                    }

                    if (line is null)
                    {
                        continue;
                    }

                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed))
                    {
                        continue;
                    }

                    lines.Add(trimmed);
                    if (lines.Count >= maxLines)
                    {
                        break;
                    }
                }
            }
            catch (IOException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }

            return lines;
        }
    }
}
