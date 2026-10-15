using SharpHook;

namespace RPA.Automation.Recording;

public class PositionCaptureService : IPositionCaptureService
{
    public async Task<(int X, int Y)?> CaptureNextClickAsync(TimeSpan timeout, CancellationToken ct)
    {
        var hook = new SimpleGlobalHook();
        var tcs = new TaskCompletionSource<(int X, int Y)>();

        void OnMouseClicked(object? sender, SharpHook.MouseHookEventArgs e)
        {
            tcs.TrySetResult((e.Data.X, e.Data.Y));
        }

        hook.MouseClicked += OnMouseClicked;

        try
        {
            // Inicia o hook em paralelo
            var runTask = hook.RunAsync();

            // Aguarda o próximo clique com timeout
            var clickTask = tcs.Task;
            var delayTask = Task.Delay(timeout, ct);
            var completedTask = await Task.WhenAny(clickTask, delayTask);

            if (completedTask == clickTask && tcs.Task.IsCompletedSuccessfully)
            {
                return await tcs.Task;
            }

            return null; // Timeout ou cancelamento
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        finally
        {
            hook.MouseClicked -= OnMouseClicked;
            hook.Dispose();
        }
    }
}
