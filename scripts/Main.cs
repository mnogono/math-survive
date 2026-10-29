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
    private QuizPanel _quiz = null!;
    private HordeDirector _horde = null!;
    private SummaryToast _toast = null!;
    private StatsPanel _stats = null!;
    private static string SectionDir => PacksRoot.Path + "/math/test";

    public override void _Ready()
    {
        var overlay = GetNode<DesktopOverlay>("DesktopOverlay");
        var stage = GetNode<Stage>("Stage");
        var player = GetNode<PlayerView>("Stage/Player");
        var door = GetNode<DoorView>("Stage/Door");
        _bubble = GetNode<BubbleView>("Ui/Bubble");
        _quiz = GetNode<QuizPanel>("Ui/Quiz");
        _horde = GetNode<HordeDirector>("Stage/Horde");
        _toast = GetNode<SummaryToast>("Ui/Toast");
        _stats = GetNode<StatsPanel>("Ui/Stats");

        _session = new GameSession(BuildConfig(), LoadPack(SectionDir), new Random());
        _session.ModeChanged += OnModeChanged;

        var window = GetWindow().Size;
        var scale = UiScale.Value;
        player.Place(stage, ToVector(Placement.PlayerFeet(window.X, window.Y, scale)));
        door.Place(stage, ToVector(Placement.DoorFeet(window.X, window.Y, scale)));
        _bubble.PlaceAbove(player.HeadTopPx);
        _quiz.PlaceAbove(player.HeadTopPx);
        _toast.PlaceAbove(player.HeadTopPx);
        _stats.PlaceAbove(player.HeadTopPx);
        _horde.Setup(_session, stage, player);
        _session.HordeStarted += _horde.Begin;

        _stats.NewRunClicked += () => _session.StartNewRun();
        door.Clicked += () => GetTree().Quit();
        _bubble.Clicked += () => _session.OpenQuiz(DebugArgs.Current.QuestionId);
        _quiz.OptionChosen += OnOptionChosen;
        _quiz.CollapseClicked += () => _session.CollapseQuiz();
        _quiz.PauseClicked += () => _session.StartPause(DateTime.UtcNow);

        overlay.RegisterHitArea(() => door.ScreenRect);
        overlay.RegisterHitArea(() => player.ScreenRect);
        overlay.RegisterHitArea(() => _bubble.Visible ? _bubble.ScreenRect : null);
        overlay.RegisterHitArea(() => _quiz.Visible ? _quiz.ScreenRect : null);
        overlay.RegisterHitArea(() => _stats.Visible ? _stats.ScreenRect : null);

        ApplyMode(_session.Mode);
        if (DebugArgs.Current.OpenQuiz)
            _session.OpenQuiz(DebugArgs.Current.QuestionId);
        if (DebugArgs.Current.HordeNow)
            _session.ForceHordeNow();
    }

    public override void _Process(double delta)
        => _session.Tick(TimeSpan.FromSeconds(delta), DateTime.UtcNow);

    private void OnModeChanged(GameMode mode)
    {
        GD.Print($"mode: {mode}");
        ApplyMode(mode);
    }

    private void OnOptionChosen(int displayIndex)
    {
        if (_session.Answer(displayIndex) is { } result)
            _quiz.ShowFeedback(result);
    }

    private void ApplyMode(GameMode mode)
    {
        _bubble.Visible = mode == GameMode.Idle;
        // Квиз виден в QuizOpen и Feedback; любой другой режим его прячет: свернули (Idle), пауза (Paused),
        // конец обратной связи (Feedback → Idle), орда (Horde — ModeChanged приходит раньше HordeStarted).
        if (mode == GameMode.QuizOpen)
            _quiz.ShowQuiz(_session.CurrentQuiz!, SectionDir, _session.Pause.Available, _session.Config.PauseDuration);
        else if (mode != GameMode.Feedback)
            _quiz.HideQuiz();
        if (mode == GameMode.HordeSummary)
            _toast.ShowFor($"Орда {_session.CurrentHorde!.Number} отбита, убито {_session.LastHordeKills}");
        else
            _toast.HideToast();
        if (mode == GameMode.GameOver)
            _stats.ShowStats(_session.Run, _session.Lifetime, _session.Combat.Ammo, _session.Pack.Section.Title);
        else
            _stats.HideStats();
        // После поражения зомби стоят у персонажа до новой игры.
        if (mode == GameMode.Idle)
            _horde.Clear();
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
