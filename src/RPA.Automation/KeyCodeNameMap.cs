using SharpHook.Native;

namespace RPA.Automation;

public static class KeyCodeNameMap
{
    private static readonly Dictionary<string, KeyCode> NameToKeyCode = new(StringComparer.OrdinalIgnoreCase)
    {
        // Special keys
        { "Enter", KeyCode.VcEnter },
        { "NumPadEnter", KeyCode.VcNumPadEnter },
        { "Tab", KeyCode.VcTab },
        { "Escape", KeyCode.VcEscape },
        { "Backspace", KeyCode.VcBackspace },
        { "Delete", KeyCode.VcDelete },
        { "Insert", KeyCode.VcInsert },
        { "Home", KeyCode.VcHome },
        { "End", KeyCode.VcEnd },
        { "PageUp", KeyCode.VcPageUp },
        { "PageDown", KeyCode.VcPageDown },
        { "Up", KeyCode.VcUp },
        { "Down", KeyCode.VcDown },
        { "Left", KeyCode.VcLeft },
        { "Right", KeyCode.VcRight },

        // Modifiers
        { "Shift", KeyCode.VcLeftShift },
        { "LeftShift", KeyCode.VcLeftShift },
        { "RightShift", KeyCode.VcRightShift },
        { "Ctrl", KeyCode.VcLeftControl },
        { "Control", KeyCode.VcLeftControl },
        { "LeftControl", KeyCode.VcLeftControl },
        { "RightControl", KeyCode.VcRightControl },
        { "Alt", KeyCode.VcLeftAlt },
        { "LeftAlt", KeyCode.VcLeftAlt },
        { "RightAlt", KeyCode.VcRightAlt },
        { "Meta", KeyCode.VcLeftMeta },
        { "Windows", KeyCode.VcLeftMeta },
        { "LeftMeta", KeyCode.VcLeftMeta },
        { "RightMeta", KeyCode.VcRightMeta },

        // Letters A-Z
        { "A", KeyCode.VcA }, { "B", KeyCode.VcB }, { "C", KeyCode.VcC }, { "D", KeyCode.VcD },
        { "E", KeyCode.VcE }, { "F", KeyCode.VcF }, { "G", KeyCode.VcG }, { "H", KeyCode.VcH },
        { "I", KeyCode.VcI }, { "J", KeyCode.VcJ }, { "K", KeyCode.VcK }, { "L", KeyCode.VcL },
        { "M", KeyCode.VcM }, { "N", KeyCode.VcN }, { "O", KeyCode.VcO }, { "P", KeyCode.VcP },
        { "Q", KeyCode.VcQ }, { "R", KeyCode.VcR }, { "S", KeyCode.VcS }, { "T", KeyCode.VcT },
        { "U", KeyCode.VcU }, { "V", KeyCode.VcV }, { "W", KeyCode.VcW }, { "X", KeyCode.VcX },
        { "Y", KeyCode.VcY }, { "Z", KeyCode.VcZ },

        // Digits 0-9
        { "0", KeyCode.Vc0 }, { "1", KeyCode.Vc1 }, { "2", KeyCode.Vc2 }, { "3", KeyCode.Vc3 },
        { "4", KeyCode.Vc4 }, { "5", KeyCode.Vc5 }, { "6", KeyCode.Vc6 }, { "7", KeyCode.Vc7 },
        { "8", KeyCode.Vc8 }, { "9", KeyCode.Vc9 },
    };

    private static readonly Dictionary<KeyCode, string> KeyCodeToName = BuildReverse();

    private static Dictionary<KeyCode, string> BuildReverse()
    {
        var reverse = new Dictionary<KeyCode, string>();
        foreach (var kvp in NameToKeyCode)
        {
            if (!reverse.ContainsKey(kvp.Value))
                reverse[kvp.Value] = kvp.Key;
        }
        return reverse;
    }

    public static KeyCode ToKeyCode(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return KeyCode.VcUndefined;

        return NameToKeyCode.TryGetValue(name, out var code) ? code : KeyCode.VcUndefined;
    }

    public static string? ToName(KeyCode code)
    {
        return KeyCodeToName.TryGetValue(code, out var name) ? name : null;
    }
}
