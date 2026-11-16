using System.Collections.Generic;

namespace MathSurvive.Core.Progress;

/// <summary>JSON-форма <c>save.json</c> (snake_case через <see cref="ProgressSaveCodec"/>).</summary>
public sealed class ProgressSaveData
{
    public int Version { get; set; } = 2;
    public List<string>? EnabledSectionIds { get; set; }
    public List<SectionProgressSaveData>? Sections { get; set; }
}

public sealed class SectionProgressSaveData
{
    public string? SectionId { get; set; }
    public int Level { get; set; }
    public int CorrectStreak { get; set; }
    public int WrongStreak { get; set; }
    public Dictionary<int, List<string>>? UsedQuestionIds { get; set; }
}
