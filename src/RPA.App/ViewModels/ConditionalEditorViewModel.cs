using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RPA.Automation.Services;
using RPA.Core.Models;
using RPA.Core.Services;

namespace RPA.App.ViewModels;

public partial class ConditionalEditorViewModel : ObservableObject
{
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly IScreenCaptureService _screenCaptureService;

    [ObservableProperty]
    private ObservableCollection<ImageSnapshot> snapshots = new();

    [ObservableProperty]
    private ImageSnapshot? selectedSnapshot;

    [ObservableProperty]
    private int similarityThreshold = 100;

    [ObservableProperty]
    private string newSnapshotName = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Pronto";

    public ConditionalEditorViewModel(
        ISnapshotRepository snapshotRepository,
        IScreenCaptureService screenCaptureService)
    {
        _snapshotRepository = snapshotRepository;
        _screenCaptureService = screenCaptureService;

        _ = LoadSnapshots();
    }

    [RelayCommand]
    public async Task CaptureSnapshot()
    {
        try
        {
            StatusMessage = "Abrindo overlay de captura...";

            var overlay = new Views.ScreenCaptureOverlay();
            bool? result = overlay.ShowDialog();

            if (result == true && overlay.CapturedRegion != null)
            {
                StatusMessage = "Capturando imagem...";

                var imageData = await _screenCaptureService.CaptureRegionAsync(overlay.CapturedRegion);

                var snapshotName = string.IsNullOrEmpty(NewSnapshotName)
                    ? $"Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}"
                    : NewSnapshotName;

                var snapshot = new ImageSnapshot(
                    Guid.NewGuid(),
                    snapshotName,
                    imageData,
                    overlay.CapturedRegion.Width,
                    overlay.CapturedRegion.Height,
                    DateTime.Now
                );

                await _snapshotRepository.SaveAsync(snapshot);

                NewSnapshotName = string.Empty;

                await LoadSnapshots();

                StatusMessage = $"Snapshot '{snapshotName}' salvo com sucesso!";
            }
            else
            {
                StatusMessage = "Captura cancelada";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
            System.Windows.MessageBox.Show($"EXCEÇÃO: {ex.GetType().Name}\n\n{ex.Message}");
        }
    }

    [RelayCommand]
    public async Task DeleteSnapshot(ImageSnapshot? snapshot)
    {
        if (snapshot == null)
            return;

        try
        {
            await _snapshotRepository.DeleteAsync(snapshot.Id);
            await LoadSnapshots();
            StatusMessage = $"Snapshot '{snapshot.Name}' deletado";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao deletar: {ex.Message}";
        }
    }

    [RelayCommand]
    public async Task LoadSnapshots()
    {
        try
        {
            var loadedSnapshots = await _snapshotRepository.GetAllAsync();
            Snapshots = new ObservableCollection<ImageSnapshot>(loadedSnapshots);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao carregar snapshots: {ex.Message}";
        }
    }

    public ConditionalStep CreateConditionalStep(
        List<Step> thenSteps,
        List<Step>? elseSteps = null)
    {
        if (SelectedSnapshot == null)
            throw new InvalidOperationException("Selecione um snapshot para criar a condição");

        var condition = new ImageComparisonCondition(
            SelectedSnapshot.Id,
            new ImageRegion(0, 0, SelectedSnapshot.Width, SelectedSnapshot.Height),
            SimilarityThreshold
        );

        return new ConditionalStep
        {
            Condition = condition,
            ThenSteps = thenSteps,
            ElseSteps = elseSteps,
            Label = $"Se imagem '{SelectedSnapshot.Name}' ({SimilarityThreshold}%)"
        };
    }
}
