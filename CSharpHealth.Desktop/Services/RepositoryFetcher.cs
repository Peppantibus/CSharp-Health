using System.Diagnostics;

namespace CSharpHealth.Desktop.Services;

public sealed class RepositoryFetcher
{
    public ResolvedSource Fetch(string repositoryUrl)
    {
        var target = Path.Combine(Path.GetTempPath(), "CSharpHealth", $"repo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(target);

        try
        {
            CloneRepository(repositoryUrl, target);
            return ResolvedSource.FromTemporaryClone(target);
        }
        catch
        {
            try
            {
                Directory.Delete(target, recursive: true);
            }
            catch (Exception)
            {
            }

            throw;
        }
    }

    private static void CloneRepository(string repositoryUrl, string target)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --depth 1 {repositoryUrl} \"{target}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Git clone failed: {error}{output}");
        }
    }
}
