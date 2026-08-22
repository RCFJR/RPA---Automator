using RPA.Execution.Models;

namespace RPA.Execution.Services;

public interface IExecutionLogExporter
{
    Task ExportToCsvAsync(List<ExecutionResult> results, string filePath);
}
