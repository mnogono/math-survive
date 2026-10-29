using System;
using System.Collections.Generic;
using Godot;
using MathSurvive.Core;
using MathSurvive.Core.Layout;
using MathSurvive.Core.Session;

namespace MathSurvive.Game;

/// <summary>
/// «Кино» орды: зомби выходят из-за правого края и идут к персонажу, персонаж стреляет сам через
/// <see cref="GameSession.Fire"/>. По окончании сам вызывает <see cref="GameSession.FinishHorde"/>.
/// Координаты зомби — физические px окна. Зомби не кликабельны (в зоны кликов оверлея не регистрируются).
/// </summary>
public partial class HordeDirector : Node3D
{
    private const float SpawnOffsetLogicalPx = 40;
    private const float StopOffsetLogicalPx = 30;
    private const double FlashSec = 0.08;

    private readonly List<ZombieView> _zombies = new();
    private GameSession _session = null!;
    private Stage _stage = null!;
    private MeshInstance3D _flash = null!;

    private float _spawnX, _stopX, _fireLineX, _feetY, _speedPx;
    private bool _active;
    private int _toSpawn, _killed;
    private double _spawnCooldown, _fireCooldown, _flashLeft;

    public void Setup(GameSession session, Stage stage, PlayerView player)
    {
        _session = session;
        _stage = stage;

        var window = GetWindow().Size;
        var scale = UiScale.Value;
        var feet = Placement.PlayerFeet(window.X, window.Y, scale);
        _spawnX = window.X + SpawnOffsetLogicalPx * scale;
        _stopX = (float)feet.x + StopOffsetLogicalPx * scale;
        _fireLineX = (float)Placement.FireLineX(window.X, window.Y, scale, session.Config.FireRangeFraction);
        _feetY = (float)feet.y;
        _speedPx = (float)session.Config.ZombieSpeedPx * scale;

        _flash = new MeshInstance3D
        {
            Mesh = new SphereMesh
            {
                Radius = 0.05f,
                Height = 0.1f,
                Material = new StandardMaterial3D
                {
                    AlbedoColor = new Color(1f, 0.85f, 0.2f),
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                },
            },
            Position = player.MuzzleWorld + new Vector3(0.03f, 0, 0.05f),
            Visible = false,
        };
        AddChild(_flash);
    }

    public void Begin(HordeInfo info)
    {
        Clear();
        _toSpawn = info.ZombieCount;
        _killed = 0;
        _spawnCooldown = 0;
        _fireCooldown = 0;
        _active = true;
    }

    /// <summary>Убрать оставшихся зомби (после game over, перед новой ордой).</summary>
    public void Clear()
    {
        _active = false;
        foreach (var zombie in _zombies)
            zombie.QueueFree();
        _zombies.Clear();
        _flash.Visible = false;
    }

    public override void _Process(double delta)
    {
        if (_session == null)
            return;
        delta = Math.Min(delta, _session.Config.MaxFrameDeltaSec);
        UpdateFlash(delta);
        if (!_active)
            return;

        Spawn(delta);
        Walk((float)delta);
        Shoot(delta);
        CheckEnd();
    }

    private void Spawn(double delta)
    {
        _spawnCooldown -= delta;
        if (_toSpawn == 0 || _spawnCooldown > 0)
            return;
        var zombie = new ZombieView();
        AddChild(zombie);
        zombie.Place(_stage, _spawnX, _feetY);
        _zombies.Add(zombie);
        _toSpawn--;
        _spawnCooldown = _session.Config.ZombieSpawnIntervalSec;
    }

    private void Walk(float delta)
    {
        foreach (var zombie in _zombies)
        {
            if (zombie.AtPlayer)
                continue;
            var x = Mathf.Max(_stopX, zombie.XPx - _speedPx * delta);
            zombie.Place(_stage, x, _feetY);
            zombie.AtPlayer = x <= _stopX;
        }
    }

    private void Shoot(double delta)
    {
        _fireCooldown -= delta;
        if (_fireCooldown > 0)
            return;
        var target = Nearest();
        if (target == null || target.XPx >= _fireLineX)
            return;

        _fireCooldown = _session.Config.FireIntervalSec;
        var outcome = _session.Fire();
        if (outcome == ShotOutcome.NoAmmo)
            return;
        _flash.Visible = true;
        _flashLeft = FlashSec;
        if (outcome == ShotOutcome.Hit)
        {
            _zombies.Remove(target);
            target.QueueFree();
            _killed++;
        }
    }

    private void CheckEnd()
    {
        if (_session.Combat.Ammo == 0 && _zombies.Exists(z => z.AtPlayer))
            Finish(survived: false);
        else if (_toSpawn == 0 && _zombies.Count == 0)
            Finish(survived: true);
    }

    private void Finish(bool survived)
    {
        _active = false;
        _session.FinishHorde(survived, _killed);
    }

    private ZombieView? Nearest()
    {
        ZombieView? nearest = null;
        foreach (var zombie in _zombies)
            if (nearest == null || zombie.XPx < nearest.XPx)
                nearest = zombie;
        return nearest;
    }

    private void UpdateFlash(double delta)
    {
        if (!_flash.Visible)
            return;
        _flashLeft -= delta;
        if (_flashLeft <= 0)
            _flash.Visible = false;
    }
}
