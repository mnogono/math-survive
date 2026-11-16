using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MathSurvive.Core.Layout;

namespace MathSurvive.Game;

/// <summary>
/// Комната выбора разделов: стена в нижней полосе окна (3D, за персонажем), таблички разделов и монеты-переключатели (2D).
/// Клик по стене сообщает точку выстрела (<see cref="Clicked"/>); следы пуль остаются на стене и табличках.
/// </summary>
public partial class RoomView : Node3D
{
    private static readonly (string Id, string Symbol, string Title)[] SectionSpecs =
    {
        ("arithmetic", "+ −", "Арифметика"),
        ("algebra", "x²", "Алгебра"),
        ("geometry", "△", "Геометрия"),
        ("analysis", "∫", "Математический анализ"),
    };

    private const float WallDepth = 0.6f;
    private const float BaseboardLogicalPx = 8f;
    /// <summary>Следы пуль — перед стеной и плинтусом, но за персонажем и зомби.</summary>
    private const float HoleZ = -WallDepth + 0.08f;
    private const int MaxWallHoles = 80;

    private readonly Mesh _holeMesh = Disc(0.03f, 12, new Color(0.1f, 0.08f, 0.07f));
    private readonly Mesh _chipMesh = Disc(0.06f, 7, new Color(0.56f, 0.52f, 0.46f));

    private readonly List<RoomIconView> _plates = new();
    private readonly List<CoinView> _coins = new();
    private readonly Queue<Node3D> _wallHoles = new();
    private readonly Random _random = new();
    private Stage _stage = null!;
    private Vector2I _window;
    private Rect2 _wall;
    private bool _interactive = true;

    /// <summary>Клик по стене: точка выстрела, физические px окна.</summary>
    public event Action<Vector2>? Clicked;

    public IEnumerable<string> SectionIds => SectionSpecs.Select(s => s.Id);

    /// <summary>Ноги персонажа (физические px окна).</summary>
    public Vector2 PlayerFeetPx
    {
        get
        {
            var (x, y) = RoomLayout.PlayerFeet(_window.X, _window.Y);
            return new Vector2((float)x, (float)y);
        }
    }

    /// <summary>Зона клика для <see cref="DesktopOverlay"/> — вся стена; <c>null</c>, пока стрелять нельзя.</summary>
    public Rect2? HitArea() => Visible && _interactive ? _wall : null;

    public void Setup(Stage stage)
    {
        _stage = stage;
        _window = GetWindow().Size;
        var scale = UiScale.Value;
        var wallTop = (float)RoomLayout.WallTop(_window.Y, scale);
        _wall = new Rect2(0, wallTop, _window.X, _window.Y - wallTop);

        AddChild(Band(wallTop, _window.Y, new Color(0.80f, 0.76f, 0.70f), -WallDepth));
        AddChild(Band(_window.Y - BaseboardLogicalPx * scale, _window.Y, new Color(0.45f, 0.36f, 0.28f), -WallDepth + 0.05f));

        // Слой 0: под интерфейсом (облачка, квиз), но поверх 3D.
        var layer = new CanvasLayer { Layer = 0 };
        AddChild(layer);
        var slots = RoomLayout.SectionSlots(_window.X, _window.Y, scale);
        for (var i = 0; i < SectionSpecs.Length; i++)
        {
            var (id, symbol, title) = SectionSpecs[i];
            var (plateRect, coinRect) = slots[i];
            var plate = new RoomIconView { Id = id, Symbol = symbol, Title = title };
            layer.AddChild(plate);
            plate.Place(ToRect(plateRect));
            _plates.Add(plate);

            var coin = new CoinView { Id = id };
            layer.AddChild(coin);
            coin.Place(ToRect(coinRect));
            _coins.Add(coin);
        }
    }

    /// <summary>Состояние раздела без анимации (при запуске).</summary>
    public void SetEnabled(string id, bool enabled)
    {
        Coin(id).SetEnabled(enabled);
        Plate(id).SectionEnabled = enabled;
    }

    public void FlipCoin(string id, bool enabled)
    {
        Coin(id).Flip(enabled);
        Plate(id).SectionEnabled = enabled;
    }

    /// <summary>Переключить нельзя (последний включённый раздел): монета покачивается.</summary>
    public void RejectCoin(string id) => Coin(id).Wobble();

    public bool IsCoinBusy(string id) => Coin(id).IsBusy;

    /// <summary>Раздел, в монету которого попадает точка (физические px окна).</summary>
    public string? CoinAt(Vector2 px) => _coins.FirstOrDefault(c => c.HasPoint(px))?.Id;

    public Vector2 CoinCenterPx(string id) => Coin(id).CenterPx;

    /// <summary>След пули: на табличке, если точка на ней, иначе на стене.</summary>
    public void MarkHit(Vector2 px)
    {
        if (_plates.FirstOrDefault(p => p.ScreenRect.HasPoint(px)) is { } plate)
        {
            plate.AddHole(px);
            return;
        }
        AddWallHole(px);
    }

    public void SetInteractive(bool enabled)
    {
        _interactive = enabled;
        foreach (var plate in _plates)
            plate.Interactive = enabled;
        foreach (var coin in _coins)
            coin.Interactive = enabled;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (HitArea() is not { } wall || !DesktopOverlay.IsLeftClickIn(e, wall))
            return;
        GetViewport().SetInputAsHandled();
        Clicked?.Invoke(DesktopOverlay.CursorWindowPx());
    }

    private void AddWallHole(Vector2 px)
    {
        var hole = new Node3D
        {
            Position = _stage.PixelToWorld(px) + new Vector3(0, 0, HoleZ),
            Scale = Vector3.One * Mathf.Lerp(0.8f, 1.2f, (float)_random.NextDouble()),
        };
        // Диски цилиндров развёрнуты к камере; случайный поворот делает скол неровным.
        var facing = new Basis(Vector3.Right, Mathf.Pi / 2) * new Basis(Vector3.Up, (float)(_random.NextDouble() * Mathf.Tau));
        hole.AddChild(new MeshInstance3D { Mesh = _chipMesh, Basis = facing });
        hole.AddChild(new MeshInstance3D { Mesh = _holeMesh, Basis = facing, Position = new Vector3(0, 0, 0.01f) });
        AddChild(hole);
        _wallHoles.Enqueue(hole);
        while (_wallHoles.Count > MaxWallHoles)
            _wallHoles.Dequeue().QueueFree();
    }

    private CoinView Coin(string id)
        => _coins.FirstOrDefault(c => c.Id == id) ?? throw new ArgumentException($"Нет раздела «{id}»", nameof(id));

    private RoomIconView Plate(string id)
        => _plates.FirstOrDefault(p => p.Id == id) ?? throw new ArgumentException($"Нет раздела «{id}»", nameof(id));

    private static Rect2 ToRect(PixelRect r) => new(r.X, r.Y, r.W, r.H);

    private static Mesh Disc(float radius, int segments, Color color) => new CylinderMesh
    {
        TopRadius = radius,
        BottomRadius = radius,
        Height = 0.005f,
        RadialSegments = segments,
        Rings = 1,
        Material = new StandardMaterial3D
        {
            AlbedoColor = color,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
        },
    };

    /// <summary>Горизонтальная полоса на всю ширину окна между topPx и bottomPx.</summary>
    private MeshInstance3D Band(float topPx, float bottomPx, Color color, float z)
    {
        var topLeft = _stage.PixelToWorld(new Vector2(0, topPx));
        var bottomRight = _stage.PixelToWorld(new Vector2(_window.X, bottomPx));
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
