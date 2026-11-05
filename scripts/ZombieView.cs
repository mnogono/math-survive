using Godot;

namespace MathSurvive.Game;

/// <summary>Анимированный зомби высотой 56 логических px. Не кликабелен.</summary>
public partial class ZombieView : Node3D
{
    private const float Height = 0.56f;

    private AnimationPlayer? _animations;
    private bool _atPlayer;
    private bool _dead;

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

    public override void _Ready()
    {
        var model = ModelVisual.Load($"res://assets/models/zombie_{Variant}.glb", Height, -90);
        AddChild(model);
        _animations = ModelVisual.FindAnimationPlayer(model);
        ModelVisual.Play(_animations, Variant == 1 ? "Walk" : "walk", loop: true);
        if (_animations != null && AnimationPhase > 0)
            _animations.Seek(_animations.CurrentAnimationLength * Mathf.Clamp(AnimationPhase, 0, 1), true);
    }

    public void Die()
    {
        if (_dead)
            return;
        _dead = true;
        AtPlayer = true;
        ModelVisual.Play(_animations, Variant == 1 ? "Death" : "die");
        AddChild(new BloodBurst { Position = new Vector3(0, Height * 0.6f, 0.05f) });
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
