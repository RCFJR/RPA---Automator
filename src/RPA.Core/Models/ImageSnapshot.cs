namespace RPA.Core.Models;

public record ImageSnapshot(
    Guid Id,
    string Name,
    byte[] ImageData,
    int Width,
    int Height,
    DateTime CapturedAt);
