using Godot;

namespace MathSurvive.Game;

/// <summary>Корень пакетов вопросов: в редакторе res://packs, в собранной игре — папка packs рядом с exe.</summary>
public static class PacksRoot
{
    private static string? _path;

    public static string Path => _path ??= OS.HasFeature("editor")
        ? "res://packs"
        : OS.GetExecutablePath().GetBaseDir() + "/packs";
}
