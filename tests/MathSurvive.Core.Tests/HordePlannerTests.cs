using System;
using MathSurvive.Core;
using Xunit;

namespace MathSurvive.Core.Tests;

public class HordePlannerTests
{
    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 8)]
    [InlineData(5, 17)]
    public void ZombieCount_grows_linearly(int horde, int expected)
        => Assert.Equal(expected, HordePlanner.ZombieCount(horde, GameConfig.Default));

    [Fact]
    public void SpawnGroup_contains_five_to_seven_zombies()
    {
        for (var seed = 0; seed < 20; seed++)
            Assert.InRange(HordePlanner.SpawnGroupOffsets(20, new Random(seed)).Count, 5, 7);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(7)]
    public void SpawnGroup_takes_all_remaining_zombies_for_final_group(int remaining)
        => Assert.Equal(remaining, HordePlanner.SpawnGroupOffsets(remaining, new Random(0)).Count);

    [Fact]
    public void SpawnGroup_zombies_overlap()
    {
        var offsets = HordePlanner.SpawnGroupOffsets(7, new Random(0));
        Assert.Equal(0.0, offsets[0]);
        for (var i = 1; i < offsets.Count; i++)
            Assert.InRange(offsets[i] - offsets[i - 1], 8.0, 16.0);
    }

    [Fact]
    public void GroupDelay_varies_between_two_and_four_seconds()
    {
        var first = HordePlanner.NextGroupDelaySec(new Random(0));
        var varies = false;
        for (var seed = 0; seed < 20; seed++)
        {
            var delay = HordePlanner.NextGroupDelaySec(new Random(seed));
            Assert.InRange(delay, 2.0, 4.0);
            varies |= delay != first;
        }
        Assert.True(varies);
    }
}
