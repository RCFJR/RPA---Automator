namespace RPA.Execution.Models;

public class ExecutionResult
{
    public int RowIndex { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }

    public TimeSpan Duration => FinishedAt - StartedAt;
}
