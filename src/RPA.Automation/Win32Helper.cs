using System.Runtime.InteropServices;
using System.Text;

namespace RPA.Automation;

[StructLayout(LayoutKind.Sequential)]
public struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public static class Win32Helper
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr WindowFromPoint(int x, int y);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint flags);

    private const uint GA_ROOT = 2;

    public static void EnumWindowsHelper(Func<IntPtr, IntPtr, bool> callback, IntPtr lParam)
    {
        EnumWindowsProc proc = (hWnd, param) => callback(hWnd, param);
        EnumWindows(proc, lParam);
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        var sb = new StringBuilder(260);
        GetWindowText(hWnd, sb, sb.Capacity);
        return sb.ToString();
    }

    public static bool IsWindowVisibleHelper(IntPtr hWnd) => IsWindowVisible(hWnd);

    public static void GetWindowRectHelper(IntPtr hWnd, out Rect rect) => GetWindowRect(hWnd, out rect);

    public static void SetForegroundWindowHelper(IntPtr hWnd) => SetForegroundWindow(hWnd);

    public static void ShowWindowHelper(IntPtr hWnd, int nCmdShow) => ShowWindow(hWnd, nCmdShow);

    public static IntPtr GetForegroundWindowHelper() => GetForegroundWindow();

    public static IntPtr GetWindowAtPoint(int x, int y) => WindowFromPoint(x, y);

    public static IntPtr GetRootWindow(IntPtr hWnd) => GetAncestor(hWnd, GA_ROOT);
}
