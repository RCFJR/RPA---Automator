using System.Windows;
using System.Windows.Controls;
using RPA.App.ViewModels;
using RPA.Core.Models;

namespace RPA.App.Views;

public partial class ShellCommandEditor : UserControl
{
    public ShellCommandEditor()
    {
        InitializeComponent();
    }

    private void AddCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellCommandEditorViewModel vm)
        {
            vm.AddCommand();
        }
    }

    private void RemoveCommandButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellCommandEditorViewModel vm)
        {
            vm.RemoveCommand();
        }
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        // Bind DefaultDelayMs from TextBox to ViewModel
        if (FindName("DefaultDelayInput") is TextBox defaultDelayBox)
        {
            defaultDelayBox.TextChanged += (s, e) =>
            {
                if (DataContext is ShellCommandEditorViewModel vm && int.TryParse(defaultDelayBox.Text, out int delay))
                {
                    vm.DefaultDelayMs = delay;
                }
            };
        }

        // Bind Commands list to ItemsControl
        if (FindName("CommandsList") is ItemsControl itemsControl && DataContext is ShellCommandEditorViewModel vm2)
        {
            itemsControl.ItemsSource = vm2.Commands;
        }
    }
}
