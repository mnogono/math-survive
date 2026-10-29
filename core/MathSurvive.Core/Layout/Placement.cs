namespace MathSurvive.Core.Layout;

/// <summary>Прямоугольник в пикселях окна.</summary>
public readonly record struct PixelRect(int X, int Y, int W, int H);

/// <summary>Расстановка элементов в координатах окна (начало — левый верхний угол, px); scale — масштаб интерфейса.</summary>
public static class Placement
{
    private const double PlayerFeetX = 70;
    private const double DoorFeetX = 24;

    public static (double x, double y) PlayerFeet(int windowW, int windowH, double scale)
        => (PlayerFeetX * scale, windowH);

    public static (double x, double y) DoorFeet(int windowW, int windowH, double scale)
        => (DoorFeetX * scale, windowH);

    /// <summary>X, с которого персонаж начинает стрелять: ноги персонажа + доля ширины окна.</summary>
    public static double FireLineX(int windowW, int windowH, double scale, double fireRangeFraction)
        => PlayerFeet(windowW, windowH, scale).x + windowW * fireRangeFraction;
}
