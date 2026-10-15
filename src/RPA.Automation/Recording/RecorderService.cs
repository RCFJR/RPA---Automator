using RPA.Automation.Services;
using RPA.Core.Models;
using SharpHook;
using SharpHook.Native;
using System.Text;

namespace RPA.Automation.Recording;

public class RecorderService : IRecorderService
{
    private SimpleGlobalHook? _hook;
    private Task? _hookRunTask;
    private readonly List<Step> _steps = new();
    private readonly StringBuilder _textBuffer = new();
    private readonly HashSet<KeyCode> _heldModifiers = new();
    private readonly object _lock = new();

    private long _lastActionTimestamp;
    private long _textBufferStartTimestamp;
    private KeyCode _suppressNextKeyTypedFor = KeyCode.VcUndefined;

    public bool IsRecording { get; private set; }

    public void Start()
    {
        lock (_lock)
        {
            if (IsRecording) return;

            _steps.Clear();
            _textBuffer.Clear();
            _heldModifiers.Clear();
            _suppressNextKeyTypedFor = KeyCode.VcUndefined;
            _lastActionTimestamp = GetTimestampMs();
            _textBufferStartTimestamp = _lastActionTimestamp;

            _hook = new SimpleGlobalHook();
            _hook.MouseClicked += OnMouseClicked;
            _hook.KeyPressed += OnKeyPressed;
            _hook.KeyReleased += OnKeyReleased;
            _hook.KeyTyped += OnKeyTyped;

            try
            {
                _hookRunTask = _hook.RunAsync();
                IsRecording = true;
            }
            catch
            {
                _hook?.Dispose();
                _hook = null;
                _hookRunTask = null;
                throw;
            }
        }
    }

    public async Task<RecordedSequenceStep> StopAsync(CancellationToken ct = default)
    {
        if (!IsRecording) return new RecordedSequenceStep();

        IsRecording = false;
        _hook?.Dispose();
        _hook = null;

        if (_hookRunTask != null)
        {
            try
            {
                await _hookRunTask;
            }
            catch
            {
                // Hook foi disposado, erro é esperado
            }
        }
        _hookRunTask = null;

        lock (_lock)
        {
            FlushTextBuffer();

            // Remove a última ação (clique no botão de parar gravação)
            if (_steps.Count > 0)
            {
                _steps.RemoveAt(_steps.Count - 1);
            }

            var steps = new RecordedSequenceStep
            {
                RecordedSteps = new List<Step>(_steps)
            };
            _steps.Clear();
            return steps;
        }
    }

    private void OnMouseClicked(object? sender, SharpHook.MouseHookEventArgs e)
    {
        lock (_lock)
        {
            if (!IsRecording) return;

            FlushTextBuffer();
            var now = GetTimestampMs();
            MaybeInsertWait(now);

            var button = e.Data.Button switch
            {
                SharpHook.Native.MouseButton.Button1 => MouseButtonKind.Left,
                SharpHook.Native.MouseButton.Button2 => MouseButtonKind.Right,
                SharpHook.Native.MouseButton.Button3 => MouseButtonKind.Middle,
                _ => MouseButtonKind.Left
            };

            var kind = e.Data.Clicks >= 2 ? ClickKind.Double : ClickKind.Single;

            var (x, y, origin) = ResolveCoordinates(e.Data.X, e.Data.Y);

            var clickStep = new ClickStep
            {
                Label = $"Clique {(kind == ClickKind.Double ? "duplo" : "simples")}",
                X = x,
                Y = y,
                Button = button,
                Kind = kind,
                Origin = origin
            };

            _steps.Add(clickStep);
            _lastActionTimestamp = now;
            StepCaptured?.Invoke(this, clickStep);
        }
    }

    private void OnKeyPressed(object? sender, SharpHook.KeyboardHookEventArgs e)
    {
        lock (_lock)
        {
            if (!IsRecording) return;

            var keyCode = e.Data.KeyCode;

            if (IsModifier(keyCode))
            {
                _heldModifiers.Add(keyCode);
                return;
            }

            // Se há modificadores (Ctrl, Alt, Shift) OU é tecla especial, gera KeyPressStep
            if (IsSpecialKey(keyCode) || _heldModifiers.Count > 0)
            {
                FlushTextBuffer();
                var now = GetTimestampMs();
                GenerateKeyPressStep(keyCode, now);
            }
        }
    }

    private void OnKeyReleased(object? sender, SharpHook.KeyboardHookEventArgs e)
    {
        lock (_lock)
        {
            if (!IsRecording) return;

            var keyCode = e.Data.KeyCode;

            if (IsModifier(keyCode))
            {
                _heldModifiers.Remove(keyCode);
            }
        }
    }

