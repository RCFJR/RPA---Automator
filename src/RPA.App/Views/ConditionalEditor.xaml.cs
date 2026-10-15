using System.Windows.Controls;
using RPA.Core.Models;

namespace RPA.App.Views;

public partial class ConditionalEditor : UserControl
{
    public ConditionalEditor()
    {
        InitializeComponent();
    }

    private async void CaptureButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is ViewModels.ConditionalEditorViewModel vm)
        {
            await vm.CaptureSnapshotCommand.ExecuteAsync(null);
        }
    }

    private void DeleteButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button button && DataContext is ViewModels.ConditionalEditorViewModel vm)
        {
            var snapshot = button.Tag as ImageSnapshot;
            if (snapshot != null)
            {
                vm.DeleteSnapshotCommand.Execute(snapshot);
            }
        }
    }
}
