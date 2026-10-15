using System.Diagnostics;
using RPA.Automation.Services;
using RPA.Core.Models;
using RPA.Core.Services;

namespace RPA.Execution.Services;

public class StepExecutor : IStepExecutor
{
    private readonly IInputSimulator _inputSimulator;
    private readonly IWindowActivator _windowActivator;
    private readonly IPlaceholderResolver _placeholderResolver;
    private readonly IScreenCaptureService _screenCaptureService;
    private readonly IImageComparisonService _imageComparisonService;
    private readonly ISnapshotRepository _snapshotRepository;

    public StepExecutor(
        IInputSimulator inputSimulator,
        IWindowActivator windowActivator,
        IPlaceholderResolver placeholderResolver,
        IScreenCaptureService screenCaptureService,
        IImageComparisonService imageComparisonService,
        ISnapshotRepository snapshotRepository)
    {
        _inputSimulator = inputSimulator ?? throw new ArgumentNullException(nameof(inputSimulator));
        _windowActivator = windowActivator ?? throw new ArgumentNullException(nameof(windowActivator));
        _placeholderResolver = placeholderResolver ?? throw new ArgumentNullException(nameof(placeholderResolver));
        _screenCaptureService = screenCaptureService ?? throw new ArgumentNullException(nameof(screenCaptureService));
        _imageComparisonService = imageComparisonService ?? throw new ArgumentNullException(nameof(imageComparisonService));
        _snapshotRepository = snapshotRepository ?? throw new ArgumentNullException(nameof(snapshotRepository));
    }

    public async Task ExecuteAsync(Step step, IReadOnlyDictionary<string, string> rowValues, CancellationToken ct)
    {
        await ExecuteAsync(step, rowValues, new Dictionary<string, string>(), ct);
    }

    public async Task ExecuteAsync(Step step, IReadOnlyDictionary<string, string> rowValues, Dictionary<string, string> globalVariables, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        switch (step)
        {
            case ActivateWindowStep activateWindow:
                await ExecuteActivateWindowAsync(activateWindow, ct);
                break;

            case WaitStep wait:
                await ExecuteWaitAsync(wait, ct);
                break;

            case ClickStep click:
                await ExecuteClickAsync(click, ct);
                break;

            case TypeTextStep typeText:
                await ExecuteTypeTextAsync(typeText, rowValues, ct);
                break;

            case KeyPressStep keyPress:
                await ExecuteKeyPressAsync(keyPress, ct);
                break;

            case RecordedSequenceStep sequence:
                await ExecuteRecordedSequenceAsync(sequence, rowValues, ct);
                break;

            case ConditionalStep conditional:
                await ExecuteConditionalStepAsync(conditional, rowValues, ct);
                break;

            case ShellCommandStep shellCommand:
                await ExecuteShellCommandStepAsync(shellCommand, ct);
                break;

            case SetVariableStep setVariable:
                await ExecuteSetVariableStepAsync(setVariable, rowValues, globalVariables, ct);
                break;

            default:
                throw new NotSupportedException($"Step type {step.GetType().Name} is not supported.");
        }
    }

    private async Task ExecuteActivateWindowAsync(ActivateWindowStep step, CancellationToken ct)
    {
        await _windowActivator.ActivateWindowAsync(step.WindowTitleContains, ct);
    }

    private async Task ExecuteWaitAsync(WaitStep step, CancellationToken ct)
    {
        try
        {
            Console.WriteLine($"[EXEC]     Aguardando {step.Milliseconds}ms...");
            await Task.Delay(step.Milliseconds, ct);
            Console.WriteLine($"[EXEC]     Wait concluído");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[EXEC]     Wait cancelado");
            throw;
        }
        catch (Exception waitEx)
        {
            Console.WriteLine($"[EXEC]     ERRO no Wait: {waitEx}");
            throw;
        }
    }

    private async Task ExecuteClickAsync(ClickStep step, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _inputSimulator.ClickAsync(step.X, step.Y, step.Button, step.Kind, step.Origin, ct);
    }

