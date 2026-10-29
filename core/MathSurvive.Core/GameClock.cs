using System;

namespace MathSurvive.Core;

public sealed class GameClock
{
    public TimeSpan Elapsed { get; private set; }

    public void Advance(TimeSpan delta) => Elapsed += delta;

    public bool IsHordeDue(TimeSpan interval) => Elapsed >= interval;

    public void Reset() => Elapsed = TimeSpan.Zero;
}
