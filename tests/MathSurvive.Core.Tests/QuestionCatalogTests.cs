using System;
using System.Collections.Generic;
using System.Linq;
using MathSurvive.Core.Questions;
using MathSurvive.Core.Progress;
using Xunit;

namespace MathSurvive.Core.Tests;

public class QuestionCatalogTests
{
    private static readonly GameConfig Config = GameConfig.Default with { LevelUpStreak = 4, LevelDownStreak = 3 };
    private static readonly string[] Order = { "arithmetic", "algebra", "geometry", "analysis" };

    private static Question Q(string id, int level) => new(id, level, "p", null,
        new[] { new QuestionOption("a", null), new QuestionOption("b", null), new QuestionOption("c", null), new QuestionOption("d", null) }, 0);

    /// <summary>Раздел с префиксом ID: по одному вопросу <c>{p}1..{p}3</c> на уровень, плюс <paramref name="extraL1"/> на уровне 1.</summary>
    private static QuestionPack Pack(string id, string p, int extraL1 = 0)
    {
        var questions = new List<Question> { Q($"{p}1", 1), Q($"{p}2", 2), Q($"{p}3", 3) };
        questions.AddRange(Enumerable.Range(1, extraL1).Select(i => Q($"{p}1-{i}", 1)));
        return new QuestionPack(new SectionInfo(id, id, 3), questions);
    }

    private static QuestionPack[] Packs(int geometryExtraL1 = 0) => new[]
    {
        Pack("arithmetic", "a"), Pack("algebra", "b"), Pack("geometry", "c", geometryExtraL1), Pack("analysis", "d"),
    };

    private static QuestionCatalog Catalog(int geometryExtraL1 = 0, int seed = 1)
        => new(Config, Packs(geometryExtraL1), Order, new Random(seed));

    private static void Record(QuestionCatalog c, bool correct, int times)
    {
        for (var i = 0; i < times; i++) c.ActiveDifficulty.RecordAnswer(correct);
    }

    [Fact]
    public void Starts_with_default_sections_enabled_and_first_active()
    {
        var c = Catalog();
        Assert.Equal(Order, c.EnabledSectionIds);
        Assert.All(Order, id => Assert.True(c.IsEnabled(id)));
        Assert.Equal("arithmetic", c.ActivePack.Section.Id);
    }

    private static string Prefix(string section) => section switch
    {
        "arithmetic" => "a",
        "algebra" => "b",
        "geometry" => "c",
        _ => "d",
    };

    [Fact]
    public void Next_question_comes_from_random_enabled_section()
    {
        var c = Catalog(seed: 3);
        var seen = new HashSet<string>();
        for (var i = 0; i < 40; i++)
        {
            var question = c.NextQuestion();
            var section = c.ActivePack.Section.Id;
            Assert.StartsWith(Prefix(section), question.Id);
            seen.Add(section);
        }
        Assert.Equal(Order.ToHashSet(), seen);

        Assert.True(c.SetEnabled("arithmetic", false));
        Assert.True(c.SetEnabled("analysis", false));
        Assert.Equal(new[] { "algebra", "geometry" }, c.EnabledSectionIds);
        seen.Clear();
        for (var i = 0; i < 30; i++)
        {
            c.NextQuestion();
            seen.Add(c.ActivePack.Section.Id);
        }
        Assert.Equal(new HashSet<string> { "algebra", "geometry" }, seen);
    }

    [Fact]
    public void Same_seed_gives_same_sections()
    {
        static string[] Run(int seed)
        {
            var c = Catalog(seed: seed);
            return Enumerable.Range(0, 12).Select(_ =>
            {
                c.NextQuestion();
                return c.ActivePack.Section.Id;
            }).ToArray();
        }
        Assert.Equal(Run(7), Run(7));
    }

    [Fact]
    public void Last_enabled_section_cannot_be_disabled()
    {
        var c = Catalog();
        Assert.False(c.SetEnabled("algebra", true));
        Assert.True(c.SetEnabled("arithmetic", false));
        Assert.False(c.SetEnabled("arithmetic", false));
        Assert.True(c.SetEnabled("algebra", false));
        Assert.True(c.SetEnabled("geometry", false));

        Assert.False(c.SetEnabled("analysis", false));
        Assert.Equal(new[] { "analysis" }, c.EnabledSectionIds);
        Assert.Equal("d1", c.NextQuestion().Id);

        Assert.True(c.SetEnabled("arithmetic", true));
        Assert.Equal(new[] { "arithmetic", "analysis" }, c.EnabledSectionIds);
    }

    [Fact]
    public void All_sections_use_the_given_config()
    {
        var c = Catalog();
        foreach (var section in Order)
        {
            c.EnableOnly(section);
            Record(c, true, 3);
            Assert.Equal(1, c.ActiveDifficulty.Level);
            Record(c, true, 1);
            Assert.Equal(2, c.ActiveDifficulty.Level);
            Record(c, false, 2);
            Assert.Equal(2, c.ActiveDifficulty.Level);
            Record(c, false, 1);
            Assert.Equal(1, c.ActiveDifficulty.Level);
        }
    }

