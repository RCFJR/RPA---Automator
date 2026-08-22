namespace RPA.Automation.Recording;

public interface IRecorderService
{
    Task<List<RecordedAction>> RecordAsync(TimeSpan timeout, CancellationToken ct);
    event EventHandler<RecordedAction>? ActionRecorded;
}
