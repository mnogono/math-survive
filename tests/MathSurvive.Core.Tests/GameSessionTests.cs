using System;
using System.Collections.Generic;
using System.Linq;
using MathSurvive.Core.Questions;
using MathSurvive.Core.Session;
using Xunit;

namespace MathSurvive.Core.Tests;

public class GameSessionTests
{
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Question Q(string id, int level) => new(id, level, "p", null,
        new[] { new QuestionOption("a", null), new QuestionOption("b", null), new QuestionOption("c", null), new QuestionOption("d", null) }, 0);

    private static QuestionPack Pack()
    {
        var questions = Enumerable.Range(1, 10).Select(i => Q($"q{i}", 1))
            .Concat(new[] { Q("q11", 2), Q("q12", 2) })
            .ToList();
        return new QuestionPack(new SectionInfo("test", "Тест", 3), questions);
    }

    private static GameSession Session(GameConfig? config = null, int seed = 1)
        => new(config ?? GameConfig.Default, Pack(), new Random(seed));

    private static int CorrectDisplay(GameSession s)
    {
        var order = s.CurrentQuiz!.Order;
        for (var i = 0; i < order.Count; i++)
            if (order[i] == 0) return i;
        throw new InvalidOperationException("нет верного варианта");
    }

    private static int WrongDisplay(GameSession s) => CorrectDisplay(s) == 0 ? 1 : 0;

    private static AnswerResult? AnswerCorrect(GameSession s) => s.Answer(CorrectDisplay(s));

    /// <summary>Открыть квиз, ответить верно и дождаться конца обратной связи (Idle).</summary>
    private static void SolveOne(GameSession s)
    {
        s.OpenQuiz();
        AnswerCorrect(s);
        s.Tick(s.Config.FeedbackDuration, T0);
    }

    [Fact]
    public void Starts_idle_and_time_does_not_flow()
    {
        var s = Session();
        s.Tick(TimeSpan.FromHours(1), T0);
        Assert.Equal(GameMode.Idle, s.Mode);
        Assert.Equal(TimeSpan.Zero, s.Clock.Elapsed);
    }

    [Fact]
    public void Open_collapse_keeps_same_question()
    {
        var s = Session();
        s.OpenQuiz();
        Assert.Equal(GameMode.QuizOpen, s.Mode);
        var quiz = s.CurrentQuiz;
        Assert.NotNull(quiz);

        s.CollapseQuiz();
        Assert.Equal(GameMode.Idle, s.Mode);
        Assert.Same(quiz, s.CurrentQuiz);

        s.OpenQuiz();
        Assert.Equal(GameMode.QuizOpen, s.Mode);
        Assert.Same(quiz, s.CurrentQuiz);
    }

    [Fact]
    public void Forced_question_is_opened()
    {
        var s = Session();
        s.OpenQuiz("q7");
        Assert.Equal("q7", s.CurrentQuiz!.Question.Id);
    }

    [Fact]
    public void Correct_answer_flow()
    {
        var s = Session();
        s.OpenQuiz();
        var r = AnswerCorrect(s);
        Assert.NotNull(r);
        Assert.True(r!.Correct);
        Assert.Equal(GameMode.Feedback, s.Mode);
        Assert.Equal(33, s.Combat.Ammo);
        Assert.Same(r, s.LastAnswer);

        s.Tick(TimeSpan.FromSeconds(1.5), T0);
        Assert.Equal(GameMode.Idle, s.Mode);
        Assert.Null(s.CurrentQuiz);
        Assert.Null(s.LastAnswer);
    }

    [Fact]
    public void Wrong_answer_reports_correct_display_index()
    {
        var s = Session();
        s.OpenQuiz();
        var wrong = WrongDisplay(s);
        Assert.NotEqual(0, s.CurrentQuiz!.Order[wrong]);

        var r = s.Answer(wrong);
        Assert.NotNull(r);
        Assert.False(r!.Correct);
        Assert.Equal(wrong, r.ChosenDisplay);
        Assert.Equal(0, s.CurrentQuiz!.Order[r.CorrectDisplay]);
        Assert.Equal(0.9, s.Combat.Accuracy, 6);
    }

    [Fact]
    public void Answer_in_feedback_mode_is_ignored()
    {
        var s = Session();
        s.OpenQuiz();
        Assert.NotNull(AnswerCorrect(s));
        Assert.Null(s.Answer(0));
        Assert.Equal(1, s.Run.Correct + s.Run.Wrong);
    }

    [Fact]
    public void Options_are_shuffled_but_consistent()
    {
        var s = Session(seed: 3);
        var orders = new List<IReadOnlyList<int>>();
        for (var i = 0; i < 10; i++)
        {
            s.OpenQuiz();
            orders.Add(s.CurrentQuiz!.Order);
            AnswerCorrect(s);
            s.Tick(s.Config.FeedbackDuration, T0);
        }

        Assert.Contains(orders, o => !o.SequenceEqual(new[] { 0, 1, 2, 3 }));
        Assert.All(orders, o => Assert.Equal(new[] { 0, 1, 2, 3 }, o.OrderBy(x => x)));
    }

