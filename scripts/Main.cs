using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MathSurvive.Core;
using MathSurvive.Core.Layout;
using MathSurvive.Core.Questions;
using MathSurvive.Core.Session;

namespace MathSurvive.Game;

public partial class Main : Node3D
{
    /// <summary>Разделы комнаты; это же порядок случайного режима.</summary>
    private static readonly string[] SectionIds = { "arithmetic", "algebra", "geometry", "analysis" };

    /// <summary>Отладочный пакет: подключается, только если <c>--question</c> указывает на его вопрос.</summary>
    private const string DebugSectionId = "test";

    private GameSession _session = null!;
    private bool _started;
    private Stage _stage = null!;
    private PlayerView _player = null!;
    private RoomView _room = null!;
    private BubbleView _bubble = null!;
    private QuizPanel _quiz = null!;
    private HordeDirector _horde = null!;
    private SummaryToast _toast = null!;
    private StatsPanel _stats = null!;
    private IReadOnlyDictionary<string, string> _sectionTitles = null!;

    private static string SectionDir(string id) => PacksRoot.Path + "/math/" + id;

    public override void _Ready()
    {
        var overlay = GetNode<DesktopOverlay>("DesktopOverlay");
        _stage = GetNode<Stage>("Stage");
        _player = GetNode<PlayerView>("Stage/Player");
        _room = GetNode<RoomView>("Stage/Room");
        var door = GetNode<DoorView>("Stage/Door");
        _bubble = GetNode<BubbleView>("Ui/Bubble");
        _quiz = GetNode<QuizPanel>("Ui/Quiz");
        _horde = GetNode<HordeDirector>("Stage/Horde");
        _toast = GetNode<SummaryToast>("Ui/Toast");
        _stats = GetNode<StatsPanel>("Ui/Stats");

        var config = BuildConfig();
        var rng = new Random();
        try
        {
            var packs = LoadPacks();
            _sectionTitles = packs.ToDictionary(p => p.Section.Id, p => p.Section.Title);
            _session = new GameSession(config, new QuestionCatalog(config, packs, SectionIds, rng), rng);
            if (DebugArgs.Current.QuestionId is { } questionId
                && packs.FirstOrDefault(p => p.Questions.Any(q => q.Id == questionId)) is { } owner)
                _session.SelectSection(owner.Section.Id);
        }
        catch (Exception e)
        {
            // Окно оверлея уже полноэкранное, поверх всех и прозрачное для кликов: без выхода его не закрыть.
            GD.PushError($"Не удалось загрузить пакеты вопросов: {e}");
            OS.Alert($"Не удалось загрузить пакеты вопросов.\n\n{e.Message}\n\nКаталог пакетов: {PacksRoot.Path}", "Math Survive");
            GetTree().Quit();
            return;
        }
        _session.ModeChanged += OnModeChanged;

        var window = GetWindow().Size;
        var scale = UiScale.Value;
        _room.Setup(_stage);
        var icon = SelectedIconId();
        _room.SetSelected(icon);
        _player.Place(_stage, _room.PlayerFeetPx(icon));
        door.Place(_stage, ToVector(Placement.DoorFeet(window.X, window.Y, scale)));
        PlaceUi();
        _horde.Setup(_session, _stage, _player);
        _session.HordeStarted += info =>
        {
            _player.FinishWalk();
            _horde.Begin(info);
        };
        var tray = new TrayController();
        AddChild(tray);
        tray.Setup(_session);

        _stats.NewRunClicked += () => _session.StartNewRun();
        door.Clicked += () => GetTree().Quit();
        _bubble.Clicked += () => _session.OpenQuiz(DebugArgs.Current.QuestionId);
        _quiz.OptionChosen += OnOptionChosen;
        _quiz.CollapseClicked += () => _session.CollapseQuiz();
        _quiz.PauseClicked += () => _session.StartPause(DateTime.UtcNow);
        _room.IconClicked += OnIconClicked;
        _player.Arrived += OnArrived;

        overlay.RegisterHitArea(() => door.ScreenRect);
        overlay.RegisterHitArea(() => _player.ScreenRect);
        overlay.RegisterHitArea(() => _bubble.Visible ? _bubble.ScreenRect : null);
        overlay.RegisterHitArea(() => _quiz.Visible ? _quiz.ScreenRect : null);
        overlay.RegisterHitArea(() => _stats.Visible ? _stats.ScreenRect : null);
        foreach (var roomIcon in _room.Icons)
            overlay.RegisterHitArea(roomIcon.HitArea);

        ApplyMode(_session.Mode);
        if (DebugArgs.Current.OpenQuiz)
            _session.OpenQuiz(DebugArgs.Current.QuestionId);
        if (DebugArgs.Current.HordeNow)
            _session.ForceHordeNow();
        if (DebugArgs.Current.SelectIcon is { } select)
            OnIconClicked(select);
        _started = true;
    }

