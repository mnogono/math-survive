using System;
using Godot;

namespace MathSurvive.Game;

/// <summary>Персонаж: капсула с «ружьём» вправо; стоит ногами на заданной точке.</summary>
public partial class PlayerView : Node3D
{
    private const float Height = 0.6f;
    private const float Radius = 0.14f;
    private const float GunLength = 0.22f;
    private const float GunThickness = 0.06f;

    public event Action? Clicked;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect { get; private set; }

    /// <summary>Макушка (физические px окна).</summary>
    public Vector2 HeadTopPx { get; private set; }

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Height = Height, Radius = Radius, Material = Material(new Color(0.25f, 0.5f, 0.85f)) },
            Position = new Vector3(0, Height / 2, 0),
        });
        AddChild(new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = new Vector3(GunLength, GunThickness, GunThickness),
                Material = Material(new Color(0.2f, 0.2f, 0.2f)),
            },
            Position = new Vector3(Radius + GunLength / 2 - 0.04f, Height * 0.6f, 0.1f),
        });
    }

    public void Place(Stage stage, Vector2 feetPx)
    {
        Position = stage.PixelToWorld(feetPx);
        var topLeft = stage.WorldToPixel(Position + new Vector3(-Radius, Height, 0));
        var bottomRight = stage.WorldToPixel(Position + new Vector3(Radius + GunLength, 0, 0));
        ScreenRect = new Rect2(topLeft, bottomRight - topLeft);
        HeadTopPx = stage.WorldToPixel(Position + new Vector3(0, Height, 0));
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible || !DesktopOverlay.IsLeftClickIn(e, ScreenRect))
            return;
        GetViewport().SetInputAsHandled();
        Clicked?.Invoke();
    }

    private static StandardMaterial3D Material(Color color) => new() { AlbedoColor = color };
}
