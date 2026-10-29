using System;
using System.Collections.Generic;

namespace MathSurvive.Core.Stats;

public sealed class SectionStats
{
    public int Correct { get; set; }
    public int Wrong { get; set; }
    public int MaxLevel { get; set; }
}

/// <summary>Статистика забега (или всей игры — тогда в неё сливаются забеги через MergeInto).</summary>
public sealed class RunStats
{
    private readonly Dictionary<string, SectionStats> _sections = new();

    public int Correct { get; private set; }
    public int Wrong { get; private set; }
    public TimeSpan AnswerTime { get; private set; }

    public TimeSpan AverageAnswerTime
    {
        get
        {
            var answers = Correct + Wrong;
            return answers == 0 ? TimeSpan.Zero : AnswerTime / answers;
        }
    }

    public IReadOnlyDictionary<string, SectionStats> Sections => _sections;
    public int ZombiesKilled { get; private set; }
    public int HordesSurvived { get; private set; }
    public int AmmoSpent { get; private set; }

    public void RecordAnswer(string sectionId, bool correct, TimeSpan time, int level)
    {
        var s = Section(sectionId);
        if (correct)
        {
            Correct++;
            s.Correct++;
        }
        else
        {
            Wrong++;
            s.Wrong++;
        }
        AnswerTime += time;
        s.MaxLevel = Math.Max(s.MaxLevel, level);
    }

    public void RecordShot() => AmmoSpent++;

    public void RecordKills(int n) => ZombiesKilled += n;

    public void RecordHordeSurvived() => HordesSurvived++;

    /// <summary>Прибавляет счётчики этого забега к <paramref name="lifetime"/>; MaxLevel — максимум.</summary>
    public void MergeInto(RunStats lifetime)
    {
        lifetime.Correct += Correct;
        lifetime.Wrong += Wrong;
        lifetime.AnswerTime += AnswerTime;
        lifetime.ZombiesKilled += ZombiesKilled;
        lifetime.HordesSurvived += HordesSurvived;
        lifetime.AmmoSpent += AmmoSpent;
        foreach (var (id, s) in _sections)
        {
            var target = lifetime.Section(id);
            target.Correct += s.Correct;
            target.Wrong += s.Wrong;
            target.MaxLevel = Math.Max(target.MaxLevel, s.MaxLevel);
        }
    }

    private SectionStats Section(string id)
    {
        if (!_sections.TryGetValue(id, out var s))
        {
            s = new SectionStats();
            _sections[id] = s;
        }
        return s;
    }
}
