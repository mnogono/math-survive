using System;
using System.Collections.Generic;

namespace MathSurvive.Core.Layout;

/// <summary>
/// Комната выбора раздела: стена внизу окна и пять иконок в ряд над головой персонажа.
/// Размеры заданы в логических px и умножаются на scale; результат — физические px окна.
/// </summary>
public static class RoomLayout
{
    public const int IconCount = 5;

    private const double IconWidth = 104;
    private const double IconHeight = 84;
    private const double IconGap = 8;
    /// <summary>От пола до низа иконки: выше персонажа и облачка «!» над ним.</summary>
    private const double IconBottom = 108;
    private const double WallHeight = 210;

    /// <summary>Центр иконки по X в долях ширины окна: 10, 30, 50, 70, 90 %.</summary>
    private static double CenterX(int windowW, int iconIndex) => windowW * (0.1 + 0.2 * iconIndex);

    public static IReadOnlyList<PixelRect> IconRects(int windowW, int windowH, double scale)
    {
        // В узком окне иконка сужается, чтобы между соседями оставался зазор.
        var w = Math.Min(IconWidth * scale, windowW * 0.2 - IconGap * scale);
        var h = IconHeight * scale;
        var y = windowH - (IconBottom + IconHeight) * scale;
        var rects = new PixelRect[IconCount];
        for (var i = 0; i < IconCount; i++)
            rects[i] = new PixelRect((int)Math.Round(CenterX(windowW, i) - w / 2), (int)Math.Round(y), (int)Math.Round(w), (int)Math.Round(h));
        return rects;
    }

    public static (double x, double y) PlayerFeet(int windowW, int windowH, double scale, int iconIndex)
    {
        if (iconIndex is < 0 or >= IconCount)
            throw new ArgumentOutOfRangeException(nameof(iconIndex));
        return (CenterX(windowW, iconIndex), windowH);
    }

    /// <summary>Верх стены (физические px окна).</summary>
    public static double WallTop(int windowH, double scale) => windowH - WallHeight * scale;
}
