using System;
using Godot;

namespace MathSurvive.Game;

/// <summary>Анимированный выживший с ружьём вправо; стоит ногами на заданной точке.</summary>
public partial class PlayerView : Node3D
{
    private const float Height = 0.6f;
    private const float Radius = 0.14f;
    private const float GunLength = 0.48f;
    private const string IdleClip = "holding-both";
    private const string ShotClip = "holding-both-shoot";

    private AnimationPlayer? _animations;
    private Node3D _muzzle = null!;

    public event Action? Clicked;

    /// <summary>Физические px окна.</summary>
    public Rect2 ScreenRect { get; private set; }

    /// <summary>Макушка (физические px окна).</summary>
    public Vector2 HeadTopPx { get; private set; }

    /// <summary>Конец ружья (мировые координаты Stage).</summary>
    public Vector3 MuzzleWorld => _muzzle.GlobalPosition;

    public override void _Ready()
    {
        var model = ModelVisual.Load("res://assets/models/hero.glb", Height, 90);
        AddChild(model);
        _animations = ModelVisual.FindAnimationPlayer(model);
        ModelVisual.Play(_animations, IdleClip, loop: true);

        var gun = ModelVisual.LoadLongest("res://assets/models/shotgun.glb", GunLength / model.Scale.X);
        var hand = (Node3D)model.FindChild("arm-right", true, false);
        hand.AddChild(gun);
        gun.Position = new Vector3(-0.2f, 0, 0.12f);
        _muzzle = new Node3D { Position = new Vector3(0, 0, GunLength / model.Scale.X / 2) };
        gun.AddChild(_muzzle);
        if (_animations != null)
            _animations.AnimationFinished += OnAnimationFinished;
    }

    public void PlayShot() => ModelVisual.Play(_animations, ShotClip);

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

    private void OnAnimationFinished(StringName animationName)
    {
        if (animationName.ToString() == ShotClip)
            ModelVisual.Play(_animations, IdleClip, loop: true);
    }
}
