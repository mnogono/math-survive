using System;
using System.Collections.Generic;
using Godot;

namespace MathSurvive.Game;

public partial class BloodBurst : Node3D
{
    private readonly List<(MeshInstance3D Mesh, Vector3 Velocity)> _shards = new();
    private readonly Random _random = new();
    private double _elapsed;

    public override void _Ready()
    {
        var mesh = new BoxMesh
        {
            Size = new Vector3(0.045f, 0.035f, 0.04f),
            Material = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.65f, 0.015f, 0.02f),
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
        };
        for (var i = 0; i < 16; i++)
        {
            var shard = new MeshInstance3D { Mesh = mesh };
            AddChild(shard);
            var velocity = new Vector3(
                (float)(_random.NextDouble() * 1.2 - 0.2),
                (float)(_random.NextDouble() * 0.9 + 0.4),
                (float)(_random.NextDouble() * 0.8 - 0.4));
            _shards.Add((shard, velocity));
        }
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        for (var i = 0; i < _shards.Count; i++)
        {
            var (mesh, velocity) = _shards[i];
            velocity += new Vector3(0, -2.8f * (float)delta, 0);
            mesh.Position += velocity * (float)delta;
            mesh.Rotation += velocity * (float)delta * 4;
            _shards[i] = (mesh, velocity);
        }
        if (_elapsed >= 0.75)
            QueueFree();
    }
}