    [Fact]
    public void Pause_hides_quiz_until_time_passes_and_is_single_use()
    {
        var s = Session();
        s.OpenQuiz();
        var quiz = s.CurrentQuiz;
        Assert.True(s.StartPause(T0));
        Assert.Equal(GameMode.Paused, s.Mode);
        Assert.Same(quiz, s.CurrentQuiz);

        s.Tick(TimeSpan.FromSeconds(1), T0 + TimeSpan.FromMinutes(9));
        Assert.Equal(GameMode.Paused, s.Mode);
        s.Tick(TimeSpan.FromSeconds(1), T0 + TimeSpan.FromMinutes(10));
        Assert.Equal(GameMode.Idle, s.Mode);

        s.OpenQuiz();
        Assert.Same(quiz, s.CurrentQuiz);
        Assert.False(s.StartPause(T0 + TimeSpan.FromMinutes(11)));
        Assert.Equal(GameMode.QuizOpen, s.Mode);
    }

    [Fact]
    public void Horde_starts_when_game_time_reaches_interval_and_question_is_kept()
    {
        var s = Session(GameConfig.Default with { HordeInterval = TimeSpan.FromMinutes(1) });
        var hordes = new List<HordeInfo>();
        s.HordeStarted += h => hordes.Add(h);

        s.OpenQuiz();
        s.Tick(TimeSpan.FromSeconds(61), T0);

        Assert.Equal(GameMode.Horde, s.Mode);
        Assert.Equal(new[] { new HordeInfo(1, 5) }, hordes);
        Assert.Equal(new HordeInfo(1, 5), s.CurrentHorde);
        Assert.Equal(0, s.Run.Correct + s.Run.Wrong);
        Assert.NotNull(s.CurrentQuiz);
    }

    [Fact]
    public void Survived_horde_returns_to_idle_and_next_horde_is_bigger()
    {
        var s = Session(GameConfig.Default with { HordeInterval = TimeSpan.FromMinutes(1) });
        var hordes = new List<HordeInfo>();
        s.HordeStarted += h => hordes.Add(h);
        s.OpenQuiz();
        s.Tick(TimeSpan.FromSeconds(61), T0);

        s.FinishHorde(true, 5);
        Assert.Equal(GameMode.HordeSummary, s.Mode);
        Assert.Equal(5, s.LastHordeKills);
        Assert.Equal(5, s.Run.ZombiesKilled);
        Assert.Equal(1, s.Run.HordesSurvived);

        s.Tick(TimeSpan.FromSeconds(4), T0);
        Assert.Equal(GameMode.Idle, s.Mode);
        Assert.Null(s.CurrentHorde);
        Assert.Equal(TimeSpan.Zero, s.Clock.Elapsed);
        Assert.Equal(2, s.HordeNumber);

        s.OpenQuiz();
        s.Tick(TimeSpan.FromSeconds(61), T0);
        Assert.Equal(GameMode.Horde, s.Mode);
        Assert.Equal(new HordeInfo(2, 8), hordes.Last());
    }

    [Fact]
    public void Lost_horde_is_game_over_and_new_run_resets_combat_keeps_difficulty()
    {
        var s = Session();
        for (var i = 0; i < 3; i++) SolveOne(s);
        Assert.Equal(2, s.Difficulty.Level);

        s.ForceHordeNow();
        Assert.Equal(GameMode.Horde, s.Mode);
        s.FinishHorde(false, 3);
        Assert.Equal(GameMode.GameOver, s.Mode);
        Assert.Equal(3, s.Lifetime.ZombiesKilled);
        Assert.Equal(3, s.Lifetime.Correct);

        s.StartNewRun();
        Assert.Equal(GameMode.Idle, s.Mode);
        Assert.Equal(30, s.Combat.Ammo);
        Assert.Equal(2, s.Difficulty.Level);
        Assert.Equal(0, s.Run.Correct);
        Assert.Equal(1, s.HordeNumber);
        Assert.Null(s.CurrentQuiz);
        Assert.Null(s.CurrentHorde);
        Assert.Equal(3, s.Lifetime.ZombiesKilled);
    }

    [Fact]
    public void Fire_spends_ammo_and_records_shots()
    {
        var s = Session(GameConfig.Default with { StartAmmo = 2 });
        s.ForceHordeNow();
        Assert.NotEqual(ShotOutcome.NoAmmo, s.Fire());
        Assert.NotEqual(ShotOutcome.NoAmmo, s.Fire());
        Assert.Equal(ShotOutcome.NoAmmo, s.Fire());
        Assert.Equal(2, s.Run.AmmoSpent);
    }

    [Fact]
    public void Calls_in_wrong_mode_do_nothing_and_mode_changes_are_reported()
    {
        var s = Session();
        var modes = new List<GameMode>();
        s.ModeChanged += m => modes.Add(m);

        Assert.Equal(ShotOutcome.NoAmmo, s.Fire());
        Assert.Equal(30, s.Combat.Ammo);
        Assert.False(s.StartPause(T0));
        Assert.Null(s.Answer(0));
        s.FinishHorde(true, 1);
        s.StartNewRun();
        s.CollapseQuiz();
        Assert.Equal(GameMode.Idle, s.Mode);
        Assert.Empty(modes);

        s.OpenQuiz();
        AnswerCorrect(s);
        s.Tick(s.Config.FeedbackDuration, T0);
        s.ForceHordeNow();
        Assert.Equal(new[] { GameMode.QuizOpen, GameMode.Feedback, GameMode.Idle, GameMode.Horde }, modes);
    }
}
