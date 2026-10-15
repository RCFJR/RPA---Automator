using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RPA.Core.Models;

namespace RPA.App.ViewModels;

public partial class ShellCommandEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private int defaultDelayMs = 1000;

    [ObservableProperty]
    private ObservableCollection<CommandItem> commands = new();

    [ObservableProperty]
    private string statusMessage = $"Pronto";

    public ShellCommandEditorViewModel()
    {
    }

    public void AddCommand()
    {
        var newCommand = new CommandItem { Command = "", DelayMs = null };
        Commands.Add(newCommand);
        StatusMessage = $"Comando adicionado ({Commands.Count} total)";
    }

    public void RemoveCommand()
    {
        if (Commands.Count > 0)
        {
            Commands.RemoveAt(Commands.Count - 1);
            StatusMessage = $"Comando removido ({Commands.Count} restantes)";
        }
    }

    public ShellCommandStep CreateShellCommandStep(string label = "Shell Command")
    {
        return new ShellCommandStep
        {
            Id = Guid.NewGuid(),
            Label = label,
            DefaultDelayMs = DefaultDelayMs,
            Commands = new List<CommandItem>(Commands)
        };
    }
}
