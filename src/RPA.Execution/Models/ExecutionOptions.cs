namespace RPA.Execution.Models;

public enum OnErrorPolicy
{
    Continue,
    Abort
}

public class ExecutionOptions
{
    public OnErrorPolicy OnErrorPolicy { get; set; } = OnErrorPolicy.Continue;
    public int StepDelayMs { get; set; } = 100;
}
