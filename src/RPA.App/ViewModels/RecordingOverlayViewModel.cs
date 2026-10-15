using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RPA.App.ViewModels;

public partial class RecordingOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private int actionsCaptured = 0;

    public string ActionsCapturedText => $"{ActionsCaptured} ação(ões) capturada(s)";

    partial void OnActionsCapturedChanged(int value)
    {
        OnPropertyChanged(nameof(ActionsCapturedText));
    }

    public event EventHandler? StopRequested;

    [RelayCommand]
    private void Stop()
    {
        StopRequested?.Invoke(this, EventArgs.Empty);
    }
}
