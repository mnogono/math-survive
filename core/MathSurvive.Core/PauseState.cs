using System;

namespace MathSurvive.Core;

public sealed class PauseState
{
    public bool Available { get; private set; } = true;
    public DateTime? Until { get; private set; }

    public bool TryStart(DateTime nowUtc, TimeSpan duration)
    {
        if (!Available) return false;
        Until = nowUtc + duration;
        Available = false;
        return true;
    }

    public bool IsActive(DateTime nowUtc) => Until.HasValue && nowUtc < Until.Value;

    public void OnCorrectAnswer() => Available = true;
}
