using System;
using Godot;

namespace MathSurvive.Game;

/// <summary>Дверь — выход из игры: коричневый прямоугольник, стоит низом на заданной точке.</summary>
public partial class DoorView : Node3D
{
    private const float Width = 0.30f;
    private const float Height = 0.56f;

    public event Action? Clicked;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect { get; private set; }

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(Width, Height, 0.04f),
                Material = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.28f, 0.14f) },
            },
            Position = new Vector3(0, Height / 2, 0),
        });
    }

    public void Place(Stage stage, Vector2 feetPx)
    {
        Position = stage.PixelToWorld(feetPx);
        var topLeft = stage.WorldToPixel(Position + new Vector3(-Width / 2, Height, 0));
        var bottomRight = stage.WorldToPixel(Position + new Vector3(Width / 2, 0, 0));
        ScreenRect = new Rect2(topLeft, bottomRight - topLeft);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible || !DesktopOverlay.IsLeftClickIn(e, ScreenRect))
            return;
        GetViewport().SetInputAsHandled();
        Clicked?.Invoke();
    }
}
