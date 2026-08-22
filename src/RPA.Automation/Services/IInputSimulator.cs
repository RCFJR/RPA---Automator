using RPA.Core.Models;

namespace RPA.Automation.Services;

public interface IInputSimulator
{
    Task ClickAsync(int x, int y, MouseButtonKind button, ClickKind kind, CoordinateOrigin origin, CancellationToken ct);
    Task TypeTextAsync(string text, TypeMode mode, CancellationToken ct);
    Task PressKeysAsync(List<string> keys, CancellationToken ct);
}
