using RPA.Core.Models;
using RPA.Execution.Models;

namespace RPA.Execution.Services;

public interface IFlowExecutionEngine
{
    Task<List<ExecutionResult>> RunAsync(
        Flow flow,
        List<CsvRow> rows,
        ExecutionOptions options,
        IProgress<ExecutionProgress>? progress = null,
        CancellationToken ct = default);

    void Pause();
    void Resume();
}

public class ExecutionProgress
{
    public int CurrentRowIndex { get; set; }
    public int TotalRows { get; set; }
    public string CurrentStep { get; set; } = string.Empty;
}
