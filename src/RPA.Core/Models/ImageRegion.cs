namespace RPA.Core.Models;

public record ImageRegion(
    int X,
    int Y,
    int Width,
    int Height,
    bool IsWindowRelative = false);
