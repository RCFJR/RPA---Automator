using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RPA.Core.Models;
using RPA.Core.Services;
using System.Collections.ObjectModel;

namespace RPA.App.ViewModels;

public partial class CsvLoaderViewModel : ObservableObject
{
    private readonly ICsvLoader _csvLoader;

    [ObservableProperty]
    private string filePath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> headers = new();

    [ObservableProperty]
    private ObservableCollection<CsvRow> rows = new();

    [ObservableProperty]
    private bool isLoaded = false;

    [ObservableProperty]
    private string statusMessage = "Nenhum CSV carregado";

    [ObservableProperty]
    private string rowCountText = "0 linhas";

    public CsvLoaderViewModel(ICsvLoader csvLoader)
    {
        _csvLoader = csvLoader ?? throw new ArgumentNullException(nameof(csvLoader));
    }

    [RelayCommand]
    public async Task LoadCsv()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            DefaultExt = ".csv",
            Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() != true)
            return;

        try
        {
            StatusMessage = "Carregando...";
            var (headerList, rowList) = await _csvLoader.LoadAsync(openFileDialog.FileName);

            FilePath = openFileDialog.FileName;
            Headers.Clear();
            foreach (var header in headerList)
            {
                Headers.Add(header);
            }

            Rows.Clear();
            foreach (var row in rowList)
            {
                Rows.Add(row);
            }

            RowCountText = $"{rowList.Count} linhas";
            StatusMessage = $"CSV carregado com sucesso: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao carregar: {ex.Message}";
            IsLoaded = false;
        }
    }

    public List<CsvRow> GetLoadedRows()
    {
        return Rows.ToList();
    }

    public List<string> GetHeaders()
    {
        return Headers.ToList();
    }
}
