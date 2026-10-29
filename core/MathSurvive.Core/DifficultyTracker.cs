using System;

namespace MathSurvive.Core;

public sealed class DifficultyTracker
{
    private readonly GameConfig _config;
    private int _correctStreak;
    private int _wrongStreak;

    public DifficultyTracker(GameConfig c, int maxLevel, int level = 1)
    {
        _config = c;
        MaxLevel = maxLevel;
        Level = Math.Clamp(level, 1, maxLevel);
    }

    public int Level { get; private set; }
    public int MaxLevel { get; }

    public void RecordAnswer(bool correct)
    {
        if (correct)
        {
            _wrongStreak = 0;
            if (++_correctStreak >= _config.LevelUpStreak)
            {
                Level = Math.Min(MaxLevel, Level + 1);
                _correctStreak = 0;
            }
        }
        else
        {
            _correctStreak = 0;
            if (++_wrongStreak >= _config.LevelDownStreak)
            {
                Level = Math.Max(1, Level - 1);
                _wrongStreak = 0;
            }
        }
    }
}
