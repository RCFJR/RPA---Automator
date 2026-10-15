using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RPA.Core.Models;

namespace RPA.App.Views;

public partial class ScreenCaptureOverlay : Window
{
    private Point _startPoint;
    private bool _isSelecting;
    public ImageRegion? CapturedRegion { get; private set; }

    public ScreenCaptureOverlay()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _startPoint = e.GetPosition(this);
        _isSelecting = true;
        SelectionRectangle.Visibility = Visibility.Visible;
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting)
            return;

        var currentPoint = e.GetPosition(this);
        var x = (int)Math.Min(_startPoint.X, currentPoint.X);
        var y = (int)Math.Min(_startPoint.Y, currentPoint.Y);
        var width = (int)Math.Abs(currentPoint.X - _startPoint.X);
        var height = (int)Math.Abs(currentPoint.Y - _startPoint.Y);

        SelectionRectangle.Margin = new Thickness(x, y, 0, 0);
        SelectionRectangle.Width = width;
        SelectionRectangle.Height = height;

        CoordinatesText.Text = $"({x}, {y}) - {width}x{height}px";
    }

    private void Window_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting)
            return;

        _isSelecting = false;

        var currentPoint = e.GetPosition(this);
        var x = (int)Math.Min(_startPoint.X, currentPoint.X);
        var y = (int)Math.Min(_startPoint.Y, currentPoint.Y);
        var width = (int)Math.Abs(currentPoint.X - _startPoint.X);
        var height = (int)Math.Abs(currentPoint.Y - _startPoint.Y);

        if (width > 5 && height > 5)
        {
            CapturedRegion = new ImageRegion(x, y, width, height, false);
            DialogResult = true;
            Close();
        }
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }
}