    [Fact]
    public void Picks_only_current_level_without_fallback()
    {
        var c = Catalog();
        c.EnableOnly("algebra");
        Record(c, true, 4);

        Assert.Equal("b2", c.NextQuestion().Id);
        Assert.Equal("b2", c.NextQuestion().Id);

        Record(c, true, 4);
        Assert.Equal("b3", c.NextQuestion().Id);

        Record(c, false, 3);
        Assert.Equal("b2", c.NextQuestion().Id);
    }

    [Fact]
    public void Sections_keep_independent_used_ids()
    {
        var c = Catalog(geometryExtraL1: 2);
        c.EnableOnly("geometry");
        var first = c.NextQuestion().Id;
        var second = c.NextQuestion().Id;

        c.EnableOnly("arithmetic");
        Assert.Equal("arithmetic", c.ActivePack.Section.Id);
        Assert.Equal("a1", c.NextQuestion().Id);

        c.EnableOnly("geometry");
        var third = c.NextQuestion().Id;
        Assert.Equal(3, new[] { first, second, third }.Distinct().Count());
    }

    [Fact]
    public void Enable_only_makes_section_active()
    {
        var c = Catalog();
        c.EnableOnly("algebra");
        Assert.Equal(new[] { "algebra" }, c.EnabledSectionIds);
        Assert.Equal("algebra", c.ActivePack.Section.Id);
        Assert.Equal("b2", c.FindActive("b2")!.Id);
        Assert.Null(c.FindActive("a1"));
    }

    [Fact]
    public void Section_outside_defaults_can_be_enabled()
    {
        var c = new QuestionCatalog(Config, Packs(), new[] { "algebra" }, new Random(1));
        Assert.Equal(new[] { "algebra" }, c.EnabledSectionIds);
        Assert.False(c.IsEnabled("geometry"));
        Assert.True(c.SetEnabled("geometry", true));
        Assert.Equal(new[] { "algebra", "geometry" }, c.EnabledSectionIds);
    }

