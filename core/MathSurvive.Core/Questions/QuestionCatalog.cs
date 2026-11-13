using System;
using System.Collections.Generic;
using System.Linq;

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
            _sections[pack.Section.Id] = new SectionState(pack,
                new QuestionPicker(pack.Questions, rng),
                new DifficultyTracker(config, pack.Section.Levels));
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
}