    private async Task ExecuteTypeTextAsync(TypeTextStep step, IReadOnlyDictionary<string, string> rowValues, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            Console.WriteLine($"[EXEC]     Resolvendo template...");
            var resolvedText = _placeholderResolver.Resolve(step.Template, rowValues);
            Console.WriteLine($"[EXEC]     Digitando: {resolvedText}");
            await _inputSimulator.TypeTextAsync(resolvedText, step.Mode, ct);
            Console.WriteLine($"[EXEC]     Texto digitado OK");
        }
        catch (Exception typeEx)
        {
            Console.WriteLine($"[EXEC]     ERRO ao digitar: {typeEx}");
            throw;
        }
    }

    private async Task ExecuteKeyPressAsync(KeyPressStep step, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _inputSimulator.PressKeysAsync(step.Keys, ct);
    }

    private async Task ExecuteRecordedSequenceAsync(RecordedSequenceStep step, IReadOnlyDictionary<string, string> rowValues, CancellationToken ct)
    {
        try
        {
            Console.WriteLine($"[EXEC]   RecordedSequence com {step.RecordedSteps.Count} sub-steps");
            for (int i = 0; i < step.RecordedSteps.Count; i++)
            {
                try
                {
                    var childStep = step.RecordedSteps[i];
                    Console.WriteLine($"[EXEC]     Sub-step {i}: {childStep?.GetType().Name ?? "NULL"}");
                    ct.ThrowIfCancellationRequested();
                    await ExecuteAsync(childStep, rowValues, ct);
                    Console.WriteLine($"[EXEC]     ✓ Sub-step {i} OK");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[EXEC]     Cancelado no sub-step {i}");
                    throw;
                }
                catch (Exception subEx)
                {
                    Console.WriteLine($"[EXEC]     ✗ ERRO no sub-step {i}: {subEx.GetType().Name}: {subEx.Message}");
                    throw;
                }
            }
        }
        catch (Exception seqEx)
        {
            Console.WriteLine($"[EXEC]   ERRO na sequência: {seqEx}");
            throw;
        }
    }

    private async Task ExecuteConditionalStepAsync(ConditionalStep step, IReadOnlyDictionary<string, string> rowValues, CancellationToken ct)
    {
        try
        {
            ct.ThrowIfCancellationRequested();

            if (step.Condition == null)
            {
                throw new InvalidOperationException("ConditionalStep não possui Condition configurada");
            }

            Console.WriteLine($"[EXEC]   ConditionalStep: Capturando região para comparação...");
            var capturedImage = await _screenCaptureService.CaptureRegionAsync(step.Condition.Region);

            Console.WriteLine($"[EXEC]   ConditionalStep: Buscando snapshot de referência ({step.Condition.SnapshotId:N})...");
            var referenceSnapshot = await _snapshotRepository.GetAsync(step.Condition.SnapshotId);

            if (referenceSnapshot == null)
            {
                throw new InvalidOperationException($"Snapshot {step.Condition.SnapshotId} não encontrado");
            }

            Console.WriteLine($"[EXEC]   ConditionalStep: Comparando imagens (threshold: {step.Condition.SimilarityThreshold}%)...");
            var conditionMet = await _imageComparisonService.CompareAsync(
                referenceSnapshot.ImageData,
                capturedImage,
                step.Condition.SimilarityThreshold
            );

            Console.WriteLine($"[EXEC]   ConditionalStep: Resultado = {(conditionMet ? "TRUE" : "FALSE")}");

            var stepsToExecute = conditionMet ? step.ThenSteps : step.ElseSteps;

            if (stepsToExecute != null && stepsToExecute.Count > 0)
            {
                Console.WriteLine($"[EXEC]     Executando {(conditionMet ? "THEN" : "ELSE")} branch com {stepsToExecute.Count} steps");
                for (int i = 0; i < stepsToExecute.Count; i++)
                {
                    try
                    {
                        var substep = stepsToExecute[i];
                        Console.WriteLine($"[EXEC]       Substep {i}: {substep?.GetType().Name ?? "NULL"}");
                        ct.ThrowIfCancellationRequested();
                        await ExecuteAsync(substep, rowValues, ct);
                        Console.WriteLine($"[EXEC]       ✓ Substep {i} OK");
                    }
                    catch (OperationCanceledException)
                    {
                        Console.WriteLine($"[EXEC]       Cancelado no substep {i}");
                        throw;
                    }
                    catch (Exception subEx)
                    {
                        Console.WriteLine($"[EXEC]       ✗ ERRO no substep {i}: {subEx.GetType().Name}: {subEx.Message}");
                        throw;
                    }
                }
            }
            else
            {
                Console.WriteLine($"[EXEC]     Nenhum step para executar ({(conditionMet ? "THEN" : "ELSE")} branch vazio)");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[EXEC]   ConditionalStep cancelado");
            throw;
        }
        catch (Exception condEx)
        {
            Console.WriteLine($"[EXEC]   ERRO no ConditionalStep: {condEx}");
            throw;
        }
    }

    private async Task ExecuteShellCommandStepAsync(ShellCommandStep step, CancellationToken ct)
    {
        Console.WriteLine($"[EXEC]   ShellCommandStep: Executando {step.Commands.Count} comandos");

        try
        {
            foreach (var (index, cmdItem) in step.Commands.Select((cmd, idx) => (idx, cmd)))
            {
                ct.ThrowIfCancellationRequested();

                Console.WriteLine($"[EXEC]     [{index + 1}/{step.Commands.Count}] {cmdItem.Command}");

                try
                {
                    var output = await ExecuteCommandAsync(cmdItem.Command, cmdItem.Shell, ct);

                    if (!string.IsNullOrWhiteSpace(output))
                        Console.WriteLine($"[EXEC]       Output:\n{output}");

                    var delayMs = cmdItem.DelayMs ?? step.DefaultDelayMs;
                    if (delayMs > 0)
                    {
                        Console.WriteLine($"[EXEC]       Aguardando {delayMs}ms antes do próximo comando...");
                        await Task.Delay(delayMs, ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"[EXEC]       Cancelado no comando {index + 1}");
                    throw;
                }
                catch (Exception cmdEx)
                {
                    Console.WriteLine($"[EXEC]       ✗ ERRO: {cmdEx.Message}");
                    throw;
                }
            }

            Console.WriteLine($"[EXEC]   ShellCommandStep concluído com sucesso");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[EXEC]   ShellCommandStep cancelado");
            throw;
        }
        catch (Exception stepEx)
        {
            Console.WriteLine($"[EXEC]   ERRO no ShellCommandStep: {stepEx}");
            throw;
        }
    }

    private async Task<string> ExecuteCommandAsync(string command, ShellType shell, CancellationToken ct)
    {
        var (fileName, arguments) = shell switch
        {
            ShellType.PowerShell => ("powershell.exe", $"-NoProfile -Command \"{command.Replace("\"", "\\\"")}\""),
            ShellType.Bash => ("bash.exe", $"-c \"{command.Replace("\"", "\\\"")}\""),
            _ => ("cmd.exe", $"/c {command}")
        };

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
                throw new InvalidOperationException($"Falha ao iniciar {fileName}");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Comando falhou (exit code {process.ExitCode}): {error}");

            return output;
        }
    }

    private async Task ExecuteSetVariableStepAsync(SetVariableStep step, IReadOnlyDictionary<string, string> rowValues, Dictionary<string, string> globalVariables, CancellationToken ct)
    {
        try
        {
            Console.WriteLine($"[EXEC]   SetVariableStep: Atribuindo ${step.VariableName}");

            var mergedValues = new Dictionary<string, string>(rowValues);
            foreach (var kv in globalVariables)
            {
                mergedValues[kv.Key] = kv.Value;
            }

            var resolvedValue = _placeholderResolver.Resolve(step.Value, mergedValues);

            globalVariables[step.VariableName] = resolvedValue;

            Console.WriteLine($"[EXEC]     ${step.VariableName} = \"{resolvedValue}\"");
            Console.WriteLine($"[EXEC]   SetVariableStep concluído");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EXEC]   ERRO no SetVariableStep: {ex}");
            throw;
        }
    }
}
