using System.Runtime.InteropServices;

namespace RPA.Automation.Services;

public class WindowActivatorService : IWindowActivator
{
    public async Task ActivateWindowAsync(string windowTitleContains, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Task.Run(() =>
        {
            var hwnd = FindWindowByTitle(windowTitleContains);

            if (hwnd == IntPtr.Zero)
                throw new InvalidOperationException($"Window containing '{windowTitleContains}' not found.");

            Win32Helper.SetForegroundWindowHelper(hwnd);
            Win32Helper.ShowWindowHelper(hwnd, 9); // SW_RESTORE

            System.Threading.Thread.Sleep(200);
        }, ct);
    }

    public (int X, int Y) GetWindowPosition(string windowTitleContains)
    {
        var hwnd = FindWindowByTitle(windowTitleContains);
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException($"Window containing '{windowTitleContains}' not found.");

        Win32Helper.GetWindowRectHelper(hwnd, out var rect);
        return (rect.Left, rect.Top);
    }

    private IntPtr FindWindowByTitle(string titlePart)
    {
        IntPtr result = IntPtr.Zero;

        Win32Helper.EnumWindowsHelper((hWnd, lParam) =>
        {
            if (Win32Helper.IsWindowVisibleHelper(hWnd))
            {
                var title = Win32Helper.GetWindowTitle(hWnd);
                if (title.Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                {
                    result = hWnd;
                    return false;
                }
            }

            return true;
        }, IntPtr.Zero);

        return result;
    }
}
