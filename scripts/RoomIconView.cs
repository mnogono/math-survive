using System;
using Godot;

namespace MathSurvive.Game;

/// <summary>Иконка раздела на стене: символ и подпись. Клик ловится по <see cref="ScreenRect"/>, как у облачка «!».</summary>
public partial class RoomIconView : Control
{
    private const int SymbolFontSize = 30;
    private const int TitleFontSize = 12;
    private const float Padding = 6f;
    private const float DisabledAlpha = 0.55f;

    private static readonly Color Ink = new(0.15f, 0.12f, 0.1f);
    private static readonly Color Selected = new(1f, 0.82f, 0.1f);

    private bool _selected;
    private bool _interactive = true;

    public string Id { get; init; } = "";
    public string Symbol { get; init; } = "";
    public string Title { get; init; } = "";

    public event Action<string>? Clicked;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect => new(Position * UiScale.Value, Size * UiScale.Value);

    /// <summary>Зона клика для <see cref="DesktopOverlay"/>; <c>null</c>, пока иконка неактивна.</summary>
    public Rect2? HitArea() => Visible && _interactive ? ScreenRect : null;

    public bool IsSelected
    {
        get => _selected;
        set { _selected = value; QueueRedraw(); }
    }

    public bool Interactive
    {
        get => _interactive;
        set
        {
            _interactive = value;
            Modulate = value ? Colors.White : new Color(1, 1, 1, DisabledAlpha);
        }
    }

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    /// <param name="screenRect">Физические px окна.</param>
    public void Place(Rect2 screenRect)
    {
        Position = screenRect.Position / UiScale.Value;
        Size = screenRect.Size / UiScale.Value;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var box = new StyleBoxFlat
        {
            BgColor = _selected ? new Color(1f, 0.95f, 0.75f) : new Color(1f, 1f, 1f, 0.92f),
            BorderColor = _selected ? Selected : new Color(0.55f, 0.5f, 0.45f),
            AntiAliasing = true,
        };
        box.SetBorderWidthAll(_selected ? 3 : 1);
        box.SetCornerRadiusAll(8);
        DrawStyleBox(box, new Rect2(Vector2.Zero, Size));

        var font = ThemeDB.FallbackFont;
        var width = Size.X - 2 * Padding;
        var symbolTop = Padding;
        DrawString(font, new Vector2(Padding, symbolTop + font.GetAscent(SymbolFontSize)), Symbol,
            HorizontalAlignment.Center, width, SymbolFontSize, Ink);

        var titleTop = symbolTop + font.GetHeight(SymbolFontSize) + 2;
        DrawMultilineString(font, new Vector2(Padding, titleTop + font.GetAscent(TitleFontSize)), Title,
            HorizontalAlignment.Center, width, TitleFontSize, maxLines: 2, Ink);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (HitArea() is not { } rect || !DesktopOverlay.IsLeftClickIn(e, rect))
            return;
        GetViewport().SetInputAsHandled();
        Clicked?.Invoke(Id);
    }
}
