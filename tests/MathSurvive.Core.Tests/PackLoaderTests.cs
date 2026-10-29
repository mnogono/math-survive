using MathSurvive.Core.Questions;
using Xunit;

namespace MathSurvive.Core.Tests;

public class PackLoaderTests
{
    private const string Section = "{\"id\":\"fractions\",\"title\":\"Дроби\",\"levels\":10}";

    private const string SvgQ = """
        {"id":"fractions-L3-0042","level":3,"prompt":"Вычислите:","prompt_svg":"svg/fractions-L3-0042-q.svg",
         "options":[{"svg":"svg/fractions-L3-0042-a0.svg"},{"svg":"svg/fractions-L3-0042-a1.svg"},
                    {"svg":"svg/fractions-L3-0042-a2.svg"},{"svg":"svg/fractions-L3-0042-a3.svg"}],
         "correct":2}
        """;

    private const string TextQ = """
        {"id":"t1","level":1,"prompt":"2+2=?",
         "options":[{"text":"3"},{"text":"4"},{"text":"5"},{"text":"6"}],"correct":1}
        """;

    private static void AssertRejects(string question, string id)
    {
        var ex = Assert.Throws<PackFormatException>(() => PackLoader.Parse(Section, "[" + question + "]"));
        Assert.Contains(id, ex.Message);
    }

    [Fact]
    public void Parses_svg_and_text_questions()
    {
        var pack = PackLoader.Parse(Section, "[" + SvgQ + "," + TextQ + "]");

        Assert.Equal("fractions", pack.Section.Id);
        Assert.Equal("Дроби", pack.Section.Title);
        Assert.Equal(10, pack.Section.Levels);
        Assert.Equal(2, pack.Questions.Count);
        var q = pack.Questions[0];
        Assert.Equal(3, q.Level);
        Assert.Equal("svg/fractions-L3-0042-q.svg", q.PromptSvg);
        Assert.Equal("svg/fractions-L3-0042-a2.svg", q.Options[2].Svg);
        Assert.Equal(2, q.Correct);
        Assert.Equal("3", pack.Questions[1].Options[0].Text);
        Assert.Equal(1, pack.Questions[1].Correct);
    }

    [Fact]
    public void Rejects_three_options() =>
        AssertRejects("""{"id":"bad3","level":1,"prompt":"x","options":[{"text":"a"},{"text":"b"},{"text":"c"}],"correct":0}""", "bad3");

    [Fact]
    public void Rejects_correct_out_of_range() =>
        AssertRejects("""{"id":"badc","level":1,"prompt":"x","options":[{"text":"a"},{"text":"b"},{"text":"c"},{"text":"d"}],"correct":4}""", "badc");

    [Fact]
    public void Rejects_option_without_text_and_svg() =>
        AssertRejects("""{"id":"bado","level":1,"prompt":"x","options":[{"text":"a"},{"text":"b"},{},{"text":"d"}],"correct":0}""", "bado");

    [Fact]
    public void Rejects_question_without_prompt() =>
        AssertRejects("""{"id":"badp","level":1,"options":[{"text":"a"},{"text":"b"},{"text":"c"},{"text":"d"}],"correct":0}""", "badp");

    [Fact]
    public void Rejects_duplicate_id()
    {
        var ex = Assert.Throws<PackFormatException>(() => PackLoader.Parse(Section, "[" + TextQ + "," + TextQ + "]"));
        Assert.Contains("t1", ex.Message);
    }
}
