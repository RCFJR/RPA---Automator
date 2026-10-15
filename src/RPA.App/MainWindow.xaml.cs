using System.Windows;
using RPA.App.ViewModels;

namespace RPA.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}