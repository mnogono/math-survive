using System;
using System.Collections.Generic;
using System.Linq;
using MathSurvive.Core.Progress;

namespace MathSurvive.Core.Questions;

/// <summary>
/// Пакеты разделов с независимыми сложностью и историей показанных вопросов.
/// Следующий вопрос — из случайного включённого раздела; последний включённый раздел выключить нельзя.
/// </summary>
public sealed class QuestionCatalog
{
    private sealed class SectionState(QuestionPack pack, QuestionPicker picker, DifficultyTracker difficulty)
    {
        public QuestionPack Pack { get; } = pack;
        public QuestionPicker Picker { get; } = picker;
        public DifficultyTracker Difficulty { get; } = difficulty;
        public Dictionary<int, HashSet<string>> Used { get; } = new();

        public HashSet<string> UsedAt(int level)
        {
            if (!Used.TryGetValue(level, out var set)) Used[level] = set = new HashSet<string>();
            return set;
        }
    }

    private readonly Dictionary<string, SectionState> _sections = new();
    private readonly List<SectionState> _ordered = new();
    private readonly IReadOnlyList<string> _defaultEnabled;
    private readonly HashSet<string> _enabled = new();
    private readonly Random _rng;
    private SectionState _active;

    /// <param name="defaultEnabled">Разделы, включённые по умолчанию; остальные пакеты доступны через <see cref="SetEnabled"/>.</param>
    public QuestionCatalog(GameConfig config, IReadOnlyList<QuestionPack> packs, IReadOnlyList<string> defaultEnabled, Random rng)
    {
        Config = config;
        _rng = rng;
        foreach (var pack in packs)
        {
            if (_sections.ContainsKey(pack.Section.Id))
                throw new ArgumentException($"Повтор раздела «{pack.Section.Id}»", nameof(packs));
            var state = new SectionState(pack,
                new QuestionPicker(pack.Questions, rng),
                new DifficultyTracker(config, pack.Section.Levels));
            _sections[pack.Section.Id] = state;
            _ordered.Add(state);
        }

        if (defaultEnabled.Count == 0)
            throw new ArgumentException("Нет разделов, включённых по умолчанию", nameof(defaultEnabled));
        if (defaultEnabled.Distinct().Count() != defaultEnabled.Count)
            throw new ArgumentException("Повтор раздела среди включённых по умолчанию", nameof(defaultEnabled));
        var missing = defaultEnabled.FirstOrDefault(id => !_sections.ContainsKey(id));
        if (missing is not null)
            throw new ArgumentException($"Нет раздела «{missing}»", nameof(defaultEnabled));

        _defaultEnabled = defaultEnabled.ToArray();
        _active = _sections[_defaultEnabled[0]];
        EnableExactly(_defaultEnabled);
    }

    public GameConfig Config { get; }

    /// <summary>Включённые разделы в порядке пакетов.</summary>
    public IReadOnlyList<string> EnabledSectionIds => Enabled().Select(s => s.Pack.Section.Id).ToArray();

    /// <summary>Раздел последнего выданного вопроса (до первого — первый включённый).</summary>
    public QuestionPack ActivePack => _active.Pack;
    public DifficultyTracker ActiveDifficulty => _active.Difficulty;

    public bool IsEnabled(string id) => _enabled.Contains(Require(id).Pack.Section.Id);

    /// <returns><c>false</c>, если раздел уже в этом состоянии или это последний включённый раздел.</returns>
    public bool SetEnabled(string id, bool enabled)
    {
        Require(id);
        if (enabled) return _enabled.Add(id);
        if (_enabled.Count == 1 || !_enabled.Remove(id)) return false;
        return true;
    }

    /// <summary>Включить только этот раздел и сделать его активным.</summary>
    public void EnableOnly(string id)
    {
        var state = Require(id);
        EnableExactly(new[] { id });
        _active = state;
    }

    public Question NextQuestion()
    {
        var enabled = Enabled().ToList();
        _active = enabled[_rng.Next(enabled.Count)];
        var level = _active.Difficulty.Level;
        return _active.Picker.NextAtLevel(level, _active.UsedAt(level));
    }

    public Question? FindActive(string id) => _active.Picker.Find(id);

    /// <summary>Включённые разделы, сложность и показанные ID всех разделов; текущий вопрос не входит.</summary>
    public QuizProgressSnapshot CaptureProgress() => new(EnabledSectionIds, _ordered.Select(s =>
        new SectionProgressSnapshot(s.Pack.Section.Id, s.Difficulty.Level, s.Difficulty.CorrectStreak, s.Difficulty.WrongStreak,
            s.Used.Where(kv => kv.Value.Count > 0)
                .ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value.Order(StringComparer.Ordinal).ToArray())))
        .ToArray());

    /// <summary>
    /// Применить snapshot целиком. <c>null</c> или недопустимый snapshot — значения по умолчанию.
    /// Разделы, которых нет в snapshot, начинают заново; ID, которых больше нет на уровне, отбрасываются.
    /// Без списка включённых разделов (старый формат) включаются разделы по умолчанию.
    /// </summary>
    public void RestoreProgress(QuizProgressSnapshot? snapshot)
    {
        var valid = ProgressRules.IsValid(snapshot, _ordered.Select(s => s.Pack.Section), Config);
        var bySection = valid ? snapshot!.Sections.ToDictionary(s => s.SectionId) : new Dictionary<string, SectionProgressSnapshot>();

        foreach (var state in _ordered)
        {
            state.Used.Clear();
            if (!bySection.TryGetValue(state.Pack.Section.Id, out var saved))
            {
                state.Difficulty.Restore(1, 0, 0);
                continue;
            }
            state.Difficulty.Restore(saved.Level, saved.CorrectStreak, saved.WrongStreak);
            foreach (var (level, ids) in saved.UsedQuestionIdsByLevel)
            {
                var known = state.Pack.Questions.Where(q => q.Level == level).Select(q => q.Id).ToHashSet();
                state.UsedAt(level).UnionWith(ids.Where(known.Contains));
            }
        }

        EnableExactly(valid && snapshot!.EnabledSectionIds is { } enabled ? enabled : _defaultEnabled);
        _active = Enabled().First();
    }

    private IEnumerable<SectionState> Enabled() => _ordered.Where(s => _enabled.Contains(s.Pack.Section.Id));

    private void EnableExactly(IEnumerable<string> ids)
    {
        _enabled.Clear();
        _enabled.UnionWith(ids);
    }

    private SectionState Require(string id)
        => _sections.TryGetValue(id, out var state) ? state : throw new ArgumentException($"Нет раздела «{id}»", nameof(id));
}
