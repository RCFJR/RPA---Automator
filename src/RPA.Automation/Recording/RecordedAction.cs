namespace RPA.Automation.Recording;

public record RecordedAction
{
    public DateTime Timestamp { get; init; }
    public ActionType Type { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public string? Text { get; init; }
    public string? Key { get; init; }
}

public enum ActionType
{
    MouseClick,
    MouseDoubleClick,
    MouseRightClick,
    KeyTyped,
    SpecialKey
}
