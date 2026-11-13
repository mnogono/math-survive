using System;
using System.Collections.Generic;
using Godot;
using MathSurvive.Core;
using MathSurvive.Core.Progress;
using MathSurvive.Core.Questions;

namespace MathSurvive.Game;

/// <summary>
/// <c>save.json</c>: в редакторе — в user://, в собранной игре — рядом с exe (как <see cref="PacksRoot"/>).
/// Ошибки чтения дают значения по умолчанию, ошибки записи только логируются.
/// </summary>
public static class ProgressSaveStore
{
    private static ProgressFileStore? _store;

    private static ProgressFileStore Store => _store ??= new ProgressFileStore(OS.HasFeature("editor")
        ? ProjectSettings.GlobalizePath("user://save.json")
        : OS.GetExecutablePath().GetBaseDir() + "/save.json");

    public static QuizProgressSnapshot? Load(IReadOnlyList<QuestionPack> packs, GameConfig config)
    {
        var snapshot = Store.Load(packs, config);
        GD.Print(snapshot == null ? $"save: нет действительного {Store.Path}, прогресс по умолчанию" : $"save: загружен {Store.Path}");
        return snapshot;
    }

    public static void Save(QuizProgressSnapshot snapshot)
    {
        try
        {
            Store.Save(snapshot);
        }
        catch (Exception e)
        {
            GD.PushError($"Не удалось сохранить прогресс в {Store.Path}: {e.Message}");
        }
    }
}
