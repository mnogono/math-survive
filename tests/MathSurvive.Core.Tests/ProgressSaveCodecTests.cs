using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MathSurvive.Core.Progress;
using MathSurvive.Core.Questions;
using Xunit;

namespace MathSurvive.Core.Tests;

public class ProgressSaveCodecTests
{
    private static readonly GameConfig Custom = GameConfig.Default with { LevelUpStreak = 4, LevelDownStreak = 3 };
    private static readonly string[] Order = { "arithmetic", "algebra" };

    private static Question Q(string id, int level) => new(id, level, "p", null,
        new[] { new QuestionOption("a", null), new QuestionOption("b", null), new QuestionOption("c", null), new QuestionOption("d", null) }, 0);

    private static QuestionPack[] Packs() => Order.Select(id => new QuestionPack(new SectionInfo(id, id, 3),
        Enumerable.Range(1, 3).SelectMany(l => Enumerable.Range(1, 3).Select(i => Q($"{id}-{l}-{i}", l))).ToList())).ToArray();

    private static QuizProgressSnapshot Sample(string? selected = "algebra") => new(selected, new[]
    {
        new SectionProgressSnapshot("arithmetic", 3, 0, 2, new Dictionary<int, IReadOnlyList<string>>
        {
            [1] = new[] { "arithmetic-1-1", "arithmetic-1-2" },
            [3] = new[] { "arithmetic-3-1" },
        }),
        new SectionProgressSnapshot("algebra", 2, 3, 0, new Dictionary<int, IReadOnlyList<string>>()),
    });

    [Theory]
    [InlineData("algebra")]
    [InlineData(null)]
    public void Round_trip_keeps_selection_levels_streaks_and_used_ids(string? selected)
    {
        var json = ProgressSaveCodec.Serialize(Sample(selected));
        var back = ProgressSaveCodec.Deserialize(json, Packs(), Custom);
        Assert.Equivalent(Sample(selected), back, strict: true);
    }

    [Fact]
    public void Json_has_only_progress_fields()
    {
        using var doc = JsonDocument.Parse(ProgressSaveCodec.Serialize(Sample(null)));
        var root = doc.RootElement;
        Assert.Equal(new[] { "version", "selected_section_id", "sections" }, root.EnumerateObject().Select(p => p.Name));
        Assert.Equal(JsonValueKind.Null, root.GetProperty("selected_section_id").ValueKind);
        Assert.Equal(new[] { "section_id", "level", "correct_streak", "wrong_streak", "used_question_ids" },
            root.GetProperty("sections")[0].EnumerateObject().Select(p => p.Name));
        Assert.Equal(2, root.GetProperty("sections")[0].GetProperty("used_question_ids").GetProperty("1").GetArrayLength());
    }

    [Fact]
    public void Restored_catalog_applies_custom_thresholds()
    {
        var snapshot = ProgressSaveCodec.Deserialize(ProgressSaveCodec.Serialize(Sample()), Packs(), Custom);
        var catalog = new QuestionCatalog(Custom, Packs(), Order, new Random(1));
        catalog.RestoreProgress(snapshot);

        Assert.Equal("algebra", catalog.SelectedSectionId);
        catalog.ActiveDifficulty.RecordAnswer(true);
        Assert.Equal(3, catalog.ActiveDifficulty.Level);

        catalog.SelectSection("arithmetic");
        catalog.ActiveDifficulty.RecordAnswer(false);
        Assert.Equal(2, catalog.ActiveDifficulty.Level);
    }

    [Fact]
    public void Streaks_beyond_config_thresholds_are_rejected()
    {
        var json = ProgressSaveCodec.Serialize(Sample());
        Assert.Null(ProgressSaveCodec.Deserialize(json, Packs(), GameConfig.Default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"sections\": null}")]
    [InlineData("{\"sections\": [null]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": \"x\"}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1}]}")]
    [InlineData("{\"sections\": [{\"level\": 1, \"used_question_ids\": {}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"topology\", \"level\": 1, \"used_question_ids\": {}}]}")]
    [InlineData("{\"selected_section_id\": \"topology\", \"sections\": []}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {}}, {\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 0, \"used_question_ids\": {}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {\"x\": []}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {\"4\": []}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {\"1\": null}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {\"1\": [null]}}]}")]
    public void Broken_or_invalid_json_gives_null(string json)
        => Assert.Null(ProgressSaveCodec.Deserialize(json, Packs(), Custom));
}
