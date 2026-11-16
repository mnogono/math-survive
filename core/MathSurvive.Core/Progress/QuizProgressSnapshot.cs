using System.Collections.Generic;

namespace MathSurvive.Core.Progress;

/// <summary>Сохраняемый прогресс квиза. <c>EnabledSectionIds == null</c> (старый формат) — включены разделы по умолчанию.</summary>
public sealed record QuizProgressSnapshot(IReadOnlyList<string>? EnabledSectionIds, IReadOnlyList<SectionProgressSnapshot> Sections);

public sealed record SectionProgressSnapshot(
    string SectionId,
    int Level,
    int CorrectStreak,
    int WrongStreak,
    IReadOnlyDictionary<int, IReadOnlyList<string>> UsedQuestionIdsByLevel);
