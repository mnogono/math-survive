using System;
using MathSurvive.Core;
using Xunit;

namespace MathSurvive.Core.Tests;

public class DifficultyTrackerTests
{
    private static void Feed(DifficultyTracker t, bool v, int n)
    {
        for (var i = 0; i < n; i++) t.RecordAnswer(v);
    }

    [Fact]
    public void Three_correct_in_a_row_raise_level()
    {
        var t = new DifficultyTracker(GameConfig.Default, maxLevel: 5);
        Feed(t, true, 3);
        Assert.Equal(2, t.Level);
        Feed(t, true, 2);
        Assert.Equal(2, t.Level);
        Feed(t, true, 1);
        Assert.Equal(3, t.Level);
    }

    [Fact]
    public void Streak_breaks_on_wrong()
    {
        var t = new DifficultyTracker(GameConfig.Default, maxLevel: 5);
        t.RecordAnswer(true); t.RecordAnswer(true); t.RecordAnswer(false); t.RecordAnswer(true);
        Assert.Equal(1, t.Level);
    }

    [Fact]
    public void Two_wrong_lower_level_not_below_one()
    {
        var t = new DifficultyTracker(GameConfig.Default, maxLevel: 5, level: 3);
        Feed(t, false, 2);
        Assert.Equal(2, t.Level);
        Feed(t, false, 2);
        Assert.Equal(1, t.Level);
        Feed(t, false, 2);
        Assert.Equal(1, t.Level);
    }

    [Fact]
    public void Level_capped_at_max()
    {
        var t = new DifficultyTracker(GameConfig.Default, maxLevel: 2);
        Feed(t, true, 9);
        Assert.Equal(2, t.Level);
    }

    [Fact]
    public void Opposite_streak_resets()
    {
        var t = new DifficultyTracker(GameConfig.Default, maxLevel: 3, level: 2);
        t.RecordAnswer(false);
        Assert.Equal(1, t.WrongStreak);
        t.RecordAnswer(true);
        Assert.Equal((1, 0), (t.CorrectStreak, t.WrongStreak));
        t.RecordAnswer(false);
        Assert.Equal((0, 1), (t.CorrectStreak, t.WrongStreak));
        Assert.Equal(2, t.Level);
    }

    [Fact]
    public void Streak_resets_at_bounds()
    {
        var top = new DifficultyTracker(GameConfig.Default, maxLevel: 3, level: 3);
        Feed(top, true, GameConfig.Default.LevelUpStreak);
        Assert.Equal((3, 0), (top.Level, top.CorrectStreak));

        var bottom = new DifficultyTracker(GameConfig.Default, maxLevel: 3);
        Feed(bottom, false, GameConfig.Default.LevelDownStreak);
        Assert.Equal((1, 0), (bottom.Level, bottom.WrongStreak));
    }

    [Fact]
    public void Custom_thresholds_apply()
    {
        var config = GameConfig.Default with { LevelUpStreak = 4, LevelDownStreak = 3 };
        var t = new DifficultyTracker(config, maxLevel: 3);
        Feed(t, true, config.LevelUpStreak - 1);
        Assert.Equal(1, t.Level);
        Feed(t, true, 1);
        Assert.Equal(2, t.Level);
        Feed(t, false, config.LevelDownStreak - 1);
        Assert.Equal(2, t.Level);
        Feed(t, false, 1);
        Assert.Equal(1, t.Level);
    }

    [Fact]
    public void Restore_sets_level_and_streaks()
    {
        var t = new DifficultyTracker(GameConfig.Default, maxLevel: 3);
        t.Restore(2, 2, 0);
        Assert.Equal((2, 2, 0), (t.Level, t.CorrectStreak, t.WrongStreak));
        t.RecordAnswer(true);
        Assert.Equal((3, 0), (t.Level, t.CorrectStreak));

        t.Restore(3, 0, 1);
        t.RecordAnswer(false);
        Assert.Equal((2, 0), (t.Level, t.WrongStreak));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(4, 0, 0)]
    [InlineData(1, 3, 0)]
    [InlineData(1, 0, 2)]
    [InlineData(1, -1, 0)]
    [InlineData(1, 0, -1)]
    public void Restore_rejects_invalid_state(int level, int correct, int wrong)
    {
        var t = new DifficultyTracker(GameConfig.Default, maxLevel: 3, level: 2);
        Assert.Throws<ArgumentOutOfRangeException>(() => t.Restore(level, correct, wrong));
        Assert.Equal((2, 0, 0), (t.Level, t.CorrectStreak, t.WrongStreak));
    }
}
