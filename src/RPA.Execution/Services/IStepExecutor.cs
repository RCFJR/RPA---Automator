using RPA.Core.Models;

namespace RPA.Execution.Services;

public interface IStepExecutor
{
    Task ExecuteAsync(Step step, IReadOnlyDictionary<string, string> rowValues, CancellationToken ct);
    Task ExecuteAsync(Step step, IReadOnlyDictionary<string, string> rowValues, Dictionary<string, string> globalVariables, CancellationToken ct);
}
