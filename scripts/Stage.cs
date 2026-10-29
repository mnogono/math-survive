using Godot;

namespace MathSurvive.Game;

/// <summary>
/// 3D-сцена с ортокамерой: 1 мировая единица = 100 логических px. Мировой (0, 0) — левый нижний угол окна, Y вверх.
/// Пиксели — физические px окна (начало — левый верхний угол).
/// </summary>
public partial class Stage : Node3D
{
    public const float LogicalPxPerUnit = 100f;

    private Vector2 _windowPx;
    private float _pxPerUnit;

    public override void _Ready()
    {
        _windowPx = GetWindow().Size;
        _pxPerUnit = LogicalPxPerUnit * UiScale.Value;

        var camera = new Camera3D
        {
            Projection = Camera3D.ProjectionType.Orthogonal,
            KeepAspect = Camera3D.KeepAspectEnum.Height,
            Size = _windowPx.Y / _pxPerUnit,
            Position = new Vector3(_windowPx.X / 2 / _pxPerUnit, _windowPx.Y / 2 / _pxPerUnit, 10),
            Near = 0.1f,
            Far = 20f,
        };
        AddChild(camera);
        camera.MakeCurrent();

        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-35, 30, 0) });
        // Рассеянный свет, чтобы теневые стороны не были чёрными; фон остаётся прозрачным (цвет очистки).
        AddChild(new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.ClearColor,
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = Colors.White,
                AmbientLightEnergy = 0.5f,
            },
        });
    }

    public Vector3 PixelToWorld(Vector2 px)
        => new(px.X / _pxPerUnit, (_windowPx.Y - px.Y) / _pxPerUnit, 0);

    public Vector2 WorldToPixel(Vector3 w)
        => new(w.X * _pxPerUnit, _windowPx.Y - w.Y * _pxPerUnit);
}
