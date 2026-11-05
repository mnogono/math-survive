using Godot;

namespace MathSurvive.Game;

/// <summary>Анимированный зомби высотой 56 логических px. Не кликабелен.</summary>
public partial class ZombieView : Node3D
{
    private const float Height = 0.56f;
    private const string RootMotionPath = "rig_CharRoot/Skeleton3D:bip";
    private static readonly string[] VariantNames =
    {
        "female_a", "female_b", "female_c", "female_d", "female_e",
        "male_a", "male_b", "male_c", "male_d", "male_e",
    };

    private AnimationPlayer? _animations;
    private Node3D _fallPivot = null!;
    private bool _atPlayer;
    private bool _dead;

    public const int VariantCount = 10;
    public int Variant { get; set; } = 1;
    public float AnimationPhase { get; set; }
    public float SpeedMultiplier { get; set; } = 1f;

    /// <summary>X ног (физические px окна).</summary>
    public float XPx { get; private set; }

    /// <summary>Дошёл до персонажа и стоит («атакует»).</summary>
    public bool AtPlayer
    {
        get => _atPlayer;
        set
        {
            if (_atPlayer == value)
                return;
            _atPlayer = value;
            if (value)
                _animations?.Stop();
        }
    }

    private static Node3D LoadPolyart(string path)
    {
        var model = GD.Load<PackedScene>(path).Instantiate<Node3D>();
        var mesh = (MeshInstance3D)model.FindChild("body_noesis_meshnode_0000", true, false);
        var bounds = mesh.GetAabb();
        var factor = Height / bounds.Size.Y;
        model.Scale = Vector3.One * factor;
        model.Position = new Vector3(0, -bounds.Position.Y * factor, 0);
        model.RotationDegrees = new Vector3(0, -90, 0);
        return model;
    }

    public override void _Ready()
    {
        var model = LoadPolyart($"res://assets/models/polyart/{VariantNames[Variant - 1]}.glb");
        _fallPivot = new Node3D();
        AddChild(_fallPivot);
        _fallPivot.AddChild(model);
        _animations = ModelVisual.FindAnimationPlayer(model);
        if (_animations != null)
        {
            var walk = (Animation)_animations.GetAnimation("walk").Duplicate();
            for (var i = walk.GetTrackCount() - 1; i >= 0; i--)
                if (walk.TrackGetType(i) == Animation.TrackType.Position3D &&
                    walk.TrackGetPath(i).ToString() == RootMotionPath)
                    walk.RemoveTrack(i);
            _animations.GetAnimationLibrary("").AddAnimation("walk_in_place", walk);
        }
        ModelVisual.Play(_animations, "walk_in_place", loop: true);
        if (_animations != null && AnimationPhase > 0)
            _animations.Seek(_animations.CurrentAnimationLength * Mathf.Clamp(AnimationPhase, 0, 1), true);
    }

    public void Die()
    {
        if (_dead)
            return;
        _dead = true;
        AtPlayer = true;
        _animations?.Stop();
        AddChild(new BloodBurst { Position = new Vector3(0, Height * 0.6f, 0.05f) });
        CreateTween().TweenProperty(_fallPivot, "rotation:z", Mathf.Pi / 2f, 0.4f);
        GetTree().CreateTimer(1.0).Timeout += () =>
        {
            if (IsInstanceValid(this))
                QueueFree();
        };
    }

    public void Place(Stage stage, float xPx, float feetYPx)
    {
        XPx = xPx;
        Position = stage.PixelToWorld(new Vector2(xPx, feetYPx));
    }
}
