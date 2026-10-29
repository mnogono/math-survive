using System;
using MathSurvive.Core;
using Xunit;

namespace MathSurvive.Core.Tests;

public class PauseStateTests
{
    [Fact]
    public void Pause_is_single_use_until_correct()
    {
        var p = new PauseState();
        var t0 = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var d = TimeSpan.FromMinutes(10);
        Assert.True(p.TryStart(t0, d));
        Assert.True(p.IsActive(t0 + TimeSpan.FromMinutes(5)));
        Assert.False(p.IsActive(t0 + d));
        Assert.False(p.TryStart(t0 + TimeSpan.FromMinutes(11), d));
        p.OnCorrectAnswer();
        Assert.True(p.TryStart(t0 + TimeSpan.FromMinutes(11), d));
    }
}
