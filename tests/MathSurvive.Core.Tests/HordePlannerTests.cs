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
}
