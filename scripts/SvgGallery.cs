using System.Linq;
using Godot;

namespace MathSurvive.Game;

/// <summary>Витрина: все SVG тестового пакета на светлом фоне — инструмент проверки пакетов.</summary>
public partial class SvgGallery : Control
{
    private const float FormulaScale = 1.5f;

    public override void _Ready()
    {
        var dir = PacksRoot.Path + "/math/test/svg";
        var files = DirAccess.GetFilesAt(dir);
        if (files.Length == 0)
            GD.PushError($"SvgGallery: нет файлов в {dir} ({DirAccess.GetOpenError()})");

        var rows = GetNode<VBoxContainer>("Scroll/Rows");
        // Один ряд на вопрос: условие (-q) первым, затем варианты a0..a3.
        var groups = files
            .Where(f => f.EndsWith(".svg"))
            .OrderBy(f => f.EndsWith("-q.svg") ? 0 : 1)
            .ThenBy(f => f, System.StringComparer.Ordinal)
            .GroupBy(f => f[..f.LastIndexOf('-')])
            .OrderBy(g => g.Key, System.StringComparer.Ordinal);

        foreach (var group in groups)
        {
            var row = new HFlowContainer();
            row.AddThemeConstantOverride("h_separation", 32);
            row.AddThemeConstantOverride("v_separation", 8);
            foreach (var file in group)
            {
                row.AddChild(new TextureRect
                {
                    Texture = SvgTextures.Load(dir + "/" + file, FormulaScale),
                    StretchMode = TextureRect.StretchModeEnum.KeepCentered,
                    TooltipText = file,
                });
            }
            rows.AddChild(row);
        }
    }
}
