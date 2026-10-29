using Godot;

namespace MathSurvive.Game;

/// <summary>Масштаб интерфейса по DPI основного монитора (на Windows ScreenGetScale всегда 1.0 — не используется).</summary>
public static class UiScale
{
    private static float? _value;

    public static float Value => _value ??= FromDpi(DisplayServer.ScreenGetDpi(DisplayServer.GetPrimaryScreen()));

    private static float FromDpi(int dpi) => dpi > 0 ? dpi / 96f : 1f;
}
