using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    private static (QuestionPack Pack, string Dir) LoadPack(string id)
    {
        var packDir = Path.Combine(RepoRoot(), "packs", "math", id);
        var pack = PackLoader.Parse(
            File.ReadAllText(Path.Combine(packDir, "section.json")),
            File.ReadAllText(Path.Combine(packDir, "questions.json")));
        return (pack, packDir);
    }

    private static void AssertSvgFilesExist(QuestionPack pack, string packDir)
    {
        foreach (var q in pack.Questions)
        {
            var paths = q.Options.Select(o => o.Svg).Append(q.PromptSvg).Append(q.PromptImage).OfType<string>();
            foreach (var relative in paths)
            {
                var file = Path.Combine(packDir, relative);
                Assert.True(File.Exists(file), relative);
                // Godot не рисует <text> в SVG: все надписи должны быть контурами.
                Assert.DoesNotContain("<text", File.ReadAllText(file));
            }
        }
    }

    [Fact]
    public void Test_pack_shows_prompt_image()
    {
        var (pack, packDir) = LoadPack("test");
        var q = Assert.Single(pack.Questions, q => q.PromptImage is not null);

        Assert.Equal("test-L3-03", q.Id);
        Assert.NotNull(q.Prompt);
        Assert.NotNull(q.PromptSvg);
        Assert.True(File.Exists(Path.Combine(packDir, q.PromptImage!)));
    }

    [Fact]
    public void Parses_built_test_pack()
    {
        var (pack, packDir) = LoadPack("test");

        Assert.Equal(9, pack.Questions.Count);
        Assert.Equal(3, pack.Section.Levels);
        AssertSvgFilesExist(pack, packDir);
    }

    [Fact]
    public void All_pack_question_ids_are_unique_across_sections()
    {
        var ids = new[] { "test", "arithmetic", "algebra", "geometry", "analysis" }
            .SelectMany(id => LoadPack(id).Pack.Questions.Select(q => q.Id))
            .ToList();

        Assert.Equal(409, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData("arithmetic", 40, 35, 25)]
    [InlineData("algebra", 40, 35, 25)]
    [InlineData("analysis", 40, 35, 25)]
    [InlineData("geometry", 40, 35, 25)]
    public void Section_pack_matches_source_and_distribution(string id, int l1, int l2, int l3)
    {
        var (pack, packDir) = LoadPack(id);

        Assert.Equal(id, pack.Section.Id);
        Assert.Equal(3, pack.Section.Levels);
        Assert.Equal(l1 + l2 + l3, pack.Questions.Count);
        Assert.Equal(l1, pack.Questions.Count(q => q.Level == 1));
        Assert.Equal(l2, pack.Questions.Count(q => q.Level == 2));
        Assert.Equal(l3, pack.Questions.Count(q => q.Level == 3));
        Assert.Equal(pack.Questions.Count, pack.Questions.Select(q => q.Id).Distinct().Count());
        Assert.All(pack.Questions, q => Assert.StartsWith($"{id}-L{q.Level}-", q.Id));
        AssertSvgFilesExist(pack, packDir);

        var srcPath = Path.Combine(RepoRoot(), "packs-src", "math", id, "questions.src.json");
        using var src = JsonDocument.Parse(File.ReadAllText(srcPath));
        Assert.Equal(pack.Questions.Count, src.RootElement.GetArrayLength());
        foreach (var q in src.RootElement.EnumerateArray())
        {
            var options = q.GetProperty("options").EnumerateArray()
                .Select(o => o.TryGetProperty("tex", out var t) ? "tex:" + t.GetString() : "text:" + o.GetProperty("text").GetString())
                .Select(s => Regex.Replace(s, @"\s+", ""))
                .ToList();
            Assert.True(options.Distinct().Count() == 4, q.GetProperty("id").GetString());
        }
    }
}
