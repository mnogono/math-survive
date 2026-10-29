using System;
using System.Collections.Generic;

namespace MathSurvive.Core.Layout;

public enum OptionsLayout { Grid2x2, Column }

/// <summary>Раскладка квиз-облачка: вариантов ответа и положения панели в окне.</summary>
public static class QuizLayout
{
    private const int MaxGridTextLength = 24;
    private const int MaxGridPromptLength = 120;

    /// <summary>
    /// <see cref="OptionsLayout.Grid2x2"/>, если условие ≤ 120 символов, а у всех вариантов ширина
    /// ≤ <paramref name="maxGridCellWidth"/> и текст ≤ 24 символов; иначе <see cref="OptionsLayout.Column"/>
    /// (длинная задача — облачко выше, варианты столбиком). Для SVG-варианта textLength = 0.
    /// </summary>
    public static OptionsLayout Choose(IReadOnlyList<(int width, int textLength)> options, int maxGridCellWidth,
        int promptLength)
    {
        if (promptLength > MaxGridPromptLength)
            return OptionsLayout.Column;
        foreach (var (width, textLength) in options)
        {
            if (width > maxGridCellWidth || textLength > MaxGridTextLength)
                return OptionsLayout.Column;
        }
        return OptionsLayout.Grid2x2;
    }

    /// <summary>
    /// Сдвигает панель внутрь окна (<paramref name="window"/> — <c>(0, 0, W, H)</c>) с отступом <paramref name="margin"/>.
    /// Размер не меняется: панель выше (шире) окна прижимается к верхнему (левому) отступу.
    /// </summary>
    public static PixelRect Clamp(PixelRect panel, PixelRect window, int margin)
        => panel with
        {
            X = ClampAxis(panel.X, panel.W, window.X, window.W, margin),
            Y = ClampAxis(panel.Y, panel.H, window.Y, window.H, margin),
        };

    private static int ClampAxis(int pos, int size, int windowPos, int windowSize, int margin)
    {
        var min = windowPos + margin;
        var max = windowPos + windowSize - margin - size;
        return max < min ? min : Math.Clamp(pos, min, max);
    }
}
