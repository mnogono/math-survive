using System;
using System.Collections.Generic;
using Godot;
using MathSurvive.Core.Layout;
using MathSurvive.Core.Session;

namespace MathSurvive.Game;

/// <summary>
/// Квиз-облачко над персонажем (макет «вариант A»): условие (текст + формула), варианты ответа сеткой 2×2 или
/// столбиком, внизу «Пауза», в правом верхнем углу «▾» — свернуть. Хвостик указывает на голову персонажа.
/// Размеры — логические px (холст масштабирован ContentScaleFactor = UiScale).
/// </summary>
public partial class QuizPanel : PanelContainer
{
    private const float PromptMaxWidth = 560f;
    private const float FormulaScale = 1.5f;
    private const float GridCellWidth = 200f; // логические px; в QuizLayout.Choose — физические (×UiScale)
    private const int WindowMargin = 8;
    private const float HeadGap = 6f;         // от кончика хвостика до макушки
    private const float TailHeight = 18f;
    private const float TailHalfWidth = 11f;
    private const int CornerRadius = 16;
    private const int BorderWidth = 2;
    private const int FontSize = 18;
    private const int CollapseFontSize = 22;
    private const float OptionPadding = 10f;

    private static readonly Color Background = new(1f, 0.99f, 0.95f);
    private static readonly Color Border = new(0.30f, 0.30f, 0.36f);
    private static readonly Color Ink = new(0.12f, 0.12f, 0.16f);
    private static readonly Color OptionFill = new(0.94f, 0.95f, 0.98f);
    private static readonly Color OptionBorder = new(0.68f, 0.71f, 0.78f);
    private static readonly Color HoverFill = new(0.86f, 0.91f, 1f);
    private static readonly Color Accent = new(0.32f, 0.48f, 0.85f);
    private static readonly Color CorrectFill = new(0.74f, 0.93f, 0.74f);
    private static readonly Color CorrectBorder = new(0.18f, 0.58f, 0.24f);
    private static readonly Color WrongFill = new(0.99f, 0.78f, 0.78f);
    private static readonly Color WrongBorder = new(0.80f, 0.20f, 0.20f);

    private readonly List<Button> _options = new();
    private ScrollContainer _scroll = null!;
    private VBoxContainer _content = null!;
    private Label _prompt = null!;
    private Button _collapse = null!;
    private TextureRect _formula = null!;
    private GridContainer _grid = null!;
    private Button _pause = null!;
    private Vector2 _headTop; // логические px
    private bool _relayoutQueued;

    public event Action<int>? OptionChosen;
    public event Action? PauseClicked;
    public event Action? CollapseClicked;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect => new(Position * UiScale.Value, Size * UiScale.Value);

    public override void _Ready()
    {
        Visible = false;
        Theme = BuildTheme();
        AddThemeStyleboxOverride("panel", Box(Background, Border, CornerRadius, 14f));

        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        AddChild(_scroll);

        _content = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 10);
        _content.MinimumSizeChanged += QueueRelayout;
        _scroll.AddChild(_content);

        var header = new HBoxContainer();
        header.AddThemeConstantOverride("separation", 8);
        _prompt = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        _collapse = new Button
        {
            Text = "▾",
            Flat = true,
            FocusMode = FocusModeEnum.None,
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
            TooltipText = "Свернуть",
        };
        _collapse.AddThemeFontSizeOverride("font_size", CollapseFontSize);
        _collapse.Pressed += () => CollapseClicked?.Invoke();
        header.AddChild(_prompt);
        header.AddChild(_collapse);
        _content.AddChild(header);

