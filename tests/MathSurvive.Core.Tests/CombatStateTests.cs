using MathSurvive.Core;
using Xunit;

namespace MathSurvive.Core.Tests;

public class CombatStateTests
{
    [Fact]
    public void Starts_with_config_values()
    {
        var s = new CombatState(GameConfig.Default);
        Assert.Equal(30, s.Ammo);
        Assert.Equal(1.0, s.Accuracy);
    }

    [Fact]
    public void Correct_answer_adds_ammo_and_accuracy_capped()
    {
        var s = new CombatState(GameConfig.Default);
        s.ApplyAnswer(false);
        s.ApplyAnswer(true);
        Assert.Equal(33, s.Ammo);
        Assert.Equal(0.95, s.Accuracy, 1e-9);
        s.ApplyAnswer(true);
        s.ApplyAnswer(true);
        Assert.Equal(1.0, s.Accuracy);
    }

    [Fact]
    public void Wrong_answer_reduces_accuracy_floored()
    {
        var s = new CombatState(GameConfig.Default);
        for (int i = 0; i < 10; i++) s.ApplyAnswer(false);
        Assert.Equal(0.20, s.Accuracy, 1e-9);
        Assert.Equal(30, s.Ammo);
    }

    [Fact]
    public void TryConsumeAmmo_stops_at_zero()
    {
        var s = new CombatState(new GameConfig { StartAmmo = 2 });
        Assert.True(s.TryConsumeAmmo());
        Assert.True(s.TryConsumeAmmo());
        Assert.False(s.TryConsumeAmmo());
        Assert.Equal(0, s.Ammo);
    }
}