    private void OnKeyTyped(object? sender, SharpHook.KeyboardHookEventArgs e)
    {
        lock (_lock)
        {
            if (!IsRecording) return;

            if (e.Data.KeyChar == char.MaxValue) // evento malformado
                return;

            // Supressão para não duplicar KeyTyped que vem de teclas especiais
            if (_suppressNextKeyTypedFor != KeyCode.VcUndefined)
            {
                _suppressNextKeyTypedFor = KeyCode.VcUndefined;
                return;
            }

            if (char.IsControl(e.Data.KeyChar))
                return;

            _textBuffer.Append(e.Data.KeyChar);
            _textBufferStartTimestamp = GetTimestampMs();
        }
    }

    private void GenerateKeyPressStep(KeyCode keyCode, long timestamp)
    {
        MaybeInsertWait(timestamp);

        var keys = new List<string>();

        foreach (var modifier in _heldModifiers.OrderBy(k => k.ToString()))
        {
            var name = KeyCodeNameMap.ToName(modifier);
            if (name != null) keys.Add(name);
        }

        var keyName = KeyCodeNameMap.ToName(keyCode);
        if (keyName != null) keys.Add(keyName);

        if (keys.Count > 0)
        {
            var keyPressStep = new KeyPressStep
            {
                Label = $"Teclas: {string.Join("+", keys)}",
                Keys = keys
            };

            _steps.Add(keyPressStep);
            _lastActionTimestamp = timestamp;
            StepCaptured?.Invoke(this, keyPressStep);
        }

        _suppressNextKeyTypedFor = keyCode;
    }

    private void FlushTextBuffer()
    {
        if (_textBuffer.Length == 0) return;

        var text = _textBuffer.ToString();
        _textBuffer.Clear();

        var typeTextStep = new TypeTextStep
        {
            Label = $"Digitar: {(text.Length > 20 ? text.Substring(0, 20) + "..." : text)}",
            Template = text,
            Mode = TypeMode.DirectInput
        };

        _steps.Add(typeTextStep);
        _lastActionTimestamp = _textBufferStartTimestamp;
        StepCaptured?.Invoke(this, typeTextStep);
    }

    private void MaybeInsertWait(long currentTimestamp)
    {
        var gapMs = currentTimestamp - _lastActionTimestamp;

        if (gapMs >= 150)
        {
            var roundedMs = ((gapMs + 25) / 50) * 50;

            var waitStep = new WaitStep
            {
                Label = $"Aguardar {roundedMs} ms",
                Milliseconds = (int)roundedMs
            };

            _steps.Add(waitStep);
            StepCaptured?.Invoke(this, waitStep);
        }
    }

    private (int X, int Y, CoordinateOrigin Origin) ResolveCoordinates(int screenX, int screenY)
    {
        var hwnd = Win32Helper.GetWindowAtPoint(screenX, screenY);
        if (hwnd == IntPtr.Zero)
            return (screenX, screenY, CoordinateOrigin.Screen);

        var rootHwnd = Win32Helper.GetRootWindow(hwnd);
        if (rootHwnd == IntPtr.Zero)
            return (screenX, screenY, CoordinateOrigin.Screen);

        Win32Helper.GetWindowRectHelper(rootHwnd, out var rect);

        var windowX = screenX - rect.Left;
        var windowY = screenY - rect.Top;

        return (windowX, windowY, CoordinateOrigin.WindowRelative);
    }

    private static bool IsModifier(KeyCode keyCode) => keyCode switch
    {
        KeyCode.VcLeftShift or KeyCode.VcRightShift or
        KeyCode.VcLeftControl or KeyCode.VcRightControl or
        KeyCode.VcLeftAlt or KeyCode.VcRightAlt or
        KeyCode.VcLeftMeta or KeyCode.VcRightMeta => true,
        _ => false
    };

    private static bool IsSpecialKey(KeyCode keyCode) => keyCode switch
    {
        KeyCode.VcEnter or KeyCode.VcNumPadEnter or
        KeyCode.VcTab or KeyCode.VcEscape or
        KeyCode.VcBackspace or KeyCode.VcDelete or
        KeyCode.VcInsert or KeyCode.VcHome or KeyCode.VcEnd or
        KeyCode.VcPageUp or KeyCode.VcPageDown or
        KeyCode.VcUp or KeyCode.VcDown or
        KeyCode.VcLeft or KeyCode.VcRight => true,
        _ => false
    };

    private static long GetTimestampMs() => Environment.TickCount64;

    public event EventHandler<Step>? StepCaptured;
}
