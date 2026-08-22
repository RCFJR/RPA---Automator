using System.Text;

namespace RPA.Automation.Recording;

public class RecorderService : IRecorderService
{
    private readonly List<RecordedAction> _actions = new();
    private DateTime _lastActionTime;
    private StringBuilder? _currentTextBuffer;
    private const int TextGroupingTimeoutMs = 1000;

    public event EventHandler<RecordedAction>? ActionRecorded;

    public async Task<List<RecordedAction>> RecordAsync(TimeSpan timeout, CancellationToken ct)
    {
        _actions.Clear();
        _lastActionTime = DateTime.UtcNow;
        _currentTextBuffer = new StringBuilder();

        // Será implementado futuramente com SharpHook completo
        // Por enquanto, retorna lista vazia para não quebrar a compilação
        FlushTextBuffer();

        return _actions;
    }

    public void AddRecordedAction(RecordedAction action)
    {
        var timeSinceLastAction = (DateTime.UtcNow - _lastActionTime).TotalMilliseconds;

        if (timeSinceLastAction > TextGroupingTimeoutMs && action.Type == ActionType.KeyTyped)
        {
            FlushTextBuffer();
        }

        _actions.Add(action);
        ActionRecorded?.Invoke(this, action);
        _lastActionTime = DateTime.UtcNow;
    }

    private void FlushTextBuffer()
    {
        if (_currentTextBuffer != null && _currentTextBuffer.Length > 0)
        {
            var action = new RecordedAction
            {
                Timestamp = _lastActionTime,
                Type = ActionType.KeyTyped,
                Text = _currentTextBuffer.ToString()
            };

            _actions.Add(action);
            ActionRecorded?.Invoke(this, action);
            _currentTextBuffer.Clear();
        }
    }
}
