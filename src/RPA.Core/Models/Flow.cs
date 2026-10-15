namespace RPA.Core.Models;

public record Flow
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string? TargetWindowTitle { get; init; }
    public List<Step> Steps { get; init; } = new();
    public ExecutionSource? ExecutionSource { get; init; }
    public Dictionary<string, string> GlobalVariables { get; init; } = new();
    public int SchemaVersion { get; init; } = 1;
}
