using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CSharpHealth.Core.Reporting;
using CSharpHealth.Core.Scanning;
using CSharpHealth.Desktop.Services;
using Forms = System.Windows.Forms;

namespace CSharpHealth.Desktop.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ScanRunner _scanRunner = new();
    private readonly RepositoryFetcher _repositoryFetcher = new();

    private string _folderPath = string.Empty;
    private string _filePath = string.Empty;
    private string _repoUrl = string.Empty;
    private string _outputText = "";
    private string _statusMessage = "Pronto.";
    private int _topGroups = 10;
    private OutputFormatOption _selectedOutputFormat;
    private SourceType _sourceType = SourceType.Folder;
    private bool _isBusy;

    public MainViewModel()
    {
        OutputFormats = new ObservableCollection<OutputFormatOption>
        {
            OutputFormatOption.Text,
            OutputFormatOption.Markdown,
            OutputFormatOption.Json
        };
        _selectedOutputFormat = OutputFormatOption.Text;

        BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
        BrowseFileCommand = new RelayCommand(_ => BrowseFile());
        RunScanCommand = new RelayCommand(_ => _ = RunScanAsync(), _ => CanRunScan);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<OutputFormatOption> OutputFormats { get; }

    public ICommand BrowseFolderCommand { get; }

    public ICommand BrowseFileCommand { get; }

    public ICommand RunScanCommand { get; }

    public string FolderPath
    {
        get => _folderPath;
        set => SetField(ref _folderPath, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetField(ref _filePath, value);
    }

    public string RepoUrl
    {
        get => _repoUrl;
        set => SetField(ref _repoUrl, value);
    }

    public string OutputText
    {
        get => _outputText;
        set => SetField(ref _outputText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public int TopGroups
    {
        get => _topGroups;
        set => SetField(ref _topGroups, value);
    }

    public OutputFormatOption SelectedOutputFormat
    {
        get => _selectedOutputFormat;
        set => SetField(ref _selectedOutputFormat, value);
    }

    public bool IsFolderSource
    {
        get => _sourceType == SourceType.Folder;
        set
        {
            if (value)
            {
                SourceType = SourceType.Folder;
            }
        }
    }

    public bool IsFileSource
    {
        get => _sourceType == SourceType.File;
        set
        {
            if (value)
            {
                SourceType = SourceType.File;
            }
        }
    }

    public bool IsRepoSource
    {
        get => _sourceType == SourceType.Repository;
        set
        {
            if (value)
            {
                SourceType = SourceType.Repository;
            }
        }
    }

    public bool CanRunScan => !_isBusy;

    private SourceType SourceType
    {
        get => _sourceType;
        set
        {
            if (SetField(ref _sourceType, value))
            {
                OnPropertyChanged(nameof(IsFolderSource));
                OnPropertyChanged(nameof(IsFileSource));
                OnPropertyChanged(nameof(IsRepoSource));
            }
        }
    }

    private bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetField(ref _isBusy, value))
            {
                (RunScanCommand as RelayCommand)?.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanRunScan));
            }
        }
    }

    private void BrowseFolder()
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Seleziona la cartella da analizzare"
        };

        if (dialog.ShowDialog() == DialogResult.OK)
        {
            FolderPath = dialog.SelectedPath;
        }
    }

    private void BrowseFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "C# files (*.cs)|*.cs|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            FilePath = dialog.FileName;
        }
    }

    private async Task RunScanAsync()
    {
        if (!TryResolveSource(out var resolved))
        {
            return;
        }

        IsBusy = true;
        StatusMessage = "Scansione in corso...";
        OutputText = string.Empty;

        try
        {
            var files = await Task.Run(() => CollectFiles(resolved));
            if (files.Count == 0)
            {
                StatusMessage = "Nessun file C# trovato nella sorgente selezionata.";
                return;
            }

            Debug.WriteLine($"Scanning {files.Count} files.");

            var settings = new ScanSettings(
                TopGroups > 0 ? TopGroups : 10,
                MinGroupSize: 2,
                MinTokens: 50,
                MinLines: 6,
                PreviewLines: 3);

            var output = await Task.Run(() => _scanRunner.Run(files, settings, SelectedOutputFormat));
            OutputText = output;
            StatusMessage = $"Scansione completata. File analizzati: {files.Count}.";
        }
        catch (Exception ex)
        {
            StatusMessage = "Errore durante la scansione.";
            OutputText = ex.Message;
        }
        finally
        {
            resolved.Dispose();
            IsBusy = false;
        }
    }

    private bool TryResolveSource(out ResolvedSource resolvedSource)
    {
        resolvedSource = ResolvedSource.Empty;
        switch (SourceType)
        {
            case SourceType.Folder:
                if (string.IsNullOrWhiteSpace(FolderPath))
                {
                    StatusMessage = "Seleziona una cartella.";
                    return false;
                }

                if (!Directory.Exists(FolderPath))
                {
                    StatusMessage = "La cartella selezionata non esiste.";
                    return false;
                }

                resolvedSource = ResolvedSource.FromFolder(FolderPath);
                return true;
            case SourceType.File:
                if (string.IsNullOrWhiteSpace(FilePath))
                {
                    StatusMessage = "Seleziona un file C#.";
                    return false;
                }

                if (!File.Exists(FilePath))
                {
                    StatusMessage = "Il file selezionato non esiste.";
                    return false;
                }

                if (!string.Equals(Path.GetExtension(FilePath), ".cs", StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "Seleziona un file con estensione .cs.";
                    return false;
                }

                resolvedSource = ResolvedSource.FromFile(FilePath);
                return true;
            case SourceType.Repository:
                if (string.IsNullOrWhiteSpace(RepoUrl))
                {
                    StatusMessage = "Inserisci l'URL del repository.";
                    return false;
                }

                try
                {
                    resolvedSource = _repositoryFetcher.Fetch(RepoUrl.Trim());
                    return true;
                }
                catch (Exception ex)
                {
                    StatusMessage = "Errore durante il clone del repository.";
                    OutputText = ex.Message;
                    return false;
                }
            default:
                StatusMessage = "Seleziona una sorgente valida.";
                return false;
        }
    }

    private static IReadOnlyList<string> CollectFiles(ResolvedSource source)
    {
        if (source.Kind == SourceKind.File)
        {
            return new List<string> { source.Path };
        }

        var scanner = new FileScanner();
        return scanner.FindCSharpFiles(source.Path);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum SourceType
{
    Folder,
    File,
    Repository
}
