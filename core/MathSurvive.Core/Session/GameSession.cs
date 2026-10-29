using System;
using System.Collections.Generic;
using MathSurvive.Core.Questions;
using MathSurvive.Core.Stats;

namespace MathSurvive.Core.Session;

/// <summary>
/// Машина состояний игры. Вызов в неподходящем режиме ничего не делает.
/// Игровое время (<see cref="Clock"/>) идёт только в <see cref="GameMode.QuizOpen"/>.
/// </summary>
public sealed class GameSession
{
    private readonly Random _rng;
    private readonly QuestionPicker _picker;
    private readonly HashSet<string> _used = new();
    private TimeSpan _questionTime;
    private TimeSpan _modeTime;

    public GameSession(GameConfig config, QuestionPack pack, Random rng)
    {
        Config = config;
        Pack = pack;
        _rng = rng;
        _picker = new QuestionPicker(pack.Questions, rng);
        Difficulty = new DifficultyTracker(config, pack.Section.Levels);
        Combat = new CombatState(config);
        Pause = new PauseState();
        Clock = new GameClock();
        Run = new RunStats();
        Lifetime = new RunStats();
    }

    public GameConfig Config { get; }
    public QuestionPack Pack { get; }
    public GameMode Mode { get; private set; } = GameMode.Idle;
    public QuizView? CurrentQuiz { get; private set; }
    public AnswerResult? LastAnswer { get; private set; }
    public HordeInfo? CurrentHorde { get; private set; }
    public int LastHordeKills { get; private set; }

    /// <summary>Номер следующей/текущей орды, с 1.</summary>
    public int HordeNumber { get; private set; } = 1;

    public CombatState Combat { get; private set; }
    public DifficultyTracker Difficulty { get; }
    public PauseState Pause { get; private set; }
    public GameClock Clock { get; private set; }
    public RunStats Run { get; private set; }
    public RunStats Lifetime { get; }

    public event Action<HordeInfo>? HordeStarted;
    public event Action<GameMode>? ModeChanged;

    /// <param name="forcedQuestionId">Для отладки: берётся, если вопроса ещё нет и id найден.</param>
    public void OpenQuiz(string? forcedQuestionId = null)
    {
        if (Mode != GameMode.Idle) return;
        if (CurrentQuiz == null)
        {
            var forced = forcedQuestionId == null ? null : _picker.Find(forcedQuestionId);
            var question = forced ?? _picker.Next(Difficulty.Level, _used);
            CurrentQuiz = new QuizView(question, ShuffledOrder(question.Options.Count));
            _questionTime = TimeSpan.Zero;
        }
        SetMode(GameMode.QuizOpen);
    }

    public void CollapseQuiz()
    {
        if (Mode != GameMode.QuizOpen) return;
        SetMode(GameMode.Idle);
    }

    public AnswerResult? Answer(int displayIndex)
    {
        if (Mode != GameMode.QuizOpen || CurrentQuiz == null) return null;
        var order = CurrentQuiz.Order;
        if (displayIndex < 0 || displayIndex >= order.Count)
            throw new ArgumentOutOfRangeException(nameof(displayIndex));

        var question = CurrentQuiz.Question;
        var correct = order[displayIndex] == question.Correct;
        var correctDisplay = IndexOf(order, question.Correct);

        Combat.ApplyAnswer(correct);
        Difficulty.RecordAnswer(correct);
        if (correct) Pause.OnCorrectAnswer();
        Run.RecordAnswer(Pack.Section.Id, correct, _questionTime, Difficulty.Level);

        LastAnswer = new AnswerResult(correct, displayIndex, correctDisplay);
        SetMode(GameMode.Feedback);
        return LastAnswer;
    }

    public bool StartPause(DateTime nowUtc)
    {
        if (Mode != GameMode.QuizOpen) return false;
        if (!Pause.TryStart(nowUtc, Config.PauseDuration)) return false;
        SetMode(GameMode.Paused);
        return true;
    }

    public void Tick(TimeSpan delta, DateTime nowUtc)
    {
        switch (Mode)
        {
            case GameMode.QuizOpen:
                Clock.Advance(delta);
                _questionTime += delta;
                StartHordeIfDue();
                break;
            case GameMode.Idle:
                StartHordeIfDue();
                break;
            case GameMode.Feedback:
                _modeTime += delta;
                if (_modeTime >= Config.FeedbackDuration)
                {
                    CurrentQuiz = null;
                    LastAnswer = null;
                    _questionTime = TimeSpan.Zero;
                    SetMode(GameMode.Idle);
                }
                break;
            case GameMode.Paused:
                if (!Pause.IsActive(nowUtc)) SetMode(GameMode.Idle);
                break;
            case GameMode.HordeSummary:
                _modeTime += delta;
                if (_modeTime >= Config.HordeSummaryDuration)
                {
                    CurrentHorde = null;
                    SetMode(GameMode.Idle);
                }
                break;
        }
    }

    public ShotOutcome Fire()
    {
        if (Mode != GameMode.Horde) return ShotOutcome.NoAmmo;
        var outcome = ShotResolver.Fire(Combat, _rng);
        if (outcome != ShotOutcome.NoAmmo) Run.RecordShot();
        return outcome;
    }

    public void FinishHorde(bool survived, int killed)
    {
        if (Mode != GameMode.Horde) return;
        LastHordeKills = killed;
        Run.RecordKills(killed);
        if (survived)
        {
            Run.RecordHordeSurvived();
            Clock.Reset();
            HordeNumber++;
            SetMode(GameMode.HordeSummary);
        }
        else
        {
            Run.MergeInto(Lifetime);
            SetMode(GameMode.GameOver);
        }
    }

    /// <summary>Новый забег после game over; сложность и общая статистика сохраняются.</summary>
    public void StartNewRun()
    {
        if (Mode != GameMode.GameOver) return;
        Combat = new CombatState(Config);
        Pause = new PauseState();
        Clock = new GameClock();
        Run = new RunStats();
        HordeNumber = 1;
        CurrentQuiz = null;
        CurrentHorde = null;
        LastAnswer = null;
        _questionTime = TimeSpan.Zero;
        SetMode(GameMode.Idle);
    }

    /// <summary>Отладка: в Idle/QuizOpen сразу начать орду.</summary>
    public void ForceHordeNow()
    {
        if (Mode != GameMode.Idle && Mode != GameMode.QuizOpen) return;
        StartHorde();
    }

    private void StartHordeIfDue()
    {
        if (Clock.IsHordeDue(Config.HordeInterval)) StartHorde();
    }

    private void StartHorde()
    {
        var horde = new HordeInfo(HordeNumber, HordePlanner.ZombieCount(HordeNumber, Config));
        CurrentHorde = horde;
        SetMode(GameMode.Horde);
        HordeStarted?.Invoke(horde);
    }

    private void SetMode(GameMode mode)
    {
        _modeTime = TimeSpan.Zero;
        if (Mode == mode) return;
        Mode = mode;
        ModeChanged?.Invoke(mode);
    }

    private IReadOnlyList<int> ShuffledOrder(int count)
    {
        var order = new int[count];
        for (var i = 0; i < count; i++) order[i] = i;
        for (var i = count - 1; i > 0; i--)
        {
            var j = _rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return Array.AsReadOnly(order);
    }

    private static int IndexOf(IReadOnlyList<int> order, int optionIndex)
    {
        for (var i = 0; i < order.Count; i++)
            if (order[i] == optionIndex) return i;
        throw new InvalidOperationException("Верный вариант отсутствует в порядке показа");
    }
}