        _formula = new TextureRect { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        _content.AddChild(_formula);

        _grid = new GridContainer { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        _grid.AddThemeConstantOverride("h_separation", 10);
        _grid.AddThemeConstantOverride("v_separation", 10);
        _content.AddChild(_grid);

        _pause = new Button { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
        _pause.Pressed += () => PauseClicked?.Invoke();
        _content.AddChild(_pause);
    }

    /// <summary>Макушка персонажа (физические px окна): облачко встаёт над ней.</summary>
    public void PlaceAbove(Vector2 headTopPx)
    {
        _headTop = headTopPx / UiScale.Value;
        QueueRelayout();
    }

    /// <param name="sectionDir">Папка раздела: пути SVG в вопросе — относительно неё.</param>
    public void ShowQuiz(QuizView view, string sectionDir, bool pauseAvailable, TimeSpan pauseDuration)
    {
        Visible = true;
        var question = view.Question;
        var font = ThemeDB.FallbackFont;

        _prompt.Visible = question.Prompt != null;
        _prompt.Text = question.Prompt ?? "";
        var promptWidth = font.GetStringSize(_prompt.Text, HorizontalAlignment.Left, -1, FontSize).X;
        _prompt.CustomMinimumSize = new Vector2(Mathf.Min(Mathf.Ceil(promptWidth) + 1, PromptMaxWidth), 0);

        _formula.Visible = question.PromptSvg != null;
        _formula.Texture = question.PromptSvg == null
            ? null
            : SvgTextures.LoadUi(sectionDir + "/" + question.PromptSvg, FormulaScale);

        foreach (var old in _options)
        {
            _grid.RemoveChild(old);
            old.QueueFree();
        }
        _options.Clear();

        var measures = new List<(int width, int textLength)>(view.Order.Count);
        var cell = Vector2.Zero; // наибольшее содержимое варианта, логические px
        for (var display = 0; display < view.Order.Count; display++)
        {
            var option = question.Options[view.Order[display]];
            var button = new Button { FocusMode = FocusModeEnum.None };
            Vector2 contentSize;
            if (option.Svg != null)
            {
                button.Icon = SvgTextures.LoadUi(sectionDir + "/" + option.Svg, FormulaScale);
                button.IconAlignment = HorizontalAlignment.Center;
                contentSize = button.Icon.GetSize();
                measures.Add((Mathf.RoundToInt(contentSize.X * UiScale.Value), 0));
            }
            else
            {
                button.Text = option.Text ?? "";
                button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                var textSize = font.GetStringSize(button.Text, HorizontalAlignment.Left, -1, FontSize);
                contentSize = new Vector2(Mathf.Min(Mathf.Ceil(textSize.X) + 1, PromptMaxWidth), textSize.Y);
                measures.Add((Mathf.RoundToInt(textSize.X * UiScale.Value), button.Text.Length));
            }
            cell = cell.Max(contentSize);

            var chosen = display;
            button.Pressed += () => OptionChosen?.Invoke(chosen);
            _options.Add(button);
            _grid.AddChild(button);
        }

        var layout = QuizLayout.Choose(measures, Mathf.RoundToInt(GridCellWidth * UiScale.Value));
        _grid.Columns = layout == OptionsLayout.Grid2x2 ? 2 : 1;
        _grid.SizeFlagsHorizontal = layout == OptionsLayout.Grid2x2 ? SizeFlags.ShrinkCenter : SizeFlags.Fill;
        // Все варианты одного размера; в столбике кнопки ещё и растягиваются на ширину облачка.
        var minButton = cell + new Vector2(OptionPadding, OptionPadding) * 2;
        foreach (var button in _options)
        {
            button.CustomMinimumSize = minButton;
            if (layout == OptionsLayout.Column)
                button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        }

        _pause.Text = $"⏸ Пауза {pauseDuration.TotalMinutes} мин";
        _pause.Disabled = !pauseAvailable;
        _collapse.Disabled = false;

        _scroll.ScrollVertical = 0;
        QueueRelayout();
    }

    /// <summary>Верный вариант — зелёным, выбранный неверный — красным; дальше варианты не нажимаются.</summary>
    public void ShowFeedback(AnswerResult r)
    {
        foreach (var button in _options)
            button.MouseFilter = MouseFilterEnum.Ignore;
        _pause.Disabled = true;
        _collapse.Disabled = true;

        Highlight(_options[r.CorrectDisplay], CorrectFill, CorrectBorder);
        if (!r.Correct)
            Highlight(_options[r.ChosenDisplay], WrongFill, WrongBorder);
    }

    public void HideQuiz() => Visible = false;

    public override void _Draw()
    {
        // Хвостик: от нижнего края облачка к макушке; рисуется поверх нижней рамки, чтобы слиться с фоном.
        var tip = _headTop - Position + new Vector2(0, -HeadGap);
        if (tip.Y <= Size.Y)
            return;
        var minX = CornerRadius + TailHalfWidth;
        var baseX = Mathf.Clamp(tip.X, minX, Mathf.Max(minX, Size.X - minX));
        var left = new Vector2(baseX - TailHalfWidth, Size.Y - BorderWidth);
        var right = new Vector2(baseX + TailHalfWidth, Size.Y - BorderWidth);
        DrawColoredPolygon(new[] { left, right, tip }, Background);
        DrawLine(left, tip, Border, BorderWidth, antialiased: true);
        DrawLine(right, tip, Border, BorderWidth, antialiased: true);
    }

    private void QueueRelayout()
    {
        if (_relayoutQueued)
            return;
        _relayoutQueued = true;
        Callable.From(Relayout).CallDeferred();
    }

    /// <summary>Размер по содержимому (не выше окна — дальше прокрутка), место над головой, затем QuizLayout.Clamp.</summary>
    private void Relayout()
    {
        _relayoutQueued = false;
        if (!Visible)
            return;

        var window = (Vector2)GetWindow().Size / UiScale.Value;
        var panelStyle = GetThemeStylebox("panel");
        var maxScrollHeight = window.Y - 2 * WindowMargin - panelStyle.GetMinimumSize().Y;
        var content = _content.GetCombinedMinimumSize();
        var scrolls = content.Y > maxScrollHeight;
        var scrollbar = scrolls ? _scroll.GetVScrollBar().GetCombinedMinimumSize().X : 0;
        _scroll.CustomMinimumSize = new Vector2(content.X + scrollbar, Mathf.Min(content.Y, maxScrollHeight));
        Size = Vector2.Zero; // сжать до минимального размера содержимого

        var desired = new PixelRect(
            Mathf.RoundToInt(_headTop.X - Size.X / 2),
            Mathf.RoundToInt(_headTop.Y - HeadGap - TailHeight - Size.Y),
            Mathf.CeilToInt(Size.X),
            Mathf.CeilToInt(Size.Y));
        var placed = QuizLayout.Clamp(desired, new PixelRect(0, 0, (int)window.X, (int)window.Y), WindowMargin);
        Position = new Vector2(placed.X, placed.Y);
        QueueRedraw();
    }

    private static void Highlight(Button button, Color fill, Color border)
    {
        var box = Box(fill, border, 10, OptionPadding);
        foreach (var state in new[] { "normal", "hover", "pressed", "hover_pressed", "disabled" })
            button.AddThemeStyleboxOverride(state, box);
    }

    private static Theme BuildTheme()
    {
        var theme = new Theme();
        theme.SetFontSize("font_size", "Label", FontSize);
        theme.SetColor("font_color", "Label", Ink);

        theme.SetFontSize("font_size", "Button", FontSize);
        theme.SetStylebox("normal", "Button", Box(OptionFill, OptionBorder, 10, OptionPadding));
        theme.SetStylebox("hover", "Button", Box(HoverFill, Accent, 10, OptionPadding));
        theme.SetStylebox("pressed", "Button", Box(HoverFill.Darkened(0.08f), Accent, 10, OptionPadding));
        theme.SetStylebox("hover_pressed", "Button", Box(HoverFill.Darkened(0.08f), Accent, 10, OptionPadding));
        theme.SetStylebox("disabled", "Button", Box(OptionFill, OptionBorder.Lightened(0.4f), 10, OptionPadding));
        theme.SetStylebox("focus", "Button", new StyleBoxEmpty());
        foreach (var color in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" })
            theme.SetColor(color, "Button", Ink);
        theme.SetColor("font_disabled_color", "Button", new Color(Ink, 0.35f));
        return theme;
    }

    private static StyleBoxFlat Box(Color fill, Color border, int radius, float padding)
    {
        var box = new StyleBoxFlat
        {
            BgColor = fill,
            BorderColor = border,
            AntiAliasing = true,
        };
        box.SetBorderWidthAll(BorderWidth);
        box.SetCornerRadiusAll(radius);
        box.SetContentMarginAll(padding);
        return box;
    }
}
