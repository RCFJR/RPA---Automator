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
            try
            {
            Console.WriteLine($"[EXEC] Iniciando loop: {iterationCount} iterações");

            for (int i = 0; i < iterationCount; i++)
            {
                Console.WriteLine($"[EXEC] === Iteração {i + 1}/{iterationCount} ===");
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (_pauseSemaphore == null)
                    {
                        Console.WriteLine($"[EXEC] ERRO: _pauseSemaphore é null!");
                        throw new InvalidOperationException("PauseSemaphore foi descartado");
                    }
                    await _pauseSemaphore.WaitAsync(ct);
                    Console.WriteLine($"[EXEC] Semáforo OK");
                }
                catch (Exception semEx)
                {
                    Console.WriteLine($"[EXEC] ERRO no semáforo: {semEx}");
                    throw;
                }

                var result = new ExecutionResult
                {
                    RowIndex = i,
                    StartedAt = DateTime.UtcNow
                };

                try
                {
                    Console.WriteLine($"[EXEC] Obtendo rowValues...");
                    IReadOnlyDictionary<string, string> rowValues;
                    try
                    {
                        rowValues = flow.ExecutionSource.GetRowAt(i);
                        Console.WriteLine($"[EXEC] RowValues obtido OK");
                    }
                    catch (Exception rowEx)
                    {
                        Console.WriteLine($"[EXEC] ERRO ao obter rowValues: {rowEx}");
                        throw;
                    }

                    var globalVariables = new Dictionary<string, string>(flow.GlobalVariables);

                    var iterationLabel = flow.ExecutionSource is RepeatExecutionSource
                        ? $"Repetição {i + 1}"
                        : $"Linha {i + 1}";

                    Console.WriteLine($"[EXEC] Processando {iterationLabel}");
                    Console.WriteLine($"[EXEC] Total de steps: {flow.Steps.Count}");
                    Console.WriteLine($"[EXEC] Variáveis globais: {string.Join(", ", globalVariables.Keys)}");

                    progress?.Report(new ExecutionProgress
                    {
                        CurrentRowIndex = i,
                        TotalRows = iterationCount,
                        CurrentStep = $"Processando {iterationLabel}..."
                    });

                    for (int stepIdx = 0; stepIdx < flow.Steps.Count; stepIdx++)
                    {
                        var step = flow.Steps[stepIdx];
                        Console.WriteLine($"[EXEC]   Step {stepIdx}: {step.Label}");
                        ct.ThrowIfCancellationRequested();

                        progress?.Report(new ExecutionProgress
                        {
                            CurrentRowIndex = i,
                            TotalRows = iterationCount,
                            CurrentStep = step.Label ?? step.GetType().Name
                        });

                        try
                        {
                            Console.WriteLine($"[EXEC]   Executando {step.GetType().Name}...");
                            await _stepExecutor.ExecuteAsync(step, rowValues, globalVariables, ct);
                            Console.WriteLine($"[EXEC]   ✓ Step concluído");
                        }
                        catch (Exception stepEx)
                        {
                            Console.WriteLine($"[EXEC]   ✗ Erro no step: {stepEx.Message}");
                            throw;
                        }

                        if (options.StepDelayMs > 0)
                        {
                            Console.WriteLine($"[EXEC]   Aguardando {options.StepDelayMs}ms...");
                            await Task.Delay(options.StepDelayMs, ct);
                        }
                    }

                    result.Success = true;
                    Console.WriteLine($"[EXEC] ✓ Iteração {i + 1} concluída com sucesso");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[EXEC] ✗ Cancelado");
                    throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[EXEC] ✗ Erro na iteração: {ex.Message}");
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
                Console.WriteLine($"[EXEC] ✓ Iteração {i + 1} adicionada ao resultado. Total: {results.Count}/{iterationCount}");

                // Libera o semáforo para a próxima iteração
                _pauseSemaphore?.Release();
                Console.WriteLine($"[EXEC] Semáforo liberado para próxima iteração");
            }

                Console.WriteLine($"[EXEC] Loop concluído! Total de iterações completadas: {results.Count}/{iterationCount}");
            }
            catch (Exception loopEx)
            {
                Console.WriteLine($"[EXEC] *** ERRO NO LOOP: {loopEx.GetType().Name}: {loopEx.Message}");
                Console.WriteLine($"[EXEC] *** Stack: {loopEx.StackTrace}");
                throw;
            }
        }
        finally
        {
            _pauseSemaphore?.Dispose();
            Console.WriteLine($"[EXEC] FINALLY: {results.Count} resultados foram adicionados");
        }

        Console.WriteLine($"[EXEC] Retornando {results.Count} resultados");
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
