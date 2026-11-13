using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MathSurvive.Core.Layout;

namespace MathSurvive.Game;

/// <summary>
/// Комната выбора раздела: стена в нижней полосе окна (3D, за персонажем) и пять иконок на ней (2D).
/// Стена клики не принимает; зоны иконок регистрирует <see cref="Main"/>.
/// </summary>
public partial class RoomView : Node3D
{
    public const string RandomId = "random";

    private static readonly (string Id, string Symbol, string Title)[] IconSpecs =
    {
        (RandomId, "?", "Случайный"),
        ("arithmetic", "+ −", "Арифметика"),
        ("algebra", "x²", "Алгебра"),
        ("geometry", "△", "Геометрия"),
        ("analysis", "∫", "Математический анализ"),
    };

    private const float WallDepth = 0.6f;
    private const float BaseboardLogicalPx = 8f;

    private readonly List<RoomIconView> _icons = new();
    private Vector2I _window;

    public event Action<string>? IconClicked;

    public IReadOnlyList<RoomIconView> Icons => _icons;

    public void Setup(Stage stage)
    {
        _window = GetWindow().Size;
        var scale = UiScale.Value;
        var wallTop = (float)RoomLayout.WallTop(_window.Y, scale);

        AddChild(Band(stage, wallTop, _window.Y, new Color(0.80f, 0.76f, 0.70f), -WallDepth));
        AddChild(Band(stage, _window.Y - BaseboardLogicalPx * scale, _window.Y, new Color(0.45f, 0.36f, 0.28f), -WallDepth + 0.05f));

        // Слой 0: под интерфейсом (облачка, квиз), но поверх 3D.
        var layer = new CanvasLayer { Layer = 0 };
        AddChild(layer);
        var rects = RoomLayout.IconRects(_window.X, _window.Y, scale);
        for (var i = 0; i < IconSpecs.Length; i++)
        {
            var (id, symbol, title) = IconSpecs[i];
            var icon = new RoomIconView { Id = id, Symbol = symbol, Title = title };
            layer.AddChild(icon);
            var r = rects[i];
            icon.Place(new Rect2(r.X, r.Y, r.W, r.H));
            icon.Clicked += clicked => IconClicked?.Invoke(clicked);
            _icons.Add(icon);
        }
    }

    /// <summary>Ноги персонажа под иконкой (физические px окна).</summary>
    public Vector2 PlayerFeetPx(string id)
    {
        var (x, y) = RoomLayout.PlayerFeet(_window.X, _window.Y, UiScale.Value, IndexOf(id));
        return new Vector2((float)x, (float)y);
    }

    public bool HasIcon(string id) => _icons.Any(i => i.Id == id);

    public void SetSelected(string id)
    {
        foreach (var icon in _icons)
            icon.IsSelected = icon.Id == id;
    }

    public void SetInteractive(bool enabled)
    {
        foreach (var icon in _icons)
            icon.Interactive = enabled;
    }

    private int IndexOf(string id)
    {
        var index = _icons.FindIndex(i => i.Id == id);
        return index >= 0 ? index : throw new ArgumentException($"Нет иконки «{id}»", nameof(id));
    }

    /// <summary>Горизонтальная полоса на всю ширину окна между topPx и bottomPx.</summary>
    private MeshInstance3D Band(Stage stage, float topPx, float bottomPx, Color color, float z)
    {
        var topLeft = stage.PixelToWorld(new Vector2(0, topPx));
        var bottomRight = stage.PixelToWorld(new Vector2(_window.X, bottomPx));
        var size = new Vector3(bottomRight.X - topLeft.X, topLeft.Y - bottomRight.Y, 0.02f);
        return new MeshInstance3D
        {
            Mesh = new BoxMesh
            {
                Size = size,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = color,
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
            },
            Position = new Vector3(topLeft.X + size.X / 2, bottomRight.Y + size.Y / 2, z),
        };
    }
}
