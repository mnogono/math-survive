namespace MathSurvive.Core;

public static class HordePlanner
{
    /// <summary>hordeNumber считается с 1.</summary>
    public static int ZombieCount(int hordeNumber, GameConfig c)
        => c.HordeBase + c.HordeGrowth * (hordeNumber - 1);
}
