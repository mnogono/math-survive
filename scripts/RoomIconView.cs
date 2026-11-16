using System.Collections.Generic;
using Godot;

namespace MathSurvive.Game;

/// <summary>
/// Табличка раздела на стене: символ, подпись и следы пуль. Выключенный раздел бледнеет.
/// Клики не ловит: попадания определяет <see cref="RoomView"/>.
/// </summary>
public partial class RoomIconView : Control
{
    private const int SymbolFontSize = 30;
    private const int TitleFontSize = 12;
    private const int MinTitleFontSize = 8;
    private const float Padding = 6f;
    private const float DisabledAlpha = 0.55f;
    private const int MaxHoles = 12;
    private const float HoleRadius = 2.5f;
    private const float ChipRadius = 4.5f;

    private static readonly Color Ink = new(0.15f, 0.12f, 0.1f);
    private static readonly Color OffInk = new(0.15f, 0.12f, 0.1f, 0.4f);
    private static readonly Color Chip = new(0.72f, 0.68f, 0.62f);
    private static readonly Color Hole = new(0.1f, 0.08f, 0.07f);

    /// <summary>Логические px относительно таблички, старые первыми.</summary>
    private readonly Queue<Vector2> _holes = new();
    private bool _sectionEnabled = true;

    public string Id { get; init; } = "";
    public string Symbol { get; init; } = "";
    public string Title { get; init; } = "";

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect => new(Position * UiScale.Value, Size * UiScale.Value);

    public bool SectionEnabled
    {
        set { _sectionEnabled = value; QueueRedraw(); }
    }

    public bool Interactive
    {
        set => Modulate = value ? Colors.White : new Color(1, 1, 1, DisabledAlpha);
    }

    /// <summary>След пули в точке (физические px окна); самые старые исчезают сверх лимита.</summary>
    public void AddHole(Vector2 screenPx)
    {
        _holes.Enqueue(screenPx / UiScale.Value - Position);
        while (_holes.Count > MaxHoles)
            _holes.Dequeue();
        QueueRedraw();
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
            BgColor = _sectionEnabled ? new Color(1f, 0.95f, 0.75f) : new Color(0.9f, 0.88f, 0.85f, 0.85f),
            BorderColor = _sectionEnabled ? new Color(0.62f, 0.48f, 0.12f) : new Color(0.6f, 0.56f, 0.5f),
            AntiAliasing = true,
        };
        box.SetBorderWidthAll(_sectionEnabled ? 2 : 1);
        box.SetCornerRadiusAll(8);
        DrawStyleBox(box, new Rect2(Vector2.Zero, Size));

        var font = ThemeDB.FallbackFont;
        var ink = _sectionEnabled ? Ink : OffInk;
        var width = Size.X - 2 * Padding;
        var symbolTop = Padding;
        DrawString(font, new Vector2(Padding, symbolTop + font.GetAscent(SymbolFontSize)), Symbol,
            HorizontalAlignment.Center, width, SymbolFontSize, ink);

        // В узком окне иконка сужается: шрифт подписи уменьшается, чтобы слова не рвались посередине.
        var titleSize = TitleFontSize;
        while (titleSize > MinTitleFontSize && LongestWordWidth(font, titleSize) > width)
            titleSize--;
        var titleTop = symbolTop + font.GetHeight(SymbolFontSize) + 2;
        DrawMultilineString(font, new Vector2(Padding, titleTop + font.GetAscent(titleSize)), Title,
            HorizontalAlignment.Center, width, titleSize, maxLines: 2, ink);

        foreach (var hole in _holes)
        {
            DrawCircle(hole, ChipRadius, Chip, antialiased: true);
            DrawCircle(hole, HoleRadius, Hole, antialiased: true);
        }
    }

    private float LongestWordWidth(Font font, int fontSize)
    {
        var max = 0f;
        foreach (var word in Title.Split(' '))
            max = Mathf.Max(max, font.GetStringSize(word, HorizontalAlignment.Left, -1, fontSize).X);
        return max;
    }
}
