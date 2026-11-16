using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace MathSurvive.Game;

/// <summary>Отладочные аргументы командной строки (после "--"): --shot, --quit-after, --open-quiz, --question, --horde-now, --ammo, --fire.</summary>
public sealed class DebugArgs
{
    private static DebugArgs? _current;

    public static DebugArgs Current => _current ??= Parse(OS.GetCmdlineUserArgs());

    public string? ShotPath { get; private init; }
    public double? QuitAfterSec { get; private init; }
    public bool OpenQuiz { get; private init; }
    public string? QuestionId { get; private init; }
    public bool HordeNow { get; private init; }
    public int? Ammo { get; private init; }

    /// <summary>
    /// Выстрелы по комнате по очереди (аргумент можно повторять): ID раздела — в его монету,
    /// <c>x,y</c> — в точку окна (физические px).
    /// </summary>
    public IReadOnlyList<string> Fire { get; private init; } = new List<string>();

    private static DebugArgs Parse(string[] args)
    {
        string? shot = null;
        double? quitAfter = null;
        bool openQuiz = false;
        string? questionId = null;
        bool hordeNow = false;
        int? ammo = null;
        var fire = new List<string>();

        foreach (var arg in args)
        {
            if (TryValue(arg, "--shot", out var v))
                shot = v;
            else if (TryValue(arg, "--quit-after", out v)
                     && double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var sec))
                quitAfter = sec;
            else if (arg == "--open-quiz")
                openQuiz = true;
            else if (TryValue(arg, "--question", out v))
                questionId = v;
            else if (arg == "--horde-now")
                hordeNow = true;
            else if (TryValue(arg, "--ammo", out v)
                     && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                ammo = n;
            else if (TryValue(arg, "--fire", out v))
                fire.Add(v);
            else
                GD.PushWarning($"DebugArgs: неизвестный или неверный аргумент '{arg}'");
        }

        return new DebugArgs
        {
            ShotPath = shot,
            QuitAfterSec = quitAfter,
            OpenQuiz = openQuiz,
            QuestionId = questionId,
            HordeNow = hordeNow,
            Ammo = ammo,
            Fire = fire,
        };
    }

    private static bool TryValue(string arg, string name, out string value)
    {
        var prefix = name + "=";
        if (arg.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            value = arg[prefix.Length..];
            return value.Length > 0;
        }
        value = "";
        return false;
    }
}
