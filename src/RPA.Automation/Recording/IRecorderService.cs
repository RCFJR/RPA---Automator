using RPA.Core.Models;

namespace RPA.Automation.Recording;

public interface IRecorderService
{
    bool IsRecording { get; }
    void Start();
    Task<RecordedSequenceStep> StopAsync(CancellationToken ct = default);
    event EventHandler<Step>? StepCaptured;
}
