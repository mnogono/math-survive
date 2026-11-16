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

    private static QuizProgressSnapshot Sample(params string[] enabled) => new(enabled.Length == 0 ? new[] { "algebra" } : enabled, new[]
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
    [InlineData("arithmetic", "algebra")]
    public void Round_trip_keeps_enabled_sections_levels_streaks_and_used_ids(params string[] enabled)
    {
        var json = ProgressSaveCodec.Serialize(Sample(enabled));
        var back = ProgressSaveCodec.Deserialize(json, Packs(), Custom);
        Assert.Equivalent(Sample(enabled), back, strict: true);
    }

    [Fact]
    public void Json_has_only_progress_fields()
    {
        using var doc = JsonDocument.Parse(ProgressSaveCodec.Serialize(Sample("arithmetic", "algebra")));
        var root = doc.RootElement;
        Assert.Equal(new[] { "version", "enabled_section_ids", "sections" }, root.EnumerateObject().Select(p => p.Name));
        Assert.Equal(2, root.GetProperty("version").GetInt32());
        Assert.Equal(new[] { "arithmetic", "algebra" }, root.GetProperty("enabled_section_ids").EnumerateArray().Select(e => e.GetString()));
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

        Assert.Equal(new[] { "algebra" }, catalog.EnabledSectionIds);
        catalog.ActiveDifficulty.RecordAnswer(true);
        Assert.Equal(3, catalog.ActiveDifficulty.Level);

        catalog.EnableOnly("arithmetic");
        catalog.ActiveDifficulty.RecordAnswer(false);
        Assert.Equal(2, catalog.ActiveDifficulty.Level);
    }

    [Fact]
    public void Version_1_file_keeps_progress_and_enables_default_sections()
    {
        const string json = """
            {"version": 1, "selected_section_id": "algebra",
             "sections": [{"section_id": "algebra", "level": 3, "correct_streak": 1, "wrong_streak": 0, "used_question_ids": {"3": ["algebra-3-2"]}}]}
            """;
        var snapshot = ProgressSaveCodec.Deserialize(json, Packs(), Custom);
        Assert.NotNull(snapshot);
        Assert.Null(snapshot!.EnabledSectionIds);

        var catalog = new QuestionCatalog(Custom, Packs(), Order, new Random(1));
        catalog.RestoreProgress(snapshot);
        Assert.Equal(Order, catalog.EnabledSectionIds);
        var algebra = catalog.CaptureProgress().Sections.Single(s => s.SectionId == "algebra");
        Assert.Equal((3, 1), (algebra.Level, algebra.CorrectStreak));
        Assert.Equal(new[] { "algebra-3-2" }, algebra.UsedQuestionIdsByLevel[3]);
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
    [InlineData("{\"enabled_section_ids\": [\"topology\"], \"sections\": []}")]
    [InlineData("{\"enabled_section_ids\": [], \"sections\": []}")]
    [InlineData("{\"enabled_section_ids\": [null], \"sections\": []}")]
    [InlineData("{\"enabled_section_ids\": [\"algebra\", \"algebra\"], \"sections\": []}")]
    [InlineData("{\"enabled_section_ids\": \"algebra\", \"sections\": []}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {}}, {\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 0, \"used_question_ids\": {}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {\"x\": []}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {\"4\": []}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {\"1\": null}}]}")]
    [InlineData("{\"sections\": [{\"section_id\": \"algebra\", \"level\": 1, \"used_question_ids\": {\"1\": [null]}}]}")]
    public void Broken_or_invalid_json_gives_null(string json)
        => Assert.Null(ProgressSaveCodec.Deserialize(json, Packs(), Custom));
}
