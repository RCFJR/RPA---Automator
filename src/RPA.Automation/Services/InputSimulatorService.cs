using RPA.Core.Models;
using SharpHook;
using SharpHook.Native;
using System.Windows.Forms;

namespace RPA.Automation.Services;

public class InputSimulatorService : IInputSimulator
{
    private readonly EventSimulator _simulator = new();

    public async Task ClickAsync(int x, int y, MouseButtonKind button, ClickKind kind, CoordinateOrigin origin, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var actualX = x;
        var actualY = y;

        if (origin == CoordinateOrigin.WindowRelative)
        {
            var (windowX, windowY) = GetActiveWindowPosition();
            actualX = windowX + x;
            actualY = windowY + y;
        }

        var sharpHookButton = button switch
        {
            MouseButtonKind.Left => MouseButton.Button1,
            MouseButtonKind.Right => MouseButton.Button2,
            MouseButtonKind.Middle => MouseButton.Button3,
            _ => MouseButton.Button1
        };

        await Task.Run(() =>
        {
            _simulator.SimulateMouseMovement((short)actualX, (short)actualY);
            Task.Delay(50).Wait();

            if (kind == ClickKind.Double)
            {
                _simulator.SimulateMousePress(sharpHookButton);
                Task.Delay(50).Wait();
                _simulator.SimulateMouseRelease(sharpHookButton);
                Task.Delay(50).Wait();
                _simulator.SimulateMousePress(sharpHookButton);
                Task.Delay(50).Wait();
                _simulator.SimulateMouseRelease(sharpHookButton);
            }
            else
            {
                _simulator.SimulateMousePress(sharpHookButton);
                Task.Delay(50).Wait();
                _simulator.SimulateMouseRelease(sharpHookButton);
            }
        }, ct);
    }

    public async Task TypeTextAsync(string text, TypeMode mode, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (mode == TypeMode.Clipboard)
        {
            await TypeViaClipboardAsync(text, ct);
        }
        else
        {
            await Task.Run(() =>
            {
                _simulator.SimulateTextEntry(text);
            }, ct);
        }
    }

    public async Task PressKeysAsync(List<string> keys, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Task.Run(() =>
        {
            foreach (var key in keys)
            {
                var keyCode = ParseKeyCode(key);
                if (keyCode != KeyCode.VcUndefined)
                {
                    _simulator.SimulateKeyPress(keyCode);
                    Task.Delay(50).Wait();
                    _simulator.SimulateKeyRelease(keyCode);
                }
            }
        }, ct);
    }

    private async Task TypeViaClipboardAsync(string text, CancellationToken ct)
    {
        var originalClipboard = string.Empty;
        try
        {
            if (Clipboard.ContainsText())
            {
                originalClipboard = Clipboard.GetText();
            }

            Clipboard.SetText(text);

            await Task.Run(() =>
            {
                _simulator.SimulateKeyPress(KeyCode.VcLeftControl);
                _simulator.SimulateKeyPress(KeyCode.VcV);
                Task.Delay(50).Wait();
                _simulator.SimulateKeyRelease(KeyCode.VcV);
                _simulator.SimulateKeyRelease(KeyCode.VcLeftControl);
            }, ct);
        }
        finally
        {
            if (!string.IsNullOrEmpty(originalClipboard))
            {
                Clipboard.SetText(originalClipboard);
            }
        }
    }

    private (int X, int Y) GetActiveWindowPosition()
    {
        var hwnd = Win32Helper.GetForegroundWindowHelper();
        if (hwnd == IntPtr.Zero)
            return (0, 0);

        Win32Helper.GetWindowRectHelper(hwnd, out var rect);
        return (rect.Left, rect.Top);
    }

    private KeyCode ParseKeyCode(string key)
    {
        return KeyCodeNameMap.ToKeyCode(key);
    }
}
