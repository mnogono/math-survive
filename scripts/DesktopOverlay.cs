using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

namespace MathSurvive.Game;

/// <summary>
/// Окно поверх рабочего стола: прозрачное, на всю рабочую область основного монитора, без захвата фокуса.
/// Клики вне зарегистрированных зон проходят насквозь (WS_EX_TRANSPARENT переключается по положению курсора),
/// отрисовка всего окна при этом не обрезается.
/// </summary>
public partial class DesktopOverlay : Node
{
    private const int HitTestIntervalMs = 33; // ~30 Гц, не зависит от Engine.MaxFps

    private readonly List<Func<Rect2?>> _areas = new();
    private volatile Rect2[] _visibleAreas = Array.Empty<Rect2>();
    private volatile bool _running;
    private nint _hwnd;

    /// <summary>Зона (физические px окна), где клики принимает игра; null — зона скрыта.</summary>
    public void RegisterHitArea(Func<Rect2?> area) => _areas.Add(area);

    /// <summary>Курсор в физических px окна.</summary>
    public static Vector2 CursorWindowPx()
        => DisplayServer.MouseGetPosition() - DisplayServer.WindowGetPosition();

    /// <summary>Нажатие левой кнопки мыши с курсором внутри rect (физические px окна).</summary>
    public static bool IsLeftClickIn(InputEvent e, Rect2 rect)
        => e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
           && rect.HasPoint(CursorWindowPx());

    public override void _Ready()
    {
        if (!DisplayServer.IsWindowTransparencyAvailable())
            GD.PushError("DesktopOverlay: прозрачность окна недоступна");

        var window = GetWindow();
        var screen = DisplayServer.GetPrimaryScreen();
        var usable = DisplayServer.ScreenGetUsableRect(screen);
        var size = usable.Size;
        if (size == DisplayServer.ScreenGetSize(screen))
            size.Y -= 1; // панель задач скрывается автоматически: окно во весь экран Windows сочтёт полноэкранным

        // NoFocus — до размеров: смена флага пересчитывает стиль окна и сбивает уже выставленный размер.
        DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.NoFocus, true);
        window.Mode = Window.ModeEnum.Windowed;
        window.Position = usable.Position;
        window.Size = size;
        window.ContentScaleFactor = UiScale.Value;

        // Стили Win32 — после всей настройки окна Godot: смена флагов Godot переписывает ex-стиль.
        _hwnd = (nint)DisplayServer.WindowGetNativeHandle(DisplayServer.HandleType.WindowHandle);
        Win32.ApplyExStyle(_hwnd, clickThrough: true);

        _running = true;
        new Thread(HitTestLoop) { IsBackground = true, Name = "DesktopOverlay hit test" }.Start();
    }

    public override void _Process(double delta)
    {
        // Зоны читаются из узлов Godot только в главном потоке; поток проверки видит готовый снимок.
        var visible = new List<Rect2>(_areas.Count);
        foreach (var area in _areas)
        {
            if (area() is { } rect)
                visible.Add(rect);
        }
        _visibleAreas = visible.ToArray();
    }

    public override void _ExitTree() => _running = false;

    private void HitTestLoop()
    {
        while (_running)
        {
            if (Win32.TryGetCursorClientPos(_hwnd, out var x, out var y))
                Win32.ApplyExStyle(_hwnd, clickThrough: !IsInAnyArea(new Vector2(x, y)));
            Thread.Sleep(HitTestIntervalMs);
        }
    }

    private bool IsInAnyArea(Vector2 point)
    {
        foreach (var rect in _visibleAreas)
        {
            if (rect.HasPoint(point))
                return true;
        }
        return false;
    }
}
