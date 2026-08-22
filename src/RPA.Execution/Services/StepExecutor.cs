using RPA.Automation.Services;
using RPA.Core.Models;
using RPA.Core.Services;

namespace RPA.Execution.Services;

public class StepExecutor : IStepExecutor
{
    private readonly IInputSimulator _inputSimulator;
    private readonly IWindowActivator _windowActivator;
    private readonly IPlaceholderResolver _placeholderResolver;

    public StepExecutor(
        IInputSimulator inputSimulator,
        IWindowActivator windowActivator,
        IPlaceholderResolver placeholderResolver)
    {
        _inputSimulator = inputSimulator ?? throw new ArgumentNullException(nameof(inputSimulator));
        _windowActivator = windowActivator ?? throw new ArgumentNullException(nameof(windowActivator));
        _placeholderResolver = placeholderResolver ?? throw new ArgumentNullException(nameof(placeholderResolver));
    }

    public async Task ExecuteAsync(Step step, IReadOnlyDictionary<string, string> rowValues, CancellationToken ct)
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
        await Task.Delay(step.Milliseconds, ct);
    }

    private async Task ExecuteClickAsync(ClickStep step, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _inputSimulator.ClickAsync(step.X, step.Y, step.Button, step.Kind, step.Origin, ct);
    }

    private async Task ExecuteTypeTextAsync(TypeTextStep step, IReadOnlyDictionary<string, string> rowValues, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var resolvedText = _placeholderResolver.Resolve(step.Template, rowValues);
        await _inputSimulator.TypeTextAsync(resolvedText, step.Mode, ct);
    }

    private async Task ExecuteKeyPressAsync(KeyPressStep step, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await _inputSimulator.PressKeysAsync(step.Keys, ct);
    }
}
