namespace RPA.Core.Models;

public record SetVariableStep : Step
{
    private string _variableName = string.Empty;
    private string _value = string.Empty;

    public string VariableName
    {
        get => _variableName;
        set => SetField(ref _variableName, value);
    }

    public string Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public override string Summary => $"Atribuir ${VariableName} = \"{Value}\"";
}
