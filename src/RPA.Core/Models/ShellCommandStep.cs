using System.Text.Json.Serialization;

namespace RPA.Core.Models;

[JsonDerivedType(typeof(ShellCommandStep), "shellCommand")]
public record ShellCommandStep : Step
{
    private int _defaultDelayMs = 1000;
    private List<CommandItem> _commands = new();

    public int DefaultDelayMs
    {
        get => _defaultDelayMs;
        set => SetField(ref _defaultDelayMs, value);
    }

    public List<CommandItem> Commands
    {
        get => _commands;
        set => SetField(ref _commands, value);
    }

    public override string Summary => $"Executar {Commands.Count} comandos shell";
}

public class CommandItem
{
    public CommandItem() { }

    public CommandItem(string command, ShellType shell = ShellType.Cmd, int? delayMs = null)
    {
        Command = command;
        Shell = shell;
        DelayMs = delayMs;
    }

    public string Command { get; set; } = "";
    public ShellType Shell { get; set; } = ShellType.Cmd;
    public int? DelayMs { get; set; }
}
