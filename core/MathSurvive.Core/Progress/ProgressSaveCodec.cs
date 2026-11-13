using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MathSurvive.Core.Questions;

namespace MathSurvive.Core.Progress;

public static class ProgressSaveCodec
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
    };

    public static string Serialize(QuizProgressSnapshot snapshot)
    {
        var data = new ProgressSaveData
        {
            SelectedSectionId = snapshot.SelectedSectionId,
            Sections = snapshot.Sections.Select(s => new SectionProgressSaveData
            {
                SectionId = s.SectionId,
                Level = s.Level,
                CorrectStreak = s.CorrectStreak,
                WrongStreak = s.WrongStreak,
                UsedQuestionIds = s.UsedQuestionIdsByLevel.ToDictionary(kv => kv.Key, kv => kv.Value.ToList()),
            }).ToList(),
        };
        return JsonSerializer.Serialize(data, Options);
    }

    /// <returns><c>null</c>, если JSON повреждён или не проходит <see cref="ProgressRules.IsValid"/>.</returns>
    public static QuizProgressSnapshot? Deserialize(string json, IReadOnlyList<QuestionPack> packs, GameConfig config)
    {
        ProgressSaveData? data;
        try
        {
            data = JsonSerializer.Deserialize<ProgressSaveData>(json, Options);
        }
        catch (Exception e) when (e is JsonException or NotSupportedException or ArgumentException)
        {
            return null;
        }
        if (data?.Sections is null || data.Sections.Any(s => s is null)) return null;

        var snapshot = new QuizProgressSnapshot(data.SelectedSectionId, data.Sections.Select(s =>
            new SectionProgressSnapshot(s.SectionId!, s.Level, s.CorrectStreak, s.WrongStreak,
                s.UsedQuestionIds?.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value)!)).ToList());
        return ProgressRules.IsValid(snapshot, packs.Select(p => p.Section), config) ? snapshot : null;
    }
}
