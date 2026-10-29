using System;
using MathSurvive.Core;
using Xunit;

namespace MathSurvive.Core.Tests;

public class GameClockTests
{
    [Fact]
    public void Clock_due_at_interval()
    {
        var c = new GameClock();
        var interval = TimeSpan.FromMinutes(30);
        c.Advance(new TimeSpan(0, 29, 59));
        Assert.False(c.IsHordeDue(interval));
        c.Advance(TimeSpan.FromSeconds(1));
        Assert.True(c.IsHordeDue(interval));
        c.Reset();
        Assert.Equal(TimeSpan.Zero, c.Elapsed);
    }
}
