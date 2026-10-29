using System;
using Godot;
using MathSurvive.Core.Layout;
using MathSurvive.Core.Stats;

namespace MathSurvive.Game;

/// <summary>Экран статистики после game over: над персонажем, с кнопкой «Новая игра».</summary>
public partial class StatsPanel : PanelContainer
{
    private const int WindowMargin = 8;
    private const float HeadGap = 6f;
    private const int FontSize = 18;
    private const int TitleFontSize = 22;

    private static readonly Color Background = new(1f, 0.99f, 0.95f);
    private static readonly Color Border = new(0.30f, 0.30f, 0.36f);
    private static readonly Color Ink = new(0.12f, 0.12f, 0.16f);
    private static readonly Color Muted = new(0.40f, 0.40f, 0.48f);
    private static readonly Color Accent = new(0.32f, 0.48f, 0.85f);

    private ScrollContainer _scroll = null!;
    private VBoxContainer _content = null!;
    private Vector2 _headTop; // логические px
    private bool _relayoutQueued;

    public event Action? NewRunClicked;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect => new(Position * UiScale.Value, Size * UiScale.Value);

    public override void _Ready()
    {
        Visible = false;
        Theme = BuildTheme();
        AddThemeStyleboxOverride("panel", Box(Background, Border, 16, 16f));
        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        AddChild(_scroll);
        _content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 4);
        _content.MinimumSizeChanged += QueueRelayout;
        _scroll.AddChild(_content);
    }

    public void PlaceAbove(Vector2 headTopPx)
    {
        _headTop = headTopPx / UiScale.Value;
        QueueRelayout();
    }

    public void ShowStats(RunStats run, RunStats lifetime, int ammoLeft, string sectionTitle)
    {
        foreach (var child in _content.GetChildren())
        {
            _content.RemoveChild(child);
            child.QueueFree();
        }

        AddLine("Игра окончена", TitleFontSize, Ink);
        AddCounters(run, sectionTitle);
        AddLine($"Патронов потрачено: {run.AmmoSpent}, осталось: {ammoLeft}");
        AddSpacer();
        AddLine("За всё время", TitleFontSize - 2, Accent);
        AddCounters(lifetime, sectionTitle);
        AddLine($"Патронов потрачено: {lifetime.AmmoSpent}");
        AddSpacer();

        var button = new Button { Text = "Новая игра", FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        button.Pressed += () =>
        {
            Visible = false;
            NewRunClicked?.Invoke();
        };
        _content.AddChild(button);

        Visible = true;
        _scroll.ScrollVertical = 0;
        QueueRelayout();
    }

    public void HideStats() => Visible = false;

    private void AddCounters(RunStats s, string sectionTitle)
    {
        AddLine($"Задачи: верно {s.Correct} / неверно {s.Wrong}");
        foreach (var (id, sec) in s.Sections)
        {
            var name = s.Sections.Count == 1 ? sectionTitle : id;
            AddLine($"   {name}: верно {sec.Correct} / неверно {sec.Wrong}, макс. уровень {sec.MaxLevel}", color: Muted);
        }
        var t = s.AverageAnswerTime;
        AddLine($"Среднее время на задачу: {(int)t.TotalMinutes}:{t.Seconds:00}");
        AddLine($"Убито зомби: {s.ZombiesKilled}");
        AddLine($"Отбито орд: {s.HordesSurvived}");
    }

    private void AddLine(string text, int size = FontSize, Color? color = null)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color ?? Ink);
        _content.AddChild(label);
    }

    private void AddSpacer() => _content.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });

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
        var window = (Vector2)GetWindow().Size / UiScale.Value;
        var maxScrollHeight = window.Y - 2 * WindowMargin - GetThemeStylebox("panel").GetMinimumSize().Y;
        var content = _content.GetCombinedMinimumSize();
        var scrollbar = content.Y > maxScrollHeight ? _scroll.GetVScrollBar().GetCombinedMinimumSize().X : 0;
        _scroll.CustomMinimumSize = new Vector2(content.X + scrollbar, Mathf.Min(content.Y, maxScrollHeight));
        Size = Vector2.Zero;

        var desired = new PixelRect(
            Mathf.RoundToInt(_headTop.X - Size.X / 2),
            Mathf.RoundToInt(_headTop.Y - HeadGap - Size.Y),
            Mathf.CeilToInt(Size.X),
            Mathf.CeilToInt(Size.Y));
        var placed = QuizLayout.Clamp(desired, new PixelRect(0, 0, (int)window.X, (int)window.Y), WindowMargin);
        Position = new Vector2(placed.X, placed.Y);
    }

    private static Theme BuildTheme()
    {
        var fill = new Color(0.94f, 0.95f, 0.98f);
        var hover = new Color(0.86f, 0.91f, 1f);
        var theme = new Theme();
        theme.SetFontSize("font_size", "Button", FontSize);
        theme.SetStylebox("normal", "Button", Box(fill, new Color(0.68f, 0.71f, 0.78f), 10, 10f));
        theme.SetStylebox("hover", "Button", Box(hover, Accent, 10, 10f));
        theme.SetStylebox("pressed", "Button", Box(hover.Darkened(0.08f), Accent, 10, 10f));
        theme.SetStylebox("focus", "Button", new StyleBoxEmpty());
        foreach (var color in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" })
            theme.SetColor(color, "Button", Ink);
        return theme;
    }

    private static StyleBoxFlat Box(Color fill, Color border, int radius, float padding)
    {
        var box = new StyleBoxFlat { BgColor = fill, BorderColor = border, AntiAliasing = true };
        box.SetBorderWidthAll(2);
        box.SetCornerRadiusAll(radius);
        box.SetContentMarginAll(padding);
        return box;
    }
}
