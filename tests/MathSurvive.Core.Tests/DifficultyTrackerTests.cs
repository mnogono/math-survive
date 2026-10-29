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
}
