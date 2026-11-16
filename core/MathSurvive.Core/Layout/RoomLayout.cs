using System;
using System.Collections.Generic;

namespace MathSurvive.Core.Layout;

/// <summary>Табличка раздела и круглая монета-переключатель справа от неё; <see cref="Coin"/> — квадрат вокруг круга.</summary>
public readonly record struct SectionSlot(PixelRect Plate, PixelRect Coin);

/// <summary>
/// Комната выбора разделов: стена внизу окна, пять колонок по 20 % ширины. В первой стоит персонаж,
/// в остальных над его головой висят разделы. Размеры заданы в логических px и умножаются на scale;
/// результат — физические px окна.
/// </summary>
public static class RoomLayout
{
    public const int SectionCount = 4;

    private const double PlateWidth = 128;
    private const double PlateHeight = 84;
    private const double CoinDiameter = 40;
    private const double CoinGap = 6;
    private const double SlotGap = 8;
    /// <summary>Доля ширины колонки, которую монета может занять в узком окне.</summary>
    private const double MaxCoinShare = 0.3;
    /// <summary>От пола до низа таблички: выше персонажа и облачка «!» над ним.</summary>
    private const double PlateBottom = 108;
    private const double WallHeight = 210;

    /// <summary>Центр колонки по X: 10, 30, 50, 70, 90 % ширины окна.</summary>
    private static double ColumnCenterX(int windowW, int column) => windowW * (0.1 + 0.2 * column);

    public static IReadOnlyList<SectionSlot> SectionSlots(int windowW, int windowH, double scale)
    {
        // В узком окне монета и табличка сужаются, чтобы между соседями оставался зазор.
        var available = windowW * 0.2 - SlotGap * scale;
        var coin = Math.Min(CoinDiameter * scale, available * MaxCoinShare);
        var gap = CoinGap * scale;
        var plateW = Math.Min(PlateWidth * scale, available - gap - coin);
        var plateH = PlateHeight * scale;
        var plateY = windowH - (PlateBottom + PlateHeight) * scale;
        var coinY = plateY + (plateH - coin) / 2;

        var slots = new SectionSlot[SectionCount];
        for (var i = 0; i < SectionCount; i++)
        {
            var left = ColumnCenterX(windowW, i + 1) - (plateW + gap + coin) / 2;
            slots[i] = new SectionSlot(Rect(left, plateY, plateW, plateH), Rect(left + plateW + gap, coinY, coin, coin));
        }
        return slots;
    }

    public static (double x, double y) PlayerFeet(int windowW, int windowH) => (ColumnCenterX(windowW, 0), windowH);

    /// <summary>Верх стены (физические px окна).</summary>
    public static double WallTop(int windowH, double scale) => windowH - WallHeight * scale;

    private static PixelRect Rect(double x, double y, double w, double h)
        => new((int)Math.Round(x), (int)Math.Round(y), (int)Math.Round(w), (int)Math.Round(h));
}
