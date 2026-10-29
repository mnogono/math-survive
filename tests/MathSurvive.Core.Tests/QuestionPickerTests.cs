using System;
using System.Collections.Generic;
using System.Linq;
using MathSurvive.Core.Questions;
using Xunit;

namespace MathSurvive.Core.Tests;

public class QuestionPickerTests
{
    private static Question Q(string id, int level) => new(id, level, "p", null,
        new[] { new QuestionOption("a", null), new QuestionOption("b", null), new QuestionOption("c", null), new QuestionOption("d", null) }, 0);

    [Fact]
    public void Picks_all_level_questions_before_repeating()
    {
        var picker = new QuestionPicker(new[] { Q("a", 1), Q("b", 1), Q("c", 1) }, new Random(7));
        var used = new HashSet<string>();

        var ids = new[] { picker.Next(1, used).Id, picker.Next(1, used).Id, picker.Next(1, used).Id };
        Assert.Equal(3, ids.Distinct().Count());

        picker.Next(1, used);
        Assert.Single(used);
    }

    [Fact]
    public void Falls_back_lower_levels_first()
    {
        var picker = new QuestionPicker(new[] { Q("a", 1), Q("d", 4) }, new Random(1));
        Assert.Equal(1, picker.Next(3, new HashSet<string>()).Level);

        var onlyHigh = new QuestionPicker(new[] { Q("d", 4) }, new Random(1));
        Assert.Equal(4, onlyHigh.Next(2, new HashSet<string>()).Level);
    }

    [Fact]
    public void Find_returns_by_id_or_null()
    {
        var picker = new QuestionPicker(new[] { Q("a", 1) }, new Random(1));
        Assert.Equal("a", picker.Find("a")!.Id);
        Assert.Null(picker.Find("zzz"));
    }

    [Fact]
    public void Empty_pack_throws()
    {
        var picker = new QuestionPicker(Array.Empty<Question>(), new Random(1));
        Assert.Throws<InvalidOperationException>(() => picker.Next(1, new HashSet<string>()));
    }
}
