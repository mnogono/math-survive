using System;
using System.Collections.Generic;

namespace MathSurvive.Core;

public static class HordePlanner
{
    /// <summary>hordeNumber считается с 1.</summary>
    public static int ZombieCount(int hordeNumber, GameConfig c)
        => c.HordeBase + c.HordeGrowth * (hordeNumber - 1);

    public static IReadOnlyList<double> SpawnGroupOffsets(int remaining, Random random)
    {
        var count = Math.Min(random.Next(5, 8), remaining);
        var offsets = new double[count];
        for (var i = 1; i < count; i++)
            offsets[i] = offsets[i - 1] + random.Next(8, 17);
        return offsets;
    }

    public static double NextGroupDelaySec(Random random)
        => 2.0 + random.NextDouble() * 2.0;
}
