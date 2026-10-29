using System;
using MathSurvive.Core.Stats;
using Xunit;

namespace MathSurvive.Core.Tests;

public class RunStatsTests
{
    [Fact]
    public void Average_and_sections()
    {
        var run = new RunStats();
        run.RecordAnswer("test", true, TimeSpan.FromSeconds(10), 1);
        run.RecordAnswer("test", false, TimeSpan.FromSeconds(20), 2);

        Assert.Equal(1, run.Correct);
        Assert.Equal(1, run.Wrong);
        Assert.Equal(TimeSpan.FromSeconds(30), run.AnswerTime);
        Assert.Equal(TimeSpan.FromSeconds(15), run.AverageAnswerTime);
        var s = run.Sections["test"];
        Assert.Equal(1, s.Correct);
        Assert.Equal(1, s.Wrong);
        Assert.Equal(2, s.MaxLevel);

        run.RecordShot();
        run.RecordShot();
        run.RecordKills(4);
        run.RecordHordeSurvived();

        var lifetime = new RunStats();
        lifetime.RecordAnswer("test", true, TimeSpan.FromSeconds(6), 3);
        lifetime.RecordAnswer("other", true, TimeSpan.FromSeconds(4), 1);
        lifetime.RecordShot();
        lifetime.RecordKills(1);

        run.MergeInto(lifetime);

        Assert.Equal(3, lifetime.Correct);
        Assert.Equal(1, lifetime.Wrong);
        Assert.Equal(TimeSpan.FromSeconds(40), lifetime.AnswerTime);
        Assert.Equal(TimeSpan.FromSeconds(10), lifetime.AverageAnswerTime);
        Assert.Equal(3, lifetime.AmmoSpent);
        Assert.Equal(5, lifetime.ZombiesKilled);
        Assert.Equal(1, lifetime.HordesSurvived);
        var merged = lifetime.Sections["test"];
        Assert.Equal(2, merged.Correct);
        Assert.Equal(1, merged.Wrong);
        Assert.Equal(3, merged.MaxLevel);
        Assert.Equal(1, lifetime.Sections["other"].Correct);

        // Источник не меняется.
        Assert.Equal(1, run.Correct);
        Assert.Equal(2, run.Sections["test"].MaxLevel);
    }

    [Fact]
    public void Average_is_zero_without_answers()
    {
        Assert.Equal(TimeSpan.Zero, new RunStats().AverageAnswerTime);
    }
}
