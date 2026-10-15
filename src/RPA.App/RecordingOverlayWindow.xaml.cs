using System.Windows;
using RPA.App.ViewModels;

namespace RPA.App;

public partial class RecordingOverlayWindow : Window
{
    public RecordingOverlayWindow(RecordingOverlayViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Top + 20;
    }
}
