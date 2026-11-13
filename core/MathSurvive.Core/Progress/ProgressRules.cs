using System.Collections.Generic;
using System.Linq;
using MathSurvive.Core.Questions;

namespace MathSurvive.Core.Progress;

/// <summary>Общая проверка прогресса для трекера сложности, каталога и файла сохранения.</summary>
public static class ProgressRules
{
    public static bool IsValidDifficulty(GameConfig config, int maxLevel, int level, int correctStreak, int wrongStreak)
        => level >= 1 && level <= maxLevel
           && correctStreak >= 0 && correctStreak < config.LevelUpStreak
           && wrongStreak >= 0 && wrongStreak < config.LevelDownStreak;

    /// <summary>
    /// Разделы и уровни должны существовать, серии — быть меньше порогов <paramref name="config"/>.
    /// Неизвестные ID вопросов допустимы: пакет мог измениться, каталог их отбрасывает.
    /// </summary>
    public static bool IsValid(QuizProgressSnapshot? snapshot, IEnumerable<SectionInfo> sections, GameConfig config)
    {
        if (snapshot?.Sections is null) return false;
        var levels = sections.ToDictionary(s => s.Id, s => s.Levels);
        if (snapshot.SelectedSectionId is not null && !levels.ContainsKey(snapshot.SelectedSectionId)) return false;

        var seen = new HashSet<string>();
        foreach (var section in snapshot.Sections)
        {
            if (section?.SectionId is null || !levels.TryGetValue(section.SectionId, out var max) || !seen.Add(section.SectionId))
                return false;
            if (!IsValidDifficulty(config, max, section.Level, section.CorrectStreak, section.WrongStreak)) return false;
            if (section.UsedQuestionIdsByLevel is null) return false;
            foreach (var (level, ids) in section.UsedQuestionIdsByLevel)
                if (level < 1 || level > max || ids is null || ids.Any(id => id is null)) return false;
        }
        return true;
    }
}
