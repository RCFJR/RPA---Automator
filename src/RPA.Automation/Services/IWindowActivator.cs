namespace RPA.Automation.Services;

public interface IWindowActivator
{
    Task ActivateWindowAsync(string windowTitleContains, CancellationToken ct);
    (int X, int Y) GetWindowPosition(string windowTitleContains);
}
