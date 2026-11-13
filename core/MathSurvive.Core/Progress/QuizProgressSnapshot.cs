using System.Collections.Generic;

namespace MathSurvive.Core.Progress;

/// <summary>Сохраняемый прогресс квиза. <c>SelectedSectionId == null</c> — случайный режим.</summary>
public sealed record QuizProgressSnapshot(string? SelectedSectionId, IReadOnlyList<SectionProgressSnapshot> Sections);

public sealed record SectionProgressSnapshot(
    string SectionId,
    int Level,
    int CorrectStreak,
    int WrongStreak,
    IReadOnlyDictionary<int, IReadOnlyList<string>> UsedQuestionIdsByLevel);
