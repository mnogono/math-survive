using Godot;

namespace MathSurvive.Game;

/// <summary>Зомби: зелёная капсула с головой (рост 0.56 ед. = 56 логических px); стоит ногами на заданной точке. Не кликабелен.</summary>
public partial class ZombieView : Node3D
{
    private const float BodyHeight = 0.42f;
    private const float BodyRadius = 0.11f;
    private const float HeadRadius = 0.08f;
    private const float Height = 0.56f;

    /// <summary>X ног (физические px окна).</summary>
    public float XPx { get; private set; }

    /// <summary>Дошёл до персонажа и стоит («атакует»).</summary>
    public bool AtPlayer { get; set; }

    public override void _Ready()
    {
        AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh
            {
                Height = BodyHeight,
                Radius = BodyRadius,
                Material = new StandardMaterial3D { AlbedoColor = new Color(0.3f, 0.6f, 0.25f) },
            },
            Position = new Vector3(0, BodyHeight / 2, 0),
        });
        AddChild(new MeshInstance3D
        {
            Mesh = new SphereMesh
            {
                Radius = HeadRadius,
                Height = HeadRadius * 2,
                Material = new StandardMaterial3D { AlbedoColor = new Color(0.45f, 0.72f, 0.35f) },
            },
            Position = new Vector3(0, Height - HeadRadius, 0),
        });
    }

    public void Place(Stage stage, float xPx, float feetYPx)
    {
        XPx = xPx;
        Position = stage.PixelToWorld(new Vector2(xPx, feetYPx));
    }
}