    public override void _Process(double delta)
    {
        if (!_started)
            return;
        _session.Tick(TimeSpan.FromSeconds(Math.Min(delta, _session.Config.MaxFrameDeltaSec)), DateTime.UtcNow);
        if (_player.IsWalking)
            PlaceUi();
    }

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

    /// <summary>Выбор раздела: Core применяет его сразу, персонаж идёт к иконке, по приходу открывается вопрос.</summary>
    private void OnIconClicked(string id)
    {
        if (_player.IsWalking || !_room.HasIcon(id))
            return;
        var changed = id == RoomView.RandomId ? _session.SelectRandom() : _session.SelectSection(id);
        if (!changed)
            return;
        _room.SetSelected(id);
        _player.WalkTo(_stage, _room.PlayerFeetPx(id));
        ApplyMode(_session.Mode);
    }

    private void OnArrived()
    {
        PlaceUi();
        ApplyMode(_session.Mode);
        _session.OpenQuiz();
    }

    private string SelectedIconId()
    {
        var id = _session.SelectedSectionId ?? RoomView.RandomId;
        return _room.HasIcon(id) ? id : RoomView.RandomId;
    }

    private void PlaceUi()
    {
        _bubble.PlaceAbove(_player.HeadTopPx);
        _quiz.PlaceAbove(_player.HeadTopPx);
        _toast.PlaceAbove(_player.HeadTopPx);
        _stats.PlaceAbove(_player.HeadTopPx);
    }

    private void ApplyMode(GameMode mode)
    {
        var walking = _player.IsWalking;
        _bubble.Visible = mode == GameMode.Idle && !walking;
        _room.SetInteractive(_session.CanChangeSection && !walking);
        // Квиз виден в QuizOpen и Feedback; любой другой режим его прячет: свернули (Idle), пауза (Paused),
        // конец обратной связи (Feedback → Idle), орда (Horde — ModeChanged приходит раньше HordeStarted).
        if (mode == GameMode.QuizOpen)
            _quiz.ShowQuiz(_session.CurrentQuiz!, SectionDir(_session.Pack.Section.Id), _session.Pause.Available, _session.Config.PauseDuration);
        else if (mode != GameMode.Feedback)
            _quiz.HideQuiz();
        if (mode == GameMode.HordeSummary)
            _toast.ShowFor($"Орда {_session.CurrentHorde!.Number} отбита, убито {_session.LastHordeKills}");
        else
            _toast.HideToast();
        if (mode == GameMode.GameOver)
            _stats.ShowStats(_session.Run, _session.Lifetime, _session.Combat.Ammo, _sectionTitles);
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

    private static List<QuestionPack> LoadPacks()
    {
        var packs = SectionIds.Select(LoadPack).ToList();
        var questionId = DebugArgs.Current.QuestionId;
        if (questionId != null && !packs.Any(p => p.Questions.Any(q => q.Id == questionId)))
            packs.Add(LoadPack(DebugSectionId));
        return packs;
    }

    private static QuestionPack LoadPack(string id)
    {
        var dir = SectionDir(id);
        return PackLoader.Parse(
            FileAccess.GetFileAsString(dir + "/section.json"),
            FileAccess.GetFileAsString(dir + "/questions.json"));
    }

    private static Vector2 ToVector((double x, double y) p) => new((float)p.x, (float)p.y);
}