    [Fact]
    public void Rejects_invalid_setup()
    {
        var rng = new Random(1);
        Assert.Throws<ArgumentException>(() => new QuestionCatalog(Config, new[] { Pack("arithmetic", "a"), Pack("arithmetic", "b") }, new[] { "arithmetic" }, rng));
        Assert.Throws<ArgumentException>(() => new QuestionCatalog(Config, Packs(), Array.Empty<string>(), rng));
        Assert.Throws<ArgumentException>(() => new QuestionCatalog(Config, Packs(), new[] { "algebra", "algebra" }, rng));
        Assert.Throws<ArgumentException>(() => new QuestionCatalog(Config, Packs(), new[] { "algebra", "topology" }, rng));
        Assert.Throws<ArgumentException>(() => Catalog().SetEnabled("topology", true));
        Assert.Throws<ArgumentException>(() => Catalog().IsEnabled("topology"));
        Assert.Throws<ArgumentException>(() => Catalog().EnableOnly("topology"));
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<string>> Used(params (int Level, string[] Ids)[] items)
        => items.ToDictionary(i => i.Level, i => (IReadOnlyList<string>)i.Ids);

    private static SectionProgressSnapshot Section(QuizProgressSnapshot s, string id) => s.Sections.Single(x => x.SectionId == id);

    [Fact]
    public void Capture_and_restore_round_trip()
    {
        var c = Catalog(geometryExtraL1: 3);
        c.EnableOnly("geometry");
        c.NextQuestion();
        c.NextQuestion();
        Record(c, true, 4);
        c.NextQuestion();
        Record(c, true, 2);
        c.EnableOnly("algebra");
        Record(c, false, 2);
        c.SetEnabled("analysis", true);

        var snapshot = c.CaptureProgress();
        Assert.Equal(new[] { "algebra", "analysis" }, snapshot.EnabledSectionIds);
        var geometry = Section(snapshot, "geometry");
        Assert.Equal((2, 2, 0), (geometry.Level, geometry.CorrectStreak, geometry.WrongStreak));
        Assert.Equal(2, geometry.UsedQuestionIdsByLevel[1].Count);
        Assert.Equal(new[] { "c2" }, geometry.UsedQuestionIdsByLevel[2]);
        var algebra = Section(snapshot, "algebra");
        Assert.Equal((1, 0, 2), (algebra.Level, algebra.CorrectStreak, algebra.WrongStreak));

        var restored = Catalog(geometryExtraL1: 3, seed: 9);
        restored.RestoreProgress(snapshot);
        Assert.Equal(new[] { "algebra", "analysis" }, restored.EnabledSectionIds);
        Assert.Equal("algebra", restored.ActivePack.Section.Id);
        Assert.Equivalent(snapshot, restored.CaptureProgress(), strict: true);

        restored.EnableOnly("geometry");
        Assert.Equal(2, restored.ActiveDifficulty.Level);
        Record(restored, true, 2);
        Assert.Equal(3, restored.ActiveDifficulty.Level);
        Record(restored, false, 6);
        Assert.Equal(1, restored.ActiveDifficulty.Level);
        var fresh = new[] { restored.NextQuestion().Id, restored.NextQuestion().Id };
        Assert.Empty(fresh.Intersect(geometry.UsedQuestionIdsByLevel[1]));
    }

    [Fact]
    public void Snapshot_without_enabled_list_enables_defaults_and_keeps_levels()
    {
        var restored = Catalog();
        restored.EnableOnly("algebra");
        restored.RestoreProgress(new QuizProgressSnapshot(null, new[] { new SectionProgressSnapshot("geometry", 3, 0, 0, Used()) }));

        Assert.Equal(Order, restored.EnabledSectionIds);
        Assert.Equal(3, Section(restored.CaptureProgress(), "geometry").Level);
    }

    public static IEnumerable<object?[]> InvalidSnapshots()
    {
        var ok = new SectionProgressSnapshot("algebra", 2, 1, 0, Used((1, new[] { "b1" })));
        yield return new object?[] { null };
        yield return new object?[] { new QuizProgressSnapshot(new[] { "topology" }, new[] { ok }) };
        yield return new object?[] { new QuizProgressSnapshot(Array.Empty<string>(), new[] { ok }) };
        yield return new object?[] { new QuizProgressSnapshot(new[] { "algebra", "algebra" }, new[] { ok }) };
        yield return new object?[] { new QuizProgressSnapshot(null, new[] { ok, ok }) };
        yield return new object?[] { new QuizProgressSnapshot(null, new[] { ok, ok with { SectionId = "topology" } }) };
        yield return new object?[] { new QuizProgressSnapshot(null, new[] { ok with { Level = 4 } }) };
        yield return new object?[] { new QuizProgressSnapshot(null, new[] { ok with { CorrectStreak = 4 } }) };
        yield return new object?[] { new QuizProgressSnapshot(null, new[] { ok with { WrongStreak = 3 } }) };
        yield return new object?[] { new QuizProgressSnapshot(null, new[] { ok with { UsedQuestionIdsByLevel = Used((5, new[] { "b1" })) } }) };
    }

    [Theory]
    [MemberData(nameof(InvalidSnapshots))]
    public void Invalid_snapshot_restores_defaults(QuizProgressSnapshot? snapshot)
    {
        var c = Catalog();
        c.EnableOnly("geometry");
        Record(c, true, 4);
        c.NextQuestion();
        c.EnableOnly("arithmetic");
        Record(c, false, 1);

        c.RestoreProgress(snapshot);

        Assert.Equivalent(Catalog().CaptureProgress(), c.CaptureProgress(), strict: true);
        Assert.Equal(Order, c.EnabledSectionIds);
        Assert.Equal("arithmetic", c.ActivePack.Section.Id);
    }

    [Fact]
    public void Restore_accepts_thresholds_of_its_config_only()
    {
        var snapshot = new QuizProgressSnapshot(new[] { "geometry" }, new[] { new SectionProgressSnapshot("geometry", 2, 3, 0, Used()) });
        var c = Catalog();
        c.RestoreProgress(snapshot);
        Assert.Equal(new[] { "geometry" }, c.EnabledSectionIds);
        Assert.Equal((2, 3), (c.ActiveDifficulty.Level, c.ActiveDifficulty.CorrectStreak));
        Record(c, true, 1);
        Assert.Equal(3, c.ActiveDifficulty.Level);

        var strict = new QuestionCatalog(GameConfig.Default, Packs(), Order, new Random(1));
        strict.RestoreProgress(snapshot);
        Assert.Equal(Order, strict.EnabledSectionIds);
        Assert.Equal(1, strict.ActiveDifficulty.Level);
    }

    [Fact]
    public void Restore_drops_unknown_ids_and_resets_missing_sections()
    {
        var c = Catalog();
        c.EnableOnly("algebra");
        Record(c, true, 4);

        c.RestoreProgress(new QuizProgressSnapshot(new[] { "geometry" }, new[]
        {
            new SectionProgressSnapshot("geometry", 1, 0, 0, Used((1, new[] { "c1", "gone" }), (2, new[] { "c1" }))),
        }));

        var progress = c.CaptureProgress();
        var geometry = Section(progress, "geometry");
        Assert.Equal(new[] { "c1" }, geometry.UsedQuestionIdsByLevel[1]);
        Assert.False(geometry.UsedQuestionIdsByLevel.ContainsKey(2));
        Assert.Equal(1, Section(progress, "algebra").Level);
    }
}
