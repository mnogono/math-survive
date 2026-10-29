using System.Collections.Generic;
using Godot;

namespace MathSurvive.Game;

/// <summary>Загрузка SVG формул в текстуры с учётом масштаба интерфейса; кэш по (путь, масштаб).</summary>
public static class SvgTextures
{
    private static readonly Dictionary<(string Path, float Scale), ImageTexture> Cache = new();

    public static ImageTexture Load(string absPath, float scale)
    {
        if (Cache.TryGetValue((absPath, scale), out var cached))
            return cached;

        var img = new Image();
        var err = img.LoadSvgFromString(FileAccess.GetFileAsString(absPath), scale * UiScale.Value);
        if (err != Error.Ok)
        {
            GD.PushError($"SvgTextures: не удалось загрузить {absPath}: {err}");
            img = Placeholder();
        }

        var texture = ImageTexture.CreateFromImage(img);
        Cache[(absPath, scale)] = texture;
        return texture;
    }

    private static Image Placeholder()
    {
        var img = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        img.Fill(Colors.Magenta);
        return img;
    }
}
