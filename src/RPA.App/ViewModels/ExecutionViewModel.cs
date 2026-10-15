using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RPA.Automation.Services;
using RPA.Core.Models;
using RPA.Core.Services;
using RPA.Execution.Models;
using RPA.Execution.Services;
using System.Collections.ObjectModel;

namespace RPA.App.ViewModels;

public partial class ExecutionViewModel : ObservableObject
{
    private readonly IFlowExecutionEngine _engine;
    private readonly IStepExecutor _stepExecutor;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private bool isExecuting = false;

    [ObservableProperty]
    private bool isPaused = false;

    [ObservableProperty]
    private int currentRowIndex = 0;

    [ObservableProperty]
    private int totalRows = 0;

    [ObservableProperty]
    private double progressValue = 0;

    [ObservableProperty]
    private string currentStepLabel = "Aguardando...";

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ExecutionLogEntry> executionLog = new();

    [ObservableProperty]
    private int successCount = 0;

    [ObservableProperty]
    private int failureCount = 0;

    public ExecutionViewModel(IFlowExecutionEngine engine, IStepExecutor stepExecutor)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _stepExecutor = stepExecutor ?? throw new ArgumentNullException(nameof(stepExecutor));
    }

    [RelayCommand]
    public async Task Execute(object? parameter)
    {
        if (parameter == null)
        {
            StatusMessage = "Parâmetros inválidos";
            return;
        }

        List<ExecutionResult>? results = null;
        int itemCount = 0;

        var mainWindow = System.Windows.Application.Current.MainWindow;
        if (mainWindow != null)
            mainWindow.WindowState = System.Windows.WindowState.Minimized;

        IsExecuting = true;
        IsPaused = false;
        _cancellationTokenSource = new CancellationTokenSource();
        CurrentRowIndex = 0;
        SuccessCount = 0;
        FailureCount = 0;
        ExecutionLog.Clear();

        try
        {
            StatusMessage = "Executando fluxo...";

            var options = new ExecutionOptions
            {
                OnErrorPolicy = OnErrorPolicy.Continue,
                StepDelayMs = 100
            };

            var progress = new Progress<ExecutionProgress>(p =>
            {
                CurrentRowIndex = p.CurrentRowIndex;
                CurrentStepLabel = p.CurrentStep;
                ProgressValue = (double)p.CurrentRowIndex / p.TotalRows * 100;
            });

            if (parameter is (Flow csvFlow, List<CsvRow> rows))
            {
                if (rows.Count == 0)
                {
                    StatusMessage = "Nenhuma linha para processar";
                    IsExecuting = false;
                    return;
                }

                TotalRows = rows.Count;
                itemCount = rows.Count;
                results = await _engine.RunAsync(csvFlow, rows, options, progress, _cancellationTokenSource.Token);
            }
            else if (parameter is (Flow repeatFlow, int repeatCount))
            {
                if (repeatCount <= 0)
                {
                    StatusMessage = "Quantidade de repetições inválida";
                    IsExecuting = false;
                    return;
                }

                var flowWithRepeat = repeatFlow with { ExecutionSource = new RepeatExecutionSource(repeatCount) };
                TotalRows = repeatCount;
                itemCount = repeatCount;
                results = await _engine.RunAsync(flowWithRepeat, options, progress, _cancellationTokenSource.Token);
            }
            else
            {
                StatusMessage = "Parâmetros inválidos";
                IsExecuting = false;
                return;
            }

            if (results != null)
            {
                // Log dos resultados
                foreach (var result in results)
                {
                    var logEntry = new ExecutionLogEntry
                    {
                        RowIndex = result.RowIndex + 1,
                        Success = result.Success,
                        Message = result.Success ? "Sucesso" : result.ErrorMessage ?? "Erro desconhecido",
                        Duration = result.Duration
                    };

                    ExecutionLog.Add(logEntry);

                    if (result.Success)
                        SuccessCount++;
                    else
                        FailureCount++;
                }

                StatusMessage = $"Execução concluída: {SuccessCount} sucesso(s), {FailureCount} erro(s)";
                ProgressValue = 100;
            }
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Execução cancelada pelo usuário";
        }
        catch (Exception ex)
        {
            StatusMessage = $"❌ Erro durante execução: {ex.Message}";
            System.Windows.MessageBox.Show($"Erro: {ex.Message}\n\n{ex.StackTrace}", "Erro na Execução", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsExecuting = false;
            IsPaused = false;
            _cancellationTokenSource?.Dispose();

            if (StatusMessage.Contains("Sucesso") || StatusMessage.Contains("concluída"))
            {
                System.Windows.MessageBox.Show("✓ Fluxo finalizado com sucesso!", "Execução Completa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
    }

    [RelayCommand]
    public void Pause()
    {
        if (!IsExecuting || IsPaused) return;

        _engine.Pause();
        IsPaused = true;
        StatusMessage = "Execução pausada";
    }

    [RelayCommand]
    public void Resume()
    {
        if (!IsExecuting || !IsPaused) return;

        _engine.Resume();
        IsPaused = false;
        StatusMessage = "Execução retomada";
    }

    [RelayCommand]
    public void Stop()
    {
        if (!IsExecuting) return;

        _cancellationTokenSource?.Cancel();
        StatusMessage = "Parando execução...";
    }
}

public class ExecutionLogEntry
{
    public int RowIndex { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }

    public string DisplayText => $"Linha {RowIndex}: [{(Success ? "✓" : "✗")}] {Message} ({Duration.TotalSeconds:F1}s)";
}
