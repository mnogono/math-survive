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
    private const double AimShotIntervalSec = 0.25;
    private const double TracerSec = 0.07;
    private const float TracerThickness = 0.012f;
    /// <summary>След пули и вспышка — перед персонажем.</summary>
    private const float EffectZ = 0.3f;

    private AnimationPlayer? _animations;
    private Node3D _muzzle = null!;
    private MeshInstance3D _tracer = null!;
    private MeshInstance3D _flash = null!;
    private Stage? _stage;
    private double _aimCooldown, _effectLeft;

    public event Action? Clicked;

    /// <summary>Ноги (физические px окна).</summary>
    public Vector2 FeetPx { get; private set; }

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

        var glow = new StandardMaterial3D
        {
            AlbedoColor = new Color(1f, 0.85f, 0.2f),
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        };
        // Эффекты в мировых координатах: не поворачиваются вместе с персонажем.
        _tracer = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(1, TracerThickness, TracerThickness), Material = glow },
            TopLevel = true,
            Visible = false,
        };
        AddChild(_tracer);
        _flash = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.05f, Height = 0.1f, Material = glow },
            TopLevel = true,
            Visible = false,
        };
        AddChild(_flash);
    }

    public void PlayShot() => ModelVisual.Play(_animations, ShotClip);

    /// <summary>Поставить персонажа (ноги в физических px окна).</summary>
    public void Place(Stage stage, Vector2 feetPx)
    {
        _stage = stage;
        FeetPx = feetPx;
        Position = stage.PixelToWorld(feetPx);
        var topLeft = stage.WorldToPixel(Position + new Vector3(-Radius, Height, 0));
        var bottomRight = stage.WorldToPixel(Position + new Vector3(Radius + GunLength, 0, 0));
        ScreenRect = new Rect2(topLeft, bottomRight - topLeft);
        HeadTopPx = stage.WorldToPixel(Position + new Vector3(0, Height, 0));
    }

    /// <summary>
    /// Выстрел в точку (физические px окна): поворот к цели, вспышка и след пули. Это не боевой выстрел — патроны не тратятся.
    /// </summary>
    /// <returns><c>false</c>, если ружьё ещё не готово к следующему выстрелу.</returns>
    public bool ShootAt(Vector2 targetPx)
    {
        if (_stage == null || _aimCooldown > 0)
            return false;
        _aimCooldown = AimShotIntervalSec;
        RotationDegrees = new Vector3(0, targetPx.X < FeetPx.X ? 180 : 0, 0);
        ModelVisual.Play(_animations, ShotClip);

        var from = MuzzleWorld with { Z = EffectZ };
        var to = _stage.PixelToWorld(targetPx) with { Z = EffectZ };
        var path = to - from;
        var angle = Mathf.Atan2(path.Y, path.X);
        _tracer.GlobalTransform = new Transform3D(
            new Basis(Vector3.Back, angle) * Basis.FromScale(new Vector3(Mathf.Max(path.Length(), 0.01f), 1, 1)),
            (from + to) / 2);
        _flash.GlobalPosition = from;
        _tracer.Visible = _flash.Visible = true;
        _effectLeft = TracerSec;
        return true;
    }

    public override void _Process(double delta)
    {
        _aimCooldown -= delta;
        if (_effectLeft <= 0)
            return;
        _effectLeft -= delta;
        if (_effectLeft <= 0)
            _tracer.Visible = _flash.Visible = false;
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
        if (animationName.ToString() != ShotClip)
            return;
        RotationDegrees = Vector3.Zero;
        ModelVisual.Play(_animations, IdleClip, loop: true);
    }
}
