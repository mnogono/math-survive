using Godot;

namespace MathSurvive.Game;

public static class ModelVisual
{
    public static Node3D Load(string path, float height, float yawDegrees)
    {
        var model = Instantiate(path);
        model.RotationDegrees = new Vector3(0, yawDegrees, 0);
        var bounds = Bounds(model);
        ScaleAndCenter(model, height / bounds.Size.Y);
        return model;
    }

    public static Node3D LoadLongest(string path, float length)
    {
        var model = Instantiate(path);
        var bounds = Bounds(model);
        var longest = Mathf.Max(bounds.Size.X, Mathf.Max(bounds.Size.Y, bounds.Size.Z));
        ScaleAndCenter(model, length / longest);
        return model;
    }

    public static AnimationPlayer? FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer player)
            return player;
        foreach (var child in node.GetChildren())
            if (FindAnimationPlayer(child) is { } found)
                return found;
        return null;
    }

    public static void Play(AnimationPlayer? player, string clip, bool loop = false)
    {
        if (player == null)
            return;
        if (!player.HasAnimation(clip))
        {
            GD.PushWarning($"Отсутствует анимация '{clip}' у {player.GetPath()}");
            return;
        }
        player.GetAnimation(clip).LoopMode = loop
            ? Animation.LoopModeEnum.Linear
            : Animation.LoopModeEnum.None;
        player.Play(clip);
    }

    private static Node3D Instantiate(string path)
        => GD.Load<PackedScene>(path).Instantiate<Node3D>();

    private static void ScaleAndCenter(Node3D model, float factor)
    {
        model.Scale *= factor;
        var bounds = Bounds(model);
        var center = bounds.GetCenter();
        model.Position -= new Vector3(center.X, bounds.Position.Y, center.Z);
    }

    private static Aabb Bounds(Node3D root)
    {
        var result = new Aabb();
        var hasBounds = false;
        Visit(root, Transform3D.Identity, ref result, ref hasBounds);
        return result;
    }

    private static void Visit(Node node, Transform3D parent, ref Aabb bounds, ref bool hasBounds)
    {
        var transform = node is Node3D node3D ? parent * node3D.Transform : parent;
        if (node is MeshInstance3D mesh)
        {
            var meshBounds = transform * mesh.GetAabb();
            bounds = hasBounds ? bounds.Merge(meshBounds) : meshBounds;
            hasBounds = true;
        }
        foreach (var child in node.GetChildren())
            Visit(child, transform, ref bounds, ref hasBounds);
    }
}
