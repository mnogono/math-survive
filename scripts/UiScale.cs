using Godot;

namespace MathSurvive.Game;

/// <summary>Масштаб интерфейса по DPI основного монитора (на Windows ScreenGetScale всегда 1.0 — не используется).</summary>
public static class UiScale
{
    private static float? _value;

    public static float Value =>
        _value ??= DisplayServer.ScreenGetDpi(DisplayServer.GetPrimaryScreen()) / 96f;
}
