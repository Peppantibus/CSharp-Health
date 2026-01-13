namespace CSharpHealth.Desktop.Services;

public sealed class ResolvedSource : IDisposable
{
    private ResolvedSource(SourceKind kind, string path, string? cleanupPath)
    {
        Kind = kind;
        Path = path;
        CleanupPath = cleanupPath;
    }

    public static ResolvedSource Empty { get; } = new(SourceKind.None, string.Empty, null);

    public SourceKind Kind { get; }

    public string Path { get; }

    private string? CleanupPath { get; }

    public static ResolvedSource FromFolder(string path) => new(SourceKind.Folder, path, null);

    public static ResolvedSource FromFile(string path) => new(SourceKind.File, path, null);

    public static ResolvedSource FromTemporaryClone(string path) => new(SourceKind.Repository, path, path);

    public void Dispose()
    {
        if (string.IsNullOrWhiteSpace(CleanupPath))
        {
            return;
        }

        try
        {
            Directory.Delete(CleanupPath, recursive: true);
        }
        catch (Exception)
        {
        }
    }
}

public enum SourceKind
{
    None,
    Folder,
    File,
    Repository
}
