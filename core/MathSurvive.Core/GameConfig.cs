using System;

namespace MathSurvive.Core;

public sealed record GameConfig
{
    public TimeSpan HordeInterval { get; init; } = TimeSpan.FromMinutes(30);
    public TimeSpan PauseDuration { get; init; } = TimeSpan.FromMinutes(10);
    public int StartAmmo { get; init; } = 30;
    public int AmmoPerCorrect { get; init; } = 3;
    public double StartAccuracy { get; init; } = 1.0;
    public double AccuracyUp { get; init; } = 0.05;
    public double AccuracyDown { get; init; } = 0.10;
    public double MinAccuracy { get; init; } = 0.20;
    public int HordeBase { get; init; } = 5;
    public int HordeGrowth { get; init; } = 3;
    public double FireRangeFraction { get; init; } = 1.0 / 3;
    public int LevelUpStreak { get; init; } = 3;
    public int LevelDownStreak { get; init; } = 2;
    public TimeSpan FeedbackDuration { get; init; } = TimeSpan.FromSeconds(1.5);
    public TimeSpan HordeSummaryDuration { get; init; } = TimeSpan.FromSeconds(4);
    /// <summary>Логические px/с.</summary>
    public double ZombieSpeedPx { get; init; } = 40;
    public double ZombieSpawnIntervalSec { get; init; } = 8;
    public double FireIntervalSec { get; init; } = 0.8;
    public int IdleMaxFps { get; init; } = 15;
    public int ActiveMaxFps { get; init; } = 60;

    public static GameConfig Default { get; } = new();
}
