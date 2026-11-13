using System;
using System.Collections.Generic;
using System.Linq;
using MathSurvive.Core.Questions;
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
    public void Starts_random_with_first_section_active()
    {
        var c = Catalog();
        Assert.Null(c.SelectedSectionId);
        Assert.Equal("arithmetic", c.ActivePack.Section.Id);
    }

    [Fact]
    public void Random_mode_cycles_sections_in_order()
    {
        var c = Catalog();
        var expected = new[] { ("a1", "arithmetic"), ("b1", "algebra"), ("c1", "geometry"), ("d1", "analysis"), ("a1", "arithmetic") };

        foreach (var (id, section) in expected)
        {
            Assert.Equal(id, c.NextQuestion().Id);
            Assert.Equal(section, c.ActivePack.Section.Id);
        }
    }

    [Fact]
    public void All_sections_use_the_given_config()
    {
        var c = Catalog();
        foreach (var section in Order)
        {
            c.SelectSection(section);
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
        c.SelectSection("algebra");
        Record(c, true, 4);

        Assert.Equal("b2", c.NextQuestion().Id);
        Assert.Equal("b2", c.NextQuestion().Id);

        Record(c, true, 4);
        Assert.Equal("b3", c.NextQuestion().Id);

        Record(c, false, 3);
        Assert.Equal("b2", c.NextQuestion().Id);
    }

    [Fact]
    public void Select_section_keeps_its_independent_used_ids()
    {
        var c = Catalog(geometryExtraL1: 2);
        c.SelectSection("geometry");
        var first = c.NextQuestion().Id;
        var second = c.NextQuestion().Id;

        c.SelectSection("arithmetic");
        Assert.Equal("arithmetic", c.ActivePack.Section.Id);
        Assert.Equal("a1", c.NextQuestion().Id);

        c.SelectSection("geometry");
        Assert.Equal("geometry", c.SelectedSectionId);
        var third = c.NextQuestion().Id;
        Assert.Equal(3, new[] { first, second, third }.Distinct().Count());
    }

    [Fact]
    public void Select_random_restarts_cycle_from_first_section()
    {
        var c = Catalog();
        c.NextQuestion();
        c.NextQuestion();
        c.SelectSection("analysis");
        Assert.Equal("d1", c.NextQuestion().Id);

        c.SelectRandom();
        Assert.Null(c.SelectedSectionId);
        Assert.Equal("a1", c.NextQuestion().Id);
        Assert.Equal("b1", c.NextQuestion().Id);
    }

    [Fact]
    public void Specific_section_does_not_move_random_cursor()
    {
        var c = Catalog();
        c.SelectSection("geometry");
        c.NextQuestion();
        c.NextQuestion();
        c.SelectRandom();
        Assert.Equal("a1", c.NextQuestion().Id);
    }

    [Fact]
    public void Find_active_searches_only_active_pack()
    {
        var c = Catalog();
        c.SelectSection("algebra");
        Assert.Equal("b2", c.FindActive("b2")!.Id);
        Assert.Null(c.FindActive("a1"));
    }

    [Fact]
    public void Rejects_invalid_setup()
    {
        var rng = new Random(1);
        Assert.Throws<ArgumentException>(() => new QuestionCatalog(Config, new[] { Pack("arithmetic", "a"), Pack("arithmetic", "b") }, new[] { "arithmetic" }, rng));
        Assert.Throws<ArgumentException>(() => new QuestionCatalog(Config, Packs(), Array.Empty<string>(), rng));
        Assert.Throws<ArgumentException>(() => new QuestionCatalog(Config, Packs(), new[] { "algebra", "algebra" }, rng));
        Assert.Throws<ArgumentException>(() => new QuestionCatalog(Config, Packs(), new[] { "algebra", "topology" }, rng));
        Assert.Throws<ArgumentException>(() => Catalog().SelectSection("topology"));
    }
}
