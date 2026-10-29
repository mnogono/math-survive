using System;
using System.Collections.Generic;
using System.Linq;

namespace MathSurvive.Core.Questions;

public sealed class QuestionPicker
{
    private readonly IReadOnlyList<Question> _all;
    private readonly Random _rng;

    public QuestionPicker(IReadOnlyList<Question> all, Random rng)
    {
        _all = all;
        _rng = rng;
    }

    public Question? Find(string id) => _all.FirstOrDefault(q => q.Id == id);

    public Question Next(int level, ISet<string> used)
    {
        if (_all.Count == 0) throw new InvalidOperationException("Пакет вопросов пуст");

        var max = _all.Max(q => q.Level);
        var order = new List<int> { level };
        for (var l = level - 1; l >= 1; l--) order.Add(l);
        for (var l = level + 1; l <= max; l++) order.Add(l);

        foreach (var l in order)
        {
            var atLevel = _all.Where(q => q.Level == l).ToList();
            if (atLevel.Count == 0) continue;

            var fresh = atLevel.Where(q => !used.Contains(q.Id)).ToList();
            if (fresh.Count == 0)
            {
                foreach (var q in atLevel) used.Remove(q.Id);
                fresh = atLevel;
            }
            var pick = fresh[_rng.Next(fresh.Count)];
            used.Add(pick.Id);
            return pick;
        }
        throw new InvalidOperationException("Нет подходящих вопросов");
    }
}
