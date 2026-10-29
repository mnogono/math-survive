using System;
using MathSurvive.Core;
using Xunit;

namespace MathSurvive.Core.Tests;

public class ShotResolverTests
{
    [Fact]
    public void No_ammo_returns_NoAmmo()
    {
        var s = new CombatState(new GameConfig { StartAmmo = 0 });
        Assert.Equal(ShotOutcome.NoAmmo, ShotResolver.Fire(s, new Random(1)));
    }

    [Fact]
    public void Full_accuracy_always_hits()
    {
        var s = new CombatState(new GameConfig { StartAmmo = 100 });
        var rng = new Random(1);
        for (int i = 0; i < 100; i++)
            Assert.Equal(ShotOutcome.Hit, ShotResolver.Fire(s, rng));
    }

    [Fact]
    public void Accuracy_drives_hit_rate()
    {
        var s = new CombatState(new GameConfig { StartAccuracy = 0.2, StartAmmo = 10000 });
        var rng = new Random(42);
        int hits = 0;
        for (int i = 0; i < 10000; i++)
            if (ShotResolver.Fire(s, rng) == ShotOutcome.Hit) hits++;
        double rate = hits / 10000.0;
        Assert.InRange(rate, 0.17, 0.23);
    }
}
