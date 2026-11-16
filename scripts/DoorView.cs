using System;
using Godot;

namespace MathSurvive.Game;

/// <summary>
/// Дверь — выход из игры: пиксельный спрайт (<c>assets/door.png</c>), стоит низом на заданной точке.
/// По клику открывается (кадры «приоткрыта» → «открыта») и после этого сообщает <see cref="Opened"/>.
/// </summary>
public partial class DoorView : Node3D
{
    private const string TexturePath = "res://assets/door.png";
    private const int FrameWidth = 16;
    private const int FrameHeight = 32;
    /// <summary>Кадры листа сверху вниз.</summary>
    private const int OpenFrame = 0, AjarFrame = 1, ClosedFrame = 2;
    /// <summary>Пиксель спрайта = 2 логических px (1 мировая единица = 100 логических px).</summary>
    private const float PixelSize = 0.02f;
    private const float Width = FrameWidth * PixelSize;
    private const float Height = FrameHeight * PixelSize;
    private const double AjarSec = 0.12;
    private const double OpenSec = 0.3;

    private Sprite3D _sprite = null!;
    private MeshInstance3D _doorway = null!;
    private double _opening = -1;

    /// <summary>Дверь открылась после клика.</summary>
    public event Action? Opened;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect { get; private set; }

    public override void _Ready()
    {
        // Тёмный проём виден, пока дверь открывается.
        _doorway = new MeshInstance3D
        {
            Mesh = new QuadMesh
            {
                Size = new Vector2(Width - 2 * PixelSize, Height - PixelSize),
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(0.08f, 0.06f, 0.05f),
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
            },
            Position = new Vector3(0, (Height - PixelSize) / 2, -0.01f),
            Visible = false,
        };
        AddChild(_doorway);

        _sprite = new Sprite3D
        {
            Texture = GD.Load<Texture2D>(TexturePath),
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            PixelSize = PixelSize,
            RegionEnabled = true,
            Position = new Vector3(0, Height / 2, 0),
        };
        ShowFrame(ClosedFrame);
        AddChild(_sprite);
    }

    public void Place(Stage stage, Vector2 feetPx)
    {
        Position = stage.PixelToWorld(feetPx);
        var topLeft = stage.WorldToPixel(Position + new Vector3(-Width / 2, Height, 0));
        var bottomRight = stage.WorldToPixel(Position + new Vector3(Width / 2, 0, 0));
        ScreenRect = new Rect2(topLeft, bottomRight - topLeft);
    }

    public override void _Process(double delta)
    {
        if (_opening < 0)
            return;
        _opening += delta;
        ShowFrame(_opening < AjarSec ? AjarFrame : OpenFrame);
        if (_opening < AjarSec + OpenSec)
            return;
        _opening = -1;
        Opened?.Invoke();
    }

    private void ShowFrame(int frame)
        => _sprite.RegionRect = new Rect2(0, frame * FrameHeight, FrameWidth, FrameHeight);

    public override void _UnhandledInput(InputEvent e)
    {
        if (!Visible || !DesktopOverlay.IsLeftClickIn(e, ScreenRect))
            return;
        GetViewport().SetInputAsHandled();
        if (_opening >= 0)
            return;
        _doorway.Visible = true;
        _opening = 0;
    }
}
