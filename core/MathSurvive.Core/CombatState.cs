using System;

namespace MathSurvive.Core;

public sealed class CombatState
{
    private readonly GameConfig _c;

    public CombatState(GameConfig c)
    {
        _c = c;
        Ammo = c.StartAmmo;
        Accuracy = c.StartAccuracy;
    }

    public int Ammo { get; private set; }
    public double Accuracy { get; private set; }

    public void ApplyAnswer(bool correct)
    {
        if (correct)
        {
            Ammo += _c.AmmoPerCorrect;
            Accuracy = Math.Min(1.0, Accuracy + _c.AccuracyUp);
        }
        else
        {
            Accuracy = Math.Max(_c.MinAccuracy, Accuracy - _c.AccuracyDown);
        }
    }

    public bool TryConsumeAmmo()
    {
        if (Ammo <= 0) return false;
        Ammo--;
        return true;
    }
}
