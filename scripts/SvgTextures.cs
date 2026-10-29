using System.Collections.Generic;
using Godot;

namespace MathSurvive.Game;

/// <summary>Загрузка SVG формул в текстуры с учётом масштаба интерфейса; кэш по (путь, масштаб, вид).</summary>
public static class SvgTextures
{
    private static readonly Dictionary<(string Path, float Scale, bool Ui), ImageTexture> Cache = new();

    /// <summary>Пиксели и размер текстуры — nominal × scale × UiScale (для холста без ContentScaleFactor).</summary>
    public static ImageTexture Load(string absPath, float scale) => Get(absPath, scale, ui: false);

    /// <summary>
    /// Для интерфейса под <c>ContentScaleFactor = UiScale</c>: пиксели те же, что у <see cref="Load"/>, а размер
    /// текстуры (логические px) — без UiScale, чтобы холст не растягивал её второй раз и формула оставалась чёткой.
    /// </summary>
    public static ImageTexture LoadUi(string absPath, float scale) => Get(absPath, scale, ui: true);

    private static ImageTexture Get(string absPath, float scale, bool ui)
    {
        if (Cache.TryGetValue((absPath, scale, ui), out var cached))
            return cached;

        var img = new Image();
        var err = img.LoadSvgFromString(FileAccess.GetFileAsString(absPath), scale * UiScale.Value);
        if (err != Error.Ok)
        {
            GD.PushError($"SvgTextures: не удалось загрузить {absPath}: {err}");
            img = Placeholder();
        }

        var texture = ImageTexture.CreateFromImage(img);
        if (ui)
        {
            var size = img.GetSize();
            texture.SetSizeOverride(new Vector2I(
                Mathf.Max(1, Mathf.RoundToInt(size.X / UiScale.Value)),
                Mathf.Max(1, Mathf.RoundToInt(size.Y / UiScale.Value))));
        }
        Cache[(absPath, scale, ui)] = texture;
        return texture;
    }

    private static Image Placeholder()
    {
        var img = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8);
        img.Fill(Colors.Magenta);
        return img;
    }
}
