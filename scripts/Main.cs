using System;
using Godot;
using MathSurvive.Core;
using MathSurvive.Core.Layout;
using MathSurvive.Core.Questions;
using MathSurvive.Core.Session;

namespace MathSurvive.Game;

public partial class Main : Node3D
{
    private GameSession _session = null!;
    private BubbleView _bubble = null!;

    public override void _Ready()
    {
        var overlay = GetNode<DesktopOverlay>("DesktopOverlay");
        var stage = GetNode<Stage>("Stage");
        var player = GetNode<PlayerView>("Stage/Player");
        var door = GetNode<DoorView>("Stage/Door");
        _bubble = GetNode<BubbleView>("Ui/Bubble");

        _session = new GameSession(BuildConfig(), LoadPack(PacksRoot.Path + "/math/test"), new Random());
        _session.ModeChanged += OnModeChanged;

        var window = GetWindow().Size;
        var scale = UiScale.Value;
        player.Place(stage, ToVector(Placement.PlayerFeet(window.X, window.Y, scale)));
        door.Place(stage, ToVector(Placement.DoorFeet(window.X, window.Y, scale)));
        _bubble.PlaceAbove(player.HeadTopPx);

        door.Clicked += () => GetTree().Quit();
        _bubble.Clicked += () => _session.OpenQuiz(DebugArgs.Current.QuestionId);

        overlay.RegisterHitArea(() => door.ScreenRect);
        overlay.RegisterHitArea(() => player.ScreenRect);
        overlay.RegisterHitArea(() => _bubble.Visible ? _bubble.ScreenRect : null);

        ApplyMode(_session.Mode);
    }

    public override void _Process(double delta)
        => _session.Tick(TimeSpan.FromSeconds(delta), DateTime.UtcNow);

    private void OnModeChanged(GameMode mode)
    {
        GD.Print($"mode: {mode}");
        ApplyMode(mode);
    }

    private void ApplyMode(GameMode mode)
    {
        _bubble.Visible = mode == GameMode.Idle;
        Engine.MaxFps = mode is GameMode.Horde or GameMode.HordeSummary
            ? _session.Config.ActiveMaxFps
            : _session.Config.IdleMaxFps;
    }

    private static GameConfig BuildConfig()
    {
        var config = GameConfig.Default;
        if (DebugArgs.Current.Ammo is { } ammo)
            config = config with { StartAmmo = ammo };
        return config;
    }

    private static QuestionPack LoadPack(string dir)
        => PackLoader.Parse(
            FileAccess.GetFileAsString(dir + "/section.json"),
            FileAccess.GetFileAsString(dir + "/questions.json"));

    private static Vector2 ToVector((double x, double y) p) => new((float)p.x, (float)p.y);
}
