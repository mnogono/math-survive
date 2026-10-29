using System;
using Godot;

namespace MathSurvive.Game;

/// <summary>
/// Жёлтый круг «!» над головой персонажа (2D поверх 3D). Мигает таймером 1 Гц: яркий ↔ бледный
/// (не гаснет полностью — цель клика всегда видна и снимок в любой момент её показывает).
/// </summary>
public partial class BubbleView : Control
{
    private const float Diameter = 28f; // логические px
    private const float Gap = 8f;       // от макушки до круга, логические px
    private const int FontSize = 20;
    private const float DimAlpha = 0.35f;

    public event Action? Clicked;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect => new(Position * UiScale.Value, Size * UiScale.Value);

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore; // клик ловит _UnhandledInput по ScreenRect, как у 3D-элементов
        Size = new Vector2(Diameter, Diameter);

        var blink = new Timer { WaitTime = 1.0, Autostart = true };
        blink.Timeout += () => Modulate = Modulate.A < 1f ? Colors.White : new Color(1, 1, 1, DimAlpha);
        AddChild(blink);
    }

    /// <summary>Над макушкой (физические px окна), с зазором.</summary>
    public void PlaceAbove(Vector2 headTopPx)
    {
        var head = headTopPx / UiScale.Value;
        Position = new Vector2(head.X - Diameter / 2, head.Y - Gap - Diameter);
    }

    public override void _Draw()
    {
        var r = Diameter / 2;
        DrawCircle(new Vector2(r, r), r, new Color(1f, 0.82f, 0.1f));
        DrawCircle(new Vector2(r, r), r, new Color(0.55f, 0.4f, 0f), filled: false, width: 2f, antialiased: true);

        var font = ThemeDB.FallbackFont;
        var baseline = r + (font.GetAscent(FontSize) - font.GetDescent(FontSize)) / 2;
        DrawString(font, new Vector2(0, baseline), "!", HorizontalAlignment.Center, Diameter, FontSize, new Color(0.15f, 0.1f, 0f));
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible || !DesktopOverlay.IsLeftClickIn(e, ScreenRect))
            return;
        GetViewport().SetInputAsHandled();
        Clicked?.Invoke();
    }
}
