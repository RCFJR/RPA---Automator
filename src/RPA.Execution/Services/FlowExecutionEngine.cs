using RPA.Core.Models;
using RPA.Execution.Models;

namespace RPA.Execution.Services;

public class FlowExecutionEngine : IFlowExecutionEngine
{
    private readonly IStepExecutor _stepExecutor;
    private SemaphoreSlim? _pauseSemaphore;
    private CancellationTokenSource? _pauseCts;

    public FlowExecutionEngine(IStepExecutor stepExecutor)
    {
        _stepExecutor = stepExecutor ?? throw new ArgumentNullException(nameof(stepExecutor));
    }

    public async Task<List<ExecutionResult>> RunAsync(
        Flow flow,
        ExecutionOptions options,
        IProgress<ExecutionProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (flow.ExecutionSource == null)
            throw new InvalidOperationException("Flow deve ter uma ExecutionSource configurada.");

        var results = new List<ExecutionResult>();
        _pauseSemaphore = new SemaphoreSlim(1, 1);
        var iterationCount = flow.ExecutionSource.GetIterationCount();

        try
        {
            for (int i = 0; i < iterationCount; i++)
            {
                ct.ThrowIfCancellationRequested();

                await _pauseSemaphore.WaitAsync(ct);

                var result = new ExecutionResult
                {
                    RowIndex = i,
                    StartedAt = DateTime.UtcNow
                };

                try
                {
                    var rowValues = flow.ExecutionSource.GetRowAt(i);
                    var iterationLabel = flow.ExecutionSource is RepeatExecutionSource
                        ? $"Repetição {i + 1}"
                        : $"Linha {i + 1}";

                    progress?.Report(new ExecutionProgress
                    {
                        CurrentRowIndex = i,
                        TotalRows = iterationCount,
                        CurrentStep = $"Processando {iterationLabel}..."
                    });

                    foreach (var step in flow.Steps)
                    {
                        ct.ThrowIfCancellationRequested();

                        progress?.Report(new ExecutionProgress
                        {
                            CurrentRowIndex = i,
                            TotalRows = iterationCount,
                            CurrentStep = step.Label ?? step.GetType().Name
                        });

                        await _stepExecutor.ExecuteAsync(step, rowValues, ct);

                        if (options.StepDelayMs > 0)
                        {
                            await Task.Delay(options.StepDelayMs, ct);
                        }
                    }

                    result.Success = true;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    result.Success = false;
                    result.ErrorMessage = ex.Message;

                    if (options.OnErrorPolicy == OnErrorPolicy.Abort)
                    {
                        result.FinishedAt = DateTime.UtcNow;
                        results.Add(result);
                        throw;
                    }
                }

                result.FinishedAt = DateTime.UtcNow;
                results.Add(result);
            }
        }
        finally
        {
            _pauseSemaphore?.Dispose();
        }

        return results;
    }

    public async Task<List<ExecutionResult>> RunAsync(
        Flow flow,
        List<CsvRow> rows,
        ExecutionOptions options,
        IProgress<ExecutionProgress>? progress = null,
        CancellationToken ct = default)
    {
        var flowWithCsv = flow with { ExecutionSource = new CsvExecutionSource(rows) };
        return await RunAsync(flowWithCsv, options, progress, ct);
    }

    public void Pause()
    {
        _pauseSemaphore?.Wait();
    }

    public void Resume()
    {
        _pauseSemaphore?.Release();
    }
}
