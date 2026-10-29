using System;

namespace MathSurvive.Core;

public enum ShotOutcome { NoAmmo, Miss, Hit }

public static class ShotResolver
{
    public static ShotOutcome Fire(CombatState s, Random rng)
    {
        if (!s.TryConsumeAmmo()) return ShotOutcome.NoAmmo;
        return rng.NextDouble() < s.Accuracy ? ShotOutcome.Hit : ShotOutcome.Miss;
    }
}
