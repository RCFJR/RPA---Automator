using RPA.Core.Models;

namespace RPA.App.Helpers;

public static class EnumSource
{
    public static Array MouseButtonKinds => Enum.GetValues(typeof(MouseButtonKind));
    public static Array ClickKinds => Enum.GetValues(typeof(ClickKind));
    public static Array CoordinateOrigins => Enum.GetValues(typeof(CoordinateOrigin));
    public static Array TypeModes => Enum.GetValues(typeof(TypeMode));
}
