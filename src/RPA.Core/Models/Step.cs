using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace RPA.Core.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ActivateWindowStep), "activateWindow")]
[JsonDerivedType(typeof(WaitStep), "wait")]
[JsonDerivedType(typeof(ClickStep), "click")]
[JsonDerivedType(typeof(TypeTextStep), "typeText")]
[JsonDerivedType(typeof(KeyPressStep), "keyPress")]
[JsonDerivedType(typeof(RecordedSequenceStep), "recordedSequence")]
[JsonDerivedType(typeof(ConditionalStep), "conditional")]
[JsonDerivedType(typeof(ShellCommandStep), "shellCommand")]
[JsonDerivedType(typeof(SetVariableStep), "setVariable")]
public abstract record Step : INotifyPropertyChanged
{
    private Guid _id = Guid.NewGuid();
    private string? _label;

    public Guid Id
    {
        get => _id;
        init => _id = value;
    }

    public string? Label
    {
        get => _label;
        set => SetField(ref _label, value);
    }

    public abstract string Summary { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Summary)));
        }
    }

    protected void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public record ActivateWindowStep : Step
{
    private string _windowTitleContains = string.Empty;

    public string WindowTitleContains
    {
        get => _windowTitleContains;
        set => SetField(ref _windowTitleContains, value);
    }

    public override string Summary => $"Ativar janela contendo \"{WindowTitleContains}\"";
}

public record WaitStep : Step
{
    private int _milliseconds;

    public int Milliseconds
    {
        get => _milliseconds;
        set => SetField(ref _milliseconds, value);
    }

    public override string Summary => $"Aguardar {Milliseconds} ms";
}

public record ClickStep : Step
{
    private int _x;
    private int _y;
    private MouseButtonKind _button = MouseButtonKind.Left;
    private ClickKind _kind = ClickKind.Single;
    private CoordinateOrigin _origin = CoordinateOrigin.WindowRelative;

    public int X
    {
        get => _x;
        set => SetField(ref _x, value);
    }

    public int Y
    {
        get => _y;
        set => SetField(ref _y, value);
    }

    public MouseButtonKind Button
    {
        get => _button;
        set => SetField(ref _button, value);
    }

    public ClickKind Kind
    {
        get => _kind;
        set => SetField(ref _kind, value);
    }

    public CoordinateOrigin Origin
    {
        get => _origin;
        set => SetField(ref _origin, value);
    }

    public override string Summary
    {
        get
        {
            var kindText = Kind == ClickKind.Double ? "duplo" : "simples";
            var originText = Origin == CoordinateOrigin.WindowRelative ? "relativo à janela" : "absoluto na tela";
            return $"Clique {kindText} ({Button}) em ({X},{Y}) — {originText}";
        }
    }
}

public record TypeTextStep : Step
{
    private string _template = string.Empty;
    private TypeMode _mode = TypeMode.DirectInput;

    public string Template
    {
        get => _template;
        set => SetField(ref _template, value);
    }

    public TypeMode Mode
    {
        get => _mode;
        set => SetField(ref _mode, value);
    }

    public override string Summary
    {
        get
        {
            var modeText = Mode == TypeMode.DirectInput ? "direto" : "via clipboard";
            var template = Template.Length > 30 ? Template.Substring(0, 30) + "..." : Template;
            return $"Digitar: \"{template}\" ({modeText})";
        }
    }
}

public record KeyPressStep : Step
{
    private List<string> _keys = new();

    public List<string> Keys
    {
        get => _keys;
        set => SetField(ref _keys, value);
    }

    public override string Summary => $"Teclas: {string.Join("+", Keys)}";
}

public record RecordedSequenceStep : Step
{
    public List<Step> RecordedSteps { get; init; } = new();

    public override string Summary => $"Sequência gravada com {RecordedSteps.Count} ações";
}
