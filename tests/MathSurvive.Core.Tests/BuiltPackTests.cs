using System;
using System.IO;
using MathSurvive.Core.Questions;
using Xunit;

namespace MathSurvive.Core.Tests;

public class BuiltPackTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MathSurvive.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Не найден MathSurvive.sln");
    }

    [Fact]
    public void Parses_built_test_pack()
    {
        var packDir = Path.Combine(RepoRoot(), "packs", "math", "test");
        var pack = PackLoader.Parse(
            File.ReadAllText(Path.Combine(packDir, "section.json")),
            File.ReadAllText(Path.Combine(packDir, "questions.json")));

        Assert.Equal(9, pack.Questions.Count);
        Assert.Equal(3, pack.Section.Levels);

        foreach (var q in pack.Questions)
        {
            if (q.PromptSvg is not null)
                Assert.True(File.Exists(Path.Combine(packDir, q.PromptSvg)), q.PromptSvg);
            foreach (var o in q.Options)
                if (o.Svg is not null)
                    Assert.True(File.Exists(Path.Combine(packDir, o.Svg)), o.Svg);
        }
    }
}
