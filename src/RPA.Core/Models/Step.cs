using System.Text.Json.Serialization;

namespace RPA.Core.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ActivateWindowStep), "activateWindow")]
[JsonDerivedType(typeof(WaitStep), "wait")]
[JsonDerivedType(typeof(ClickStep), "click")]
[JsonDerivedType(typeof(TypeTextStep), "typeText")]
[JsonDerivedType(typeof(KeyPressStep), "keyPress")]
public abstract record Step
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string? Label { get; init; }
}

public record ActivateWindowStep : Step
{
    public string WindowTitleContains { get; init; } = string.Empty;
}

public record WaitStep : Step
{
    public int Milliseconds { get; init; }
}

public record ClickStep : Step
{
    public int X { get; init; }
    public int Y { get; init; }
    public MouseButtonKind Button { get; init; } = MouseButtonKind.Left;
    public ClickKind Kind { get; init; } = ClickKind.Single;
    public CoordinateOrigin Origin { get; init; } = CoordinateOrigin.WindowRelative;
}

public record TypeTextStep : Step
{
    public string Template { get; init; } = string.Empty;
    public TypeMode Mode { get; init; } = TypeMode.DirectInput;
}

public record KeyPressStep : Step
{
    public List<string> Keys { get; init; } = new();
}
