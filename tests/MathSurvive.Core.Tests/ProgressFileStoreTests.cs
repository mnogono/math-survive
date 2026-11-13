using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MathSurvive.Core.Progress;
using MathSurvive.Core.Questions;
using Xunit;

namespace MathSurvive.Core.Tests;

public sealed class ProgressFileStoreTests : IDisposable
{
    private static readonly GameConfig Custom = GameConfig.Default with { LevelUpStreak = 4, LevelDownStreak = 3 };

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "MathSurviveTests", Guid.NewGuid().ToString("N"));
    private string SavePath => Path.Combine(_dir, "save.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static Question Q(string id, int level) => new(id, level, "p", null,
        new[] { new QuestionOption("a", null), new QuestionOption("b", null), new QuestionOption("c", null), new QuestionOption("d", null) }, 0);

    private static QuestionPack[] Packs() => new[] { "arithmetic", "geometry" }.Select(id => new QuestionPack(new SectionInfo(id, id, 3),
        Enumerable.Range(1, 3).Select(l => Q($"{id}-{l}", l)).ToList())).ToArray();

    private static QuizProgressSnapshot Sample(int correctStreak = 2) => new("geometry", new[]
    {
        new SectionProgressSnapshot("arithmetic", 1, 0, 0, new Dictionary<int, IReadOnlyList<string>>()),
        new SectionProgressSnapshot("geometry", 3, correctStreak, 0, new Dictionary<int, IReadOnlyList<string>>
        {
            [2] = new[] { "geometry-2" },
            [3] = new[] { "geometry-3" },
        }),
    });

    [Fact]
    public void Missing_file_gives_null()
        => Assert.Null(new ProgressFileStore(SavePath).Load(Packs(), Custom));

    [Fact]
    public void Broken_file_gives_null()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SavePath, "{ не json");
        Assert.Null(new ProgressFileStore(SavePath).Load(Packs(), Custom));
    }

    [Fact]
    public void Save_then_load_round_trips()
    {
        var store = new ProgressFileStore(SavePath);
        store.Save(Sample(correctStreak: 3));

        Assert.True(File.Exists(SavePath));
        Assert.False(File.Exists(store.TempPath));
        Assert.Equal(_dir, Path.GetDirectoryName(store.TempPath));
        Assert.Equivalent(Sample(correctStreak: 3), store.Load(Packs(), Custom), strict: true);
    }

    [Fact]
    public void Save_replaces_existing_file()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SavePath, "старое содержимое, длиннее нового сохранения ............................................................................................................................................................................................................................................................................................................................................................................................................");
        var store = new ProgressFileStore(SavePath);

        store.Save(Sample());

        Assert.Equivalent(Sample(), store.Load(Packs(), Custom), strict: true);
        Assert.Equal(new[] { SavePath }, Directory.GetFiles(_dir));
    }

    [Fact]
    public void Load_rejects_streaks_beyond_config()
    {
        var store = new ProgressFileStore(SavePath);
        store.Save(Sample(correctStreak: 3));
        Assert.Null(store.Load(Packs(), GameConfig.Default));
    }

    [Fact]
    public void Failed_save_keeps_previous_file()
    {
        var store = new ProgressFileStore(SavePath);
        store.Save(Sample());
        using (File.Open(store.TempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            Assert.ThrowsAny<IOException>(() => store.Save(Sample(correctStreak: 1)));

        Assert.Equivalent(Sample(), store.Load(Packs(), Custom), strict: true);
    }
}
