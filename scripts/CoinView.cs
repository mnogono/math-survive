using Godot;

namespace MathSurvive.Game;

/// <summary>
/// Круглая монета-переключатель раздела: аверс — галочка (раздел включён), реверс — крестик.
/// При попадании переворачивается с подскоком; если переключить нельзя — покачивается на месте.
/// Клики не ловит: попадание определяет <see cref="RoomView"/>.
/// </summary>
public partial class CoinView : Control
{
    private const double FlipSec = 0.45;
    private const double WobbleSec = 0.4;
    private const float HopLogicalPx = 12f;
    private const float WobbleRadians = 0.4f;
    private const float MinFlipScale = 0.06f;
    private const float DisabledAlpha = 0.55f;

    private static readonly Color Rim = new(0.62f, 0.48f, 0.12f);
    private static readonly Color RimLight = new(0.98f, 0.85f, 0.42f);
    private static readonly Color On = new(0.2f, 0.62f, 0.27f);
    private static readonly Color Off = new(0.78f, 0.2f, 0.17f);
    private static readonly Color Mark = new(1f, 0.98f, 0.92f);

    private bool _enabled = true;
    private double _flip = -1;
    private double _wobble = -1;

    public string Id { get; init; } = "";

    /// <summary>Состояние, к которому монета придёт после анимации.</summary>
    public bool IsEnabled => _enabled;

    public bool IsBusy => _flip >= 0 || _wobble >= 0;

    public bool Interactive
    {
        set => Modulate = value ? Colors.White : new Color(1, 1, 1, DisabledAlpha);
    }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetProcess(false);
    }

    /// <param name="screenRect">Квадрат вокруг монеты, физические px окна.</param>
    public void Place(Rect2 screenRect)
    {
        Position = screenRect.Position / UiScale.Value;
        Size = screenRect.Size / UiScale.Value;
        QueueRedraw();
    }

    /// <summary>Точка (физические px окна) внутри круга.</summary>
    public bool HasPoint(Vector2 screenPx)
    {
        var scale = UiScale.Value;
        return screenPx.DistanceTo((Position + Size / 2) * scale) <= Size.X / 2 * scale;
    }

    /// <summary>Центр монеты, физические px окна.</summary>
    public Vector2 CenterPx => (Position + Size / 2) * UiScale.Value;

    /// <summary>Без анимации.</summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        _flip = _wobble = -1;
        SetProcess(false);
        QueueRedraw();
    }

    public void Flip(bool enabled)
    {
        _enabled = enabled;
        _flip = 0;
        _wobble = -1;
        SetProcess(true);
    }

    public void Wobble()
    {
        _wobble = 0;
        _flip = -1;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (_flip >= 0 && (_flip += delta / FlipSec) >= 1)
            _flip = -1;
        if (_wobble >= 0 && (_wobble += delta / WobbleSec) >= 1)
            _wobble = -1;
        if (!IsBusy)
            SetProcess(false);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var face = _enabled;
        var scaleX = 1f;
        var hop = 0f;
        var angle = 0f;
        if (_flip >= 0)
        {
            // Пол-оборота вокруг вертикальной оси: до середины видна старая сторона, после — новая.
            var t = (float)_flip;
            scaleX = Mathf.Max(Mathf.Abs(Mathf.Cos(Mathf.Pi * t)), MinFlipScale);
            face = t < 0.5f ? !_enabled : _enabled;
            hop = -Mathf.Sin(Mathf.Pi * t) * HopLogicalPx;
        }
        if (_wobble >= 0)
        {
            var t = (float)_wobble;
            angle = Mathf.Sin(t * Mathf.Pi * 6) * (1 - t) * WobbleRadians;
        }

        var r = Size.X / 2;
        DrawSetTransform(Size / 2 + new Vector2(0, hop), angle, new Vector2(scaleX, 1));
        DrawCircle(Vector2.Zero, r, Rim, antialiased: true);
        DrawCircle(Vector2.Zero, r * 0.92f, RimLight, antialiased: true);
        DrawCircle(Vector2.Zero, r * 0.78f, face ? On : Off, antialiased: true);
        DrawArc(Vector2.Zero, r * 0.78f, 0, Mathf.Tau, 32, Rim, Mathf.Max(1f, r * 0.06f), antialiased: true);

        var width = Mathf.Max(2f, r * 0.2f);
        if (face)
        {
            DrawPolyline(new[] { new Vector2(-0.4f, 0.02f) * r, new Vector2(-0.12f, 0.3f) * r, new Vector2(0.42f, -0.3f) * r },
                Mark, width, antialiased: true);
        }
        else
        {
            var d = 0.32f * r;
            DrawLine(new Vector2(-d, -d), new Vector2(d, d), Mark, width, antialiased: true);
            DrawLine(new Vector2(-d, d), new Vector2(d, -d), Mark, width, antialiased: true);
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }
}
