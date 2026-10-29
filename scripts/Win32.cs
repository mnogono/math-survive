using System.Runtime.InteropServices;

namespace MathSurvive.Game;

/// <summary>Вызовы Win32 для окна-оверлея. Расширенные стили окна выставляются только здесь, поверх стилей Godot (OR).</summary>
internal static class Win32
{
    private const int GwlExStyle = -20;
    private const long WsExTransparent = 0x00000020;
    private const long WsExLayered = 0x00080000;
    private const uint LwaAlpha = 0x2;

    [StructLayout(LayoutKind.Sequential)]
    private struct Point
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern nint GetWindowLongPtr(nint hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetLayeredWindowAttributes(nint hWnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point p);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ScreenToClient(nint hWnd, ref Point p);

    /// <summary>
    /// Окно — слоистое (WS_EX_LAYERED, непрозрачность 255); clickThrough — WS_EX_TRANSPARENT: клики проходят к окнам под ним.
    /// Остальные биты стиля сохраняются; если ничего не меняется — вызова SetWindowLongPtr нет.
    /// </summary>
    public static void ApplyExStyle(nint hwnd, bool clickThrough)
    {
        long current = GetWindowLongPtr(hwnd, GwlExStyle);
        var wanted = current | WsExLayered;
        wanted = clickThrough ? wanted | WsExTransparent : wanted & ~WsExTransparent;
        if (wanted == current)
            return;

        SetWindowLongPtr(hwnd, GwlExStyle, (nint)wanted);
        // По документации Win32 слоистое окно без атрибутов может не показываться; альфа по пикселям при этом сохраняется (проверено).
        if ((current & WsExLayered) == 0)
            SetLayeredWindowAttributes(hwnd, 0, 255, LwaAlpha);
    }

    /// <summary>Курсор в координатах клиентской области окна (физические px).</summary>
    public static bool TryGetCursorClientPos(nint hwnd, out int x, out int y)
    {
        x = y = 0;
        if (!GetCursorPos(out var p) || !ScreenToClient(hwnd, ref p))
            return false;
        x = p.X;
        y = p.Y;
        return true;
    }
}
