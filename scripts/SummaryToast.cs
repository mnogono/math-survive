using Godot;
using MathSurvive.Core.Layout;

namespace MathSurvive.Game;

/// <summary>Короткая сводка отбитой орды над персонажем.</summary>
public partial class SummaryToast : PanelContainer
{
    private const int WindowMargin = 8;
    private const float HeadGap = 6f;
    private const int FontSize = 20;

    private static readonly Color Background = new(1f, 0.99f, 0.95f);
    private static readonly Color Border = new(0.30f, 0.30f, 0.36f);
    private static readonly Color Ink = new(0.12f, 0.12f, 0.16f);

    private Label _label = null!;
    private Vector2 _headTop; // логические px
    private bool _relayoutQueued;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect => new(Position * UiScale.Value, Size * UiScale.Value);

    public override void _Ready()
    {
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;
        var box = new StyleBoxFlat { BgColor = Background, BorderColor = Border, AntiAliasing = true };
        box.SetBorderWidthAll(2);
        box.SetCornerRadiusAll(14);
        box.SetContentMarginAll(12);
        AddThemeStyleboxOverride("panel", box);
        _label = new Label { MouseFilter = MouseFilterEnum.Ignore };
        _label.AddThemeFontSizeOverride("font_size", FontSize);
        _label.AddThemeColorOverride("font_color", Ink);
        AddChild(_label);
    }

    public void PlaceAbove(Vector2 headTopPx)
    {
        _headTop = headTopPx / UiScale.Value;
        QueueRelayout();
    }

    public void ShowFor(string text)
    {
        _label.Text = text;
        Visible = true;
        QueueRelayout();
    }

    public void HideToast() => Visible = false;

    private void QueueRelayout()
    {
        if (_relayoutQueued)
            return;
        _relayoutQueued = true;
        Callable.From(Relayout).CallDeferred();
    }

    private void Relayout()
    {
        _relayoutQueued = false;
        if (!Visible)
            return;
        Size = Vector2.Zero;
        var window = (Vector2)GetWindow().Size / UiScale.Value;
        var desired = new PixelRect(
            Mathf.RoundToInt(_headTop.X - Size.X / 2),
            Mathf.RoundToInt(_headTop.Y - HeadGap - Size.Y),
            Mathf.CeilToInt(Size.X),
            Mathf.CeilToInt(Size.Y));
        var placed = QuizLayout.Clamp(desired, new PixelRect(0, 0, (int)window.X, (int)window.Y), WindowMargin);
        Position = new Vector2(placed.X, placed.Y);
    }
}
