using System;
using System.Collections.Generic;
using System.Linq;
using MathSurvive.Core.Progress;

namespace MathSurvive.Core.Questions;

/// <summary>
/// Пакеты разделов с независимыми сложностью и историей показанных вопросов.
/// Выбор <c>null</c> — случайный режим: вопросы по кругу из разделов <c>randomOrder</c>.
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
    private readonly IReadOnlyList<string> _randomOrder;
    private int _randomCursor;
    private SectionState _active;

    public QuestionCatalog(GameConfig config, IReadOnlyList<QuestionPack> packs, IReadOnlyList<string> randomOrder, Random rng)
    {
        Config = config;
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

        if (randomOrder.Count == 0)
            throw new ArgumentException("Пустой порядок случайного режима", nameof(randomOrder));
        if (randomOrder.Distinct().Count() != randomOrder.Count)
            throw new ArgumentException("Повтор раздела в порядке случайного режима", nameof(randomOrder));
        var missing = randomOrder.FirstOrDefault(id => !_sections.ContainsKey(id));
        if (missing is not null)
            throw new ArgumentException($"Нет раздела «{missing}»", nameof(randomOrder));

        _randomOrder = randomOrder.ToArray();
        _active = _sections[_randomOrder[0]];
    }

    public GameConfig Config { get; }

    /// <summary><c>null</c> — случайный режим.</summary>
    public string? SelectedSectionId { get; private set; }

    public QuestionPack ActivePack => _active.Pack;
    public DifficultyTracker ActiveDifficulty => _active.Difficulty;

    public Question NextQuestion()
    {
        if (SelectedSectionId is null)
        {
            _active = _sections[_randomOrder[_randomCursor]];
            _randomCursor = (_randomCursor + 1) % _randomOrder.Count;
        }
        var level = _active.Difficulty.Level;
        return _active.Picker.NextAtLevel(level, _active.UsedAt(level));
    }

    public Question? FindActive(string id) => _active.Picker.Find(id);

    public void SelectSection(string id)
    {
        if (!_sections.TryGetValue(id, out var state))
            throw new ArgumentException($"Нет раздела «{id}»", nameof(id));
        SelectedSectionId = id;
        _active = state;
    }

    public void SelectRandom()
    {
        SelectedSectionId = null;
        _randomCursor = 0;
        _active = _sections[_randomOrder[0]];
    }

    /// <summary>Выбор, сложность и показанные ID всех разделов; текущий вопрос и курсор случайного режима не входят.</summary>
    public QuizProgressSnapshot CaptureProgress() => new(SelectedSectionId, _ordered.Select(s =>
        new SectionProgressSnapshot(s.Pack.Section.Id, s.Difficulty.Level, s.Difficulty.CorrectStreak, s.Difficulty.WrongStreak,
            s.Used.Where(kv => kv.Value.Count > 0)
                .ToDictionary(kv => kv.Key, kv => (IReadOnlyList<string>)kv.Value.Order(StringComparer.Ordinal).ToArray())))
        .ToArray());

    /// <summary>
    /// Применить snapshot целиком. <c>null</c> или недопустимый snapshot — значения по умолчанию.
    /// Разделы, которых нет в snapshot, начинают заново; ID, которых больше нет на уровне, отбрасываются.
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

        if (valid && snapshot!.SelectedSectionId is { } id) SelectSection(id);
        else SelectRandom();
    }
}
