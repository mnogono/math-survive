# Math Survive — этапы 0–2 (каркас, квиз, бой) — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Играбельная версия на примитивах: прозрачное окно поверх рабочего стола, персонаж на панели задач, дверь-выход, «!» → квиз с формулами (SVG) из тестового пакета, скрытые патроны и точность, орда-«кино» по 30 минутам игрового времени, game over со статистикой.

**Architecture:** Вся игровая логика — в библиотеке `core/MathSurvive.Core` (чистый C#, без Godot), покрыта xUnit-тестами. Godot-проект (`MathSurvive.csproj`, корень репозитория) ссылается на неё и содержит только сцены и «вид»: окно, примитивы, квиз, зомби. Центр логики — машина состояний `GameSession`, которую сцена двигает вызовами `Tick`/`OpenQuiz`/`Answer`/… и читает её `Mode`.

**Tech Stack:** Godot 4.7.2 mono (`C:\w\godot\Godot_v4.7.2-stable_mono_win64`), .NET SDK 8, C# 12, xUnit, System.Text.Json; Node.js 24 + `mathjax-full` (только инструмент подготовки SVG).

**Spec:** `docs/superpowers/specs/2026-10-30-math-survive-design.md`

**Вне этого плана (отдельные планы позже):** этап 3 — импортёр готовых квизов и генератор; этап 4 — меню «область → раздел», меню персонажа, `settings.ini`, `save.json`; этап 5 — Mixamo-графика и звук. Здесь настройки — значения по умолчанию в `GameConfig`, пакет — один тестовый раздел, сохранения нет.

## Global Constraints

- Godot 4.7.2 mono, C#; редактор: `C:\w\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`, консольный: `...\Godot_v4.7.2-stable_mono_win64_console.exe`.
- Платформа Windows 10/11, только основной монитор.
- Управление только мышью; окно игры не забирает фокус клавиатуры у рабочих окон.
- Клики мимо элементов игры проходят насквозь к окнам под ней.
- Патроны во время игры нигде не показываются (только в статистике).
- Игровое время течёт только пока квиз раскрыт.
- Значения механики по умолчанию (из спеки): интервал орды 30 игровых минут; пауза 10 реальных минут; старт 30 патронов; +3 патрона за верный; точность старт 100%, +5% за верный (≤100%), −10% за неверный (≥20%); зомби в орде N = 5 + 3·(N−1); 1 попадание на зомби; стрельба с трети ширины экрана; уровень +1 после 3 верных подряд, −1 после 2 неверных подряд (≥1).
- Коммиты: в рабочую ветку `stage0-2` в worktree `C:\wt\math-survive-s02`; в конце — fast-forward в `master` и push (в этом проекте коммит в `master` разрешён). Без подписи Claude и без trailer `Co-Authored-By`.
- Логика не зависит от Godot: в `core/` нет `using Godot`.
- Случайность в логике — только через переданный `System.Random` (детерминированные тесты с seed).

## Review Focus

1. **Клики насквозь в каждом режиме** (ожидание, квиз раскрыт, орда, статистика): клик по рабочему столу/окну/панели задач вне элементов игры доходит до них. → ручная проверка в Task 8, шаг «Проверка пользователем», и повтор в Task 12.
2. **Фокус клавиатуры:** печатаешь в Блокноте, кликаешь «!» и вариант ответа — ввод продолжает идти в Блокнот. → ручная проверка в Task 8 и Task 12.
3. **Масштаб Windows 125%/150% и панель задач не снизу / автоскрытие:** персонаж стоит на нижнем крае рабочей области, формулы чёткие. → тест `PlacementTests` в Task 8 (рабочая область со смещением), SVG грузится с учётом `ScreenGetScale` в Task 7.
4. **Двойной клик по варианту ответа:** засчитывается один ответ. → тест `Answer_InFeedbackMode_IsIgnored` в Task 5.
5. **Длинная текстовая задача:** облачко помещается в рабочую область, варианты столбиком. → тесты `QuizLayoutTests` в Task 9.

---

## Структура файлов

```
math-survive/
  project.godot                 # Godot-проект (Task 1)
  MathSurvive.csproj            # Godot.NET.Sdk, ссылка на core (Task 1)
  MathSurvive.sln               # все три проекта (Task 1)
  nuget.config                  # источники nuget.org + локальные nupkgs Godot (Task 1)
  .gitignore                    # .godot/, bin/, obj/, node_modules/, .superpowers/ (Task 1)
  core/MathSurvive.Core/
    MathSurvive.Core.csproj     # net8.0 classlib (Task 1)
    GameConfig.cs               # настройки по умолчанию (Task 2)
    CombatState.cs              # патроны, точность (Task 2)
    ShotResolver.cs             # выстрел (Task 2)
    HordePlanner.cs             # размер орды (Task 2)
    DifficultyTracker.cs        # уровень и серии (Task 3)
    PauseState.cs               # разовая пауза (Task 3)
    GameClock.cs                # игровое время до орды (Task 3)
    Questions/Question.cs       # модель вопроса (Task 4)
    Questions/PackLoader.cs     # разбор section.json/questions.json (Task 4)
    Questions/QuestionPicker.cs # выбор без повторов (Task 4)
    Stats/RunStats.cs           # статистика забега и за всё время (Task 5)
    Session/GameMode.cs         # режимы (Task 5)
    Session/GameSession.cs      # машина состояний (Task 5)
    Session/QuizView.cs         # вопрос с перемешанными вариантами (Task 5)
    Layout/Placement.cs         # раскладка по рабочей области (Task 8)
    Layout/QuizLayout.cs        # выбор раскладки квиза (Task 9)
  tests/MathSurvive.Core.Tests/ # xUnit, по файлу тестов на класс (Tasks 1–9)
  tools/tex2svg/
    package.json                # mathjax-full (Task 6)
    build-pack.mjs              # LaTeX-источник → пакет с SVG (Task 6)
  packs-src/math/test/questions.src.json   # исходник тестового пакета с LaTeX (Task 6)
  packs/math/area.json, packs/math/test/{section.json,questions.json,svg/*.svg}  # собранный пакет (Task 6)
  scenes/Main.tscn              # корневая сцена (Task 7, дополняется 8–12)
  scripts/
    Main.cs                     # корень: держит GameSession, связывает виды (Task 7+)
    SvgTextures.cs              # SVG → ImageTexture (Task 7)
    DebugArgs.cs                # --shot=<png> --quit-after=<сек> (Task 7)
    DesktopOverlay.cs           # окно: рабочая область, прозрачность, поверх всех, клики насквозь (Task 8)
    Win32.cs                    # P/Invoke для no-activate/возврата фокуса (Task 8)
    Stage.cs                    # 3D-сцена: камера, перевод пикселей в мир (Task 8)
    PlayerView.cs, DoorView.cs, BubbleView.cs  # примитивы и клики (Task 8)
    QuizPanel.cs                # квиз-облачко (Task 9)
    HordeDirector.cs, ZombieView.cs            # орда (Task 10)
    SummaryToast.cs, StatsPanel.cs             # сводка орды и статистика (Task 11)
```

---

### Task 0 (предусловие, делает координатор с разрешения пользователя): .NET SDK 8 и worktree

**Модель:** — (координатор)

- [ ] **Step 1:** Установить .NET SDK 8 (в системе `dotnet` нет; Godot 4.7.2 mono требует net8.0): `winget install Microsoft.DotNet.SDK.8` — только после согласия пользователя.
- [ ] **Step 2:** Проверить: `dotnet --list-sdks` → строка `8.0.x`.
- [ ] **Step 3:** Создать worktree: `git -c core.longpaths=true worktree add -b stage0-2 C:/wt/math-survive-s02 master`, затем в нём `git config core.longpaths true`.

---

### Task 1: Каркас решения — Godot-проект, Core, тесты

**Модель:** sonnet

**Files:**
- Create: `project.godot`, `MathSurvive.csproj`, `MathSurvive.sln`, `nuget.config`, `.gitignore` (дополнить), `core/MathSurvive.Core/MathSurvive.Core.csproj`, `tests/MathSurvive.Core.Tests/MathSurvive.Core.Tests.csproj`, `tests/MathSurvive.Core.Tests/SmokeTests.cs`, `scenes/Main.tscn`, `scripts/Main.cs`

**Interfaces:**
- Produces: сборка `MathSurvive.Core` (namespace `MathSurvive.Core`), тестовый проект; Godot-сборка `MathSurvive` (namespace `MathSurvive.Game`); главная сцена `res://scenes/Main.tscn` с корнем `Node3D` и скриптом `Main`.

- [ ] **Step 1: Файлы проекта.**
  - `project.godot`: `config/name="Math Survive"`, `run/main_scene="res://scenes/Main.tscn"`, `[dotnet] project/assembly_name="MathSurvive"`, `config/features=PackedStringArray("4.7", "C#", "Forward Plus")`.
  - `MathSurvive.csproj`: `<Project Sdk="Godot.NET.Sdk/4.7.2">`, `TargetFramework net8.0`, `RootNamespace MathSurvive.Game`, `<Compile Remove="core/**;tests/**;tools/**;packs-src/**" />`, `<ProjectReference Include="core/MathSurvive.Core/MathSurvive.Core.csproj" />`.
  - `MathSurvive.Core.csproj`: `Microsoft.NET.Sdk`, net8.0, `Nullable enable`, `LangVersion 12`.
  - Тестовый проект: net8.0, пакеты `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (последние стабильные через `dotnet add package`), ссылка на Core.
  - `nuget.config`: источники `nuget.org` и `C:\w\godot\Godot_v4.7.2-stable_mono_win64\GodotSharp\Tools\nupkgs`.
  - `.gitignore`: добавить `.godot/`, `bin/`, `obj/`, `node_modules/`, `*.user`.
  - `MathSurvive.sln`: три проекта (`dotnet new sln` + `dotnet sln add`).
  - `scenes/Main.tscn`: корень `Node3D` «Main» со скриптом `res://scripts/Main.cs` (`public partial class Main : Node3D` в namespace `MathSurvive.Game`, пустой `_Ready`).
- [ ] **Step 2: Smoke-тест** `SmokeTests.Core_assembly_loads`: `Assert.Equal("MathSurvive.Core", typeof(MathSurvive.Core.AssemblyMarker).Assembly.GetName().Name);` и класс `public static class AssemblyMarker {}` в Core.
- [ ] **Step 3: Проверка сборки и тестов.** Run: `dotnet build MathSurvive.sln` → `Build succeeded`, 0 ошибок. Run: `dotnet test tests/MathSurvive.Core.Tests` → `Passed! … Failed: 0`.
- [ ] **Step 4: Проверка Godot.** Run: `"C:\w\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe" --headless --path . --build-solutions --quit` → без `ERROR`. Затем `... --headless --path . --quit-after 60` → без `ERROR`/`SCRIPT ERROR` в выводе. (Первый запуск с `--editor --headless --quit` создаёт `.godot/` — допустимо.)
- [ ] **Step 5: Commit** `git add -A && git commit -m "Каркас: Godot C#-проект, Core и тесты"`

---

### Task 2: Core — настройки, бой, выстрел, размер орды

**Модель:** sonnet

**Files:**
- Create: `core/MathSurvive.Core/GameConfig.cs`, `CombatState.cs`, `ShotResolver.cs`, `HordePlanner.cs`
- Test: `tests/MathSurvive.Core.Tests/CombatStateTests.cs`, `ShotResolverTests.cs`, `HordePlannerTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record GameConfig` со свойствами `init` и значениями по умолчанию: `TimeSpan HordeInterval = 30 мин`, `TimeSpan PauseDuration = 10 мин`, `int StartAmmo = 30`, `int AmmoPerCorrect = 3`, `double StartAccuracy = 1.0`, `double AccuracyUp = 0.05`, `double AccuracyDown = 0.10`, `double MinAccuracy = 0.20`, `int HordeBase = 5`, `int HordeGrowth = 3`, `double FireRangeFraction = 1.0/3`, `int LevelUpStreak = 3`, `int LevelDownStreak = 2`, `TimeSpan FeedbackDuration = 1.5 с`, `TimeSpan HordeSummaryDuration = 4 с`, `double ZombieSpeedPx = 60`, `double ZombieSpawnIntervalSec = 4`, `double FireIntervalSec = 0.8`, `int IdleMaxFps = 15`, `int ActiveMaxFps = 60`. `public static GameConfig Default { get; } = new();`
  - `public sealed class CombatState { CombatState(GameConfig c); int Ammo {get;} double Accuracy {get;} void ApplyAnswer(bool correct); bool TryConsumeAmmo(); }`
  - `public enum ShotOutcome { NoAmmo, Miss, Hit }`
  - `public static class ShotResolver { static ShotOutcome Fire(CombatState s, Random rng); }` — тратит патрон, попадание если `rng.NextDouble() < s.Accuracy`.
  - `public static class HordePlanner { static int ZombieCount(int hordeNumber, GameConfig c); }` — `hordeNumber` с 1.

- [ ] **Step 1: Тесты.**
  - `CombatStateTests.Starts_with_config_values`: `Ammo == 30`, `Accuracy == 1.0`.
  - `Correct_answer_adds_ammo_and_accuracy_capped`: старт, `ApplyAnswer(false)` (0.9), `ApplyAnswer(true)` → `Ammo == 33`, `Accuracy` ≈ 0.95 (точность 1e-9); ещё 2× `true` → `Accuracy == 1.0`.
  - `Wrong_answer_reduces_accuracy_floored`: 10× `ApplyAnswer(false)` → `Accuracy` ≈ 0.20, `Ammo == 30`.
  - `TryConsumeAmmo_stops_at_zero`: `StartAmmo = 2` → true, true, false; `Ammo == 0`.
  - `ShotResolverTests.No_ammo_returns_NoAmmo` (`StartAmmo = 0`).
  - `Full_accuracy_always_hits`: 100 выстрелов при `StartAmmo = 100` с `new Random(1)` → все `Hit`.
  - `Accuracy_drives_hit_rate`: точность 0.2 (`StartAccuracy = 0.2`), 10000 выстрелов, `new Random(42)` → доля Hit в [0.17, 0.23].
  - `HordePlannerTests`: `ZombieCount(1)==5`, `(2)==8`, `(5)==17`.
- [ ] **Step 2:** `dotnet test tests/MathSurvive.Core.Tests` → FAIL (типов нет).
- [ ] **Step 3:** Реализовать типы по Interfaces.
- [ ] **Step 4:** `dotnet test tests/MathSurvive.Core.Tests` → все PASS.
- [ ] **Step 5: Commit** `git commit -am "Core: настройки, патроны, точность, выстрел, размер орды"` (с `git add` новых файлов).

---

### Task 3: Core — уровень сложности, пауза, игровое время

**Модель:** sonnet

**Files:**
- Create: `core/MathSurvive.Core/DifficultyTracker.cs`, `PauseState.cs`, `GameClock.cs`
- Test: `tests/MathSurvive.Core.Tests/DifficultyTrackerTests.cs`, `PauseStateTests.cs`, `GameClockTests.cs`

**Interfaces:**
- Consumes: `GameConfig` (Task 2).
- Produces:
  - `public sealed class DifficultyTracker { DifficultyTracker(GameConfig c, int maxLevel, int level = 1); int Level {get;} int MaxLevel {get;} void RecordAnswer(bool correct); }` — серия верных/неверных сбрасывается при смене знака и после смены уровня; уровень в [1, maxLevel].
  - `public sealed class PauseState { bool Available {get;} = true; DateTime? Until {get;} bool TryStart(DateTime nowUtc, TimeSpan duration); bool IsActive(DateTime nowUtc); void OnCorrectAnswer(); }` — `TryStart` делает `Available=false`; повторно до `OnCorrectAnswer` вернёт false.
  - `public sealed class GameClock { TimeSpan Elapsed {get;} void Advance(TimeSpan delta); bool IsHordeDue(TimeSpan interval); void Reset(); }`

- [ ] **Step 1: Тесты.**
  - `Three_correct_in_a_row_raise_level`: 3× true → `Level == 2`; ещё 2× true → 2; третий → 3.
  - `Streak_breaks_on_wrong`: true, true, false, true → `Level == 1`.
  - `Two_wrong_lower_level_not_below_one`: tracker с `level: 3` → false, false → 2; false, false → 1; false, false → 1.
  - `Level_capped_at_max`: `maxLevel: 2`, 9× true → 2.
  - `Pause_is_single_use_until_correct`: `TryStart(t0, 10мин)` → true, `IsActive(t0+5мин)` true, `IsActive(t0+10мин)` false, `TryStart(t0+11мин)` false; `OnCorrectAnswer()`; `TryStart(...)` true.
  - `Clock_due_at_interval`: `Advance(29:59)` → `IsHordeDue(30мин)` false; `Advance(1с)` → true; `Reset()` → `Elapsed == 0`.
- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Реализовать.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5: Commit** `"Core: уровень сложности, разовая пауза, игровое время"`

---

### Task 4: Core — вопросы, загрузка пакета, выбор без повторов

**Модель:** sonnet

**Files:**
- Create: `core/MathSurvive.Core/Questions/Question.cs`, `PackLoader.cs`, `QuestionPicker.cs`
- Test: `tests/MathSurvive.Core.Tests/PackLoaderTests.cs`, `QuestionPickerTests.cs`

**Interfaces:**
- Produces (namespace `MathSurvive.Core.Questions`):
  - `public sealed record QuestionOption(string? Text, string? Svg);`
  - `public sealed record Question(string Id, int Level, string? Prompt, string? PromptSvg, IReadOnlyList<QuestionOption> Options, int Correct);`
  - `public sealed record SectionInfo(string Id, string Title, int Levels);`
  - `public sealed record QuestionPack(SectionInfo Section, IReadOnlyList<Question> Questions);`
  - `public static class PackLoader { static QuestionPack Parse(string sectionJson, string questionsJson); }` — JSON snake_case как в спеке (`prompt_svg`, `options[].svg|text`, `correct`); `questions.json` — массив вопросов. Бросает `PackFormatException(string message)` (свой класс) если: у вопроса не 4 варианта; `correct` вне 0..3; у варианта нет ни `text`, ни `svg`; у вопроса нет ни `prompt`, ни `prompt_svg`; повтор `id`. Сообщение содержит `id` вопроса.
  - `public sealed class QuestionPicker { QuestionPicker(IReadOnlyList<Question> all, Random rng); Question Next(int level, ISet<string> used); }` — случайный из уровня, чьего `Id` нет в `used`, добавляет `Id` в `used`; если все на уровне использованы — удаляет из `used` id этого уровня и выбирает заново; если на уровне нет вопросов — берёт ближайший уровень (сначала ниже, затем выше); пустой пакет → `InvalidOperationException`.

- [ ] **Step 1: Тесты.**
  - `Parses_svg_and_text_questions`: строка-JSON с одним SVG-вопросом (как пример в спеке) и одним текстовым (`"prompt":"2+2=?"`, варианты `text`) → 2 вопроса, `Options[2].Svg == "svg/fractions-L3-0042-a2.svg"`, `Correct == 2`, второй `Options[0].Text == "3"`.
  - `Rejects_three_options`, `Rejects_correct_out_of_range`, `Rejects_duplicate_id`, `Rejects_option_without_text_and_svg` → `Assert.Throws<PackFormatException>`, `Message` содержит id.
  - `Picks_all_level_questions_before_repeating`: 3 вопроса уровня 1, `new Random(7)`, 3 вызова → 3 разных id; 4-й вызов не бросает, `used` после него содержит ровно 1 id.
  - `Falls_back_to_nearest_lower_then_higher_level`: вопросы уровней 1 и 4; `Next(3, …)` → уровень 1; только уровень 4 → `Next(2, …)` → уровень 4.
  - `Empty_pack_throws`.
- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Реализовать (System.Text.Json, `JsonNamingPolicy.SnakeCaseLower` или явные `JsonPropertyName`).
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5: Commit** `"Core: модель вопроса, загрузка пакета, выбор без повторов"`

---

### Task 5: Core — машина состояний GameSession и статистика

**Модель:** opus

**Files:**
- Create: `core/MathSurvive.Core/Session/GameMode.cs`, `QuizView.cs`, `GameSession.cs`, `core/MathSurvive.Core/Stats/RunStats.cs`
- Test: `tests/MathSurvive.Core.Tests/GameSessionTests.cs`, `RunStatsTests.cs`

**Interfaces:**
- Consumes: Tasks 2–4.
- Produces (namespace `MathSurvive.Core.Session` / `.Stats`):
  - `public enum GameMode { Idle, QuizOpen, Feedback, Paused, Horde, HordeSummary, GameOver }`
  - `public sealed record QuizView(Question Question, IReadOnlyList<int> Order);` — `Order[displayIndex] = индекс варианта в Question.Options`.
  - `public sealed record AnswerResult(bool Correct, int ChosenDisplay, int CorrectDisplay);`
  - `public sealed record HordeInfo(int Number, int ZombieCount);`
  - `public sealed class SectionStats { int Correct; int Wrong; int MaxLevel; }` (свойства с `get; set;`)
  - `public sealed class RunStats { int Correct {get;} int Wrong {get;} TimeSpan AnswerTime {get;} TimeSpan AverageAnswerTime {get;} IReadOnlyDictionary<string, SectionStats> Sections {get;} int ZombiesKilled {get;} int HordesSurvived {get;} int AmmoSpent {get;} void RecordAnswer(string sectionId, bool correct, TimeSpan time, int level); void RecordShot(); void RecordKills(int n); void RecordHordeSurvived(); void MergeInto(RunStats lifetime); }`
  - `public sealed class GameSession`:
    - `GameSession(GameConfig config, QuestionPack pack, Random rng)`
    - `GameMode Mode {get;}`, `QuizView? CurrentQuiz {get;}`, `AnswerResult? LastAnswer {get;}`, `HordeInfo? CurrentHorde {get;}`, `int LastHordeKills {get;}`, `int HordeNumber {get;}` (номер следующей/текущей орды, с 1), `CombatState Combat {get;}`, `DifficultyTracker Difficulty {get;}`, `PauseState Pause {get;}`, `GameClock Clock {get;}`, `RunStats Run {get;}`, `RunStats Lifetime {get;}`
    - `void OpenQuiz()`, `void CollapseQuiz()`, `AnswerResult? Answer(int displayIndex)`, `bool StartPause(DateTime nowUtc)`, `void Tick(TimeSpan delta, DateTime nowUtc)`, `ShotOutcome Fire()`, `void FinishHorde(bool survived, int killed)`, `void StartNewRun()`
    - `event Action<HordeInfo>? HordeStarted`

- [ ] **Step 1: Таблица переходов (реализовать ровно её).** Вызов в неподходящем режиме — ничего не делает (`Answer` возвращает `null`, `StartPause` — `false`).

| Из | Вызов / условие | В | Эффект |
|---|---|---|---|
| Idle | `OpenQuiz()` | QuizOpen | если `CurrentQuiz == null` — взять `Picker.Next(Difficulty.Level, used)` и перемешать порядок вариантов (`rng`) |
| QuizOpen | `CollapseQuiz()` | Idle | вопрос и накопленное время на него сохраняются |
| QuizOpen | `Tick(d)` | QuizOpen | `Clock.Advance(d)`, время на текущий вопрос += d; если `Clock.IsHordeDue` → см. ниже |
| QuizOpen | `Clock.IsHordeDue` в `Tick` | Horde | вопрос не засчитывается и остаётся `CurrentQuiz` (покажется после орды); `CurrentHorde = new(HordeNumber, HordePlanner.ZombieCount(...))`; `HordeStarted` |
| QuizOpen | `Answer(i)` | Feedback | `Combat.ApplyAnswer`, `Difficulty.RecordAnswer`, при верном `Pause.OnCorrectAnswer()`, `Run.RecordAnswer(section, correct, времяНаВопрос, Difficulty.Level)`; `LastAnswer` |
| QuizOpen | `StartPause(now)` и `Pause.TryStart` | Paused | вопрос сохраняется |
| Feedback | `Tick` суммарно ≥ `FeedbackDuration` | Idle | `CurrentQuiz = null`, `LastAnswer = null` |
| Paused | `Tick(_, now)` и `!Pause.IsActive(now)` | Idle | |
| Horde | `Fire()` | Horde | `ShotResolver.Fire`; при выстреле (не NoAmmo) `Run.RecordShot()` |
| Horde | `FinishHorde(true, k)` | HordeSummary | `LastHordeKills = k`, `Run.RecordKills(k)`, `Run.RecordHordeSurvived()`, `Clock.Reset()`, `HordeNumber++` |
| HordeSummary | `Tick` суммарно ≥ `HordeSummaryDuration` | Idle | `CurrentHorde = null` |
| Horde | `FinishHorde(false, k)` | GameOver | `Run.RecordKills(k)`, `Run.MergeInto(Lifetime)` |
| GameOver | `StartNewRun()` | Idle | новые `Combat`, `Pause`, `Clock`, `Run`; `HordeNumber = 1`; `CurrentQuiz = null`; `Difficulty` и `Lifetime` сохраняются |

  `Fire()` вне Horde возвращает `ShotOutcome.NoAmmo` и ничего не тратит. В Idle/Paused/Feedback/HordeSummary игровое время не идёт.

- [ ] **Step 2: Тесты** (в каждом `new GameSession(GameConfig.Default with {...}, pack, new Random(1))`, пакет — 10 вопросов уровня 1 с `correct = 0`):
  - `Starts_idle_and_time_does_not_flow`: `Tick(1ч)` → `Mode == Idle`, `Clock.Elapsed == 0`.
  - `Open_collapse_keeps_same_question`: `OpenQuiz`, запомнить id, `CollapseQuiz`, `OpenQuiz` → тот же id.
  - `Correct_answer_flow`: `OpenQuiz`; выбрать display-индекс, где `Order[i] == 0`; `Answer` → `Correct == true`, `Mode == Feedback`, `Combat.Ammo == 33`; `Tick(1.5с)` → `Idle`, `CurrentQuiz == null`.
  - `Wrong_answer_reports_correct_display_index`: ответ с `Order[i] != 0` → `Correct == false`, `Order[CorrectDisplay] == 0`, точность 0.9.
  - `Answer_InFeedbackMode_IsIgnored` (Review Focus 4): два `Answer` подряд → второй `null`, `Run.Correct + Run.Wrong == 1`.
  - `Options_are_shuffled_but_consistent`: при `new Random(3)` хотя бы у одного из 10 вопросов `Order` не равен `[0,1,2,3]`; у всех `Order` — перестановка 0..3.
  - `Pause_hides_quiz_until_time_passes_and_is_single_use`: `OpenQuiz`, `StartPause(t0)` → true, `Paused`; `Tick(_, t0+9мин)` → Paused; `Tick(_, t0+10мин)` → Idle; `OpenQuiz`, `StartPause(...)` → false (неверных/верных ответов не было).
  - `Horde_starts_when_game_time_reaches_interval_and_question_is_kept`: `HordeInterval = 1 мин`; `OpenQuiz`; `Tick(61с)` → `Horde`, событие с `(1, 5)`, `Run.Correct + Run.Wrong == 0`, `CurrentQuiz` не null.
  - `Survived_horde_returns_to_idle_and_next_horde_is_bigger`: после орды `FinishHorde(true, 5)` → HordeSummary; `Tick(4с)` → Idle; `Clock.Elapsed == 0`; `HordeNumber == 2`; вторая орда → событие `(2, 8)`.
  - `Lost_horde_is_game_over_and_new_run_resets_combat_keeps_difficulty`: уровень поднят до 2 (3 верных), орда, `FinishHorde(false, 3)` → GameOver, `Lifetime.ZombiesKilled == 3`; `StartNewRun()` → Idle, `Combat.Ammo == 30`, `Difficulty.Level == 2`, `Run.Correct == 0`.
  - `Fire_spends_ammo_and_records_shots`: в орде `StartAmmo = 2` → `Fire()` ×3 → третий `NoAmmo`, `Run.AmmoSpent == 2`.
  - `RunStatsTests.Average_and_sections`: два ответа (раздел "test", 10с верно ур.1, 20с неверно ур.2) → `AverageAnswerTime == 15с`, `Sections["test"].Correct == 1`, `.Wrong == 1`, `.MaxLevel == 2`; `MergeInto` суммирует счётчики и берёт max по `MaxLevel`.
- [ ] **Step 3:** `dotnet test` → FAIL.
- [ ] **Step 4:** Реализовать `RunStats`, `QuizView`, `GameSession` по таблице.
- [ ] **Step 5:** `dotnet test` → все PASS.
- [ ] **Step 6: Commit** `"Core: машина состояний игры и статистика"`

---

### Task 6: Инструмент LaTeX → SVG и тестовый пакет

**Модель:** sonnet

**Files:**
- Create: `tools/tex2svg/package.json`, `tools/tex2svg/build-pack.mjs`, `packs-src/math/test/questions.src.json`, `packs/math/area.json`, `packs/math/test/section.json`, сгенерированные `packs/math/test/questions.json` и `packs/math/test/svg/*.svg`, `packs/SOURCES.md`

**Interfaces:**
- Consumes: формат пакета из Task 4 (`PackLoader.Parse` должен принять результат).
- Produces: `node tools/tex2svg/build-pack.mjs <src.json> <outDir>`; пакет `res://packs/math/test/` (section `{"id":"test","title":"Тест","levels":3}`).

- [ ] **Step 1: Исходник `questions.src.json`** — массив вопросов, как в формате пакета, но вместо `prompt_svg`/`svg` поля `prompt_tex`/`tex` (LaTeX без `$`). 9 вопросов, по 3 на уровни 1–3, покрывающие: дробь `\frac{3}{4}+\frac{1}{6}`, степень с отрицательным показателем, корень `\sqrt[3]{27}`, производная `f(x)=3x^{4}-\frac{2}{x}`, производная `\sin(\alpha x)`, интеграл `\int_{0}^{\pi}\sin x\,dx`, неопределённый интеграл с `+C`, сумма `\sum_{k=1}^{n}k`, и **один текстовый вопрос** с длинным условием (≥200 символов) и текстовыми вариантами. Верные ответы проверить вручную.
- [ ] **Step 2: `build-pack.mjs`.** Для каждого `*_tex` — MathJax `tex2svg` (пакет `mathjax-full`, `fontCache: 'none'`), удалить внешнюю обёртку `mjx-container`, оставить самодостаточный `<svg>`; **заменить атрибуты `width`/`height` в `ex` на `px` (1ex = 8px)**, т.к. растеризатор Godot может не понимать `ex`; записать `svg/<id>-q.svg`, `svg/<id>-a<k>.svg`; записать `questions.json` с путями. Текстовые поля копировать как есть.
- [ ] **Step 3: Сборка.** Run: `cd tools/tex2svg && npm install && node build-pack.mjs ../../packs-src/math/test/questions.src.json ../../packs/math/test` → создано 9 записей и SVG-файлы; ни один SVG не содержит `ex"` в `width`/`height` (`grep -L` проверка).
- [ ] **Step 4: Тест совместимости с Core** `PackLoaderTests.Parses_built_test_pack`: читает `packs/math/test/section.json` и `questions.json` (путь от корня репозитория через поиск вверх от `AppContext.BaseDirectory` до `MathSurvive.sln`) → 9 вопросов, `Section.Levels == 3`, все пути SVG существуют на диске.
- [ ] **Step 5:** `dotnet test` → PASS. `packs/SOURCES.md`: «test — составлено вручную».
- [ ] **Step 6: Commit** (`node_modules` в `.gitignore`) `"Инструмент LaTeX→SVG и тестовый пакет"`

---

### Task 7: Спайк-риск — SVG MathJax в Godot, отладочные аргументы

**Модель:** opus

**Files:**
- Create: `scripts/SvgTextures.cs`, `scripts/DebugArgs.cs`; Modify: `scripts/Main.cs`, `scenes/Main.tscn`

**Interfaces:**
- Produces:
  - `public static class SvgTextures { static ImageTexture Load(string resPath, float scale); }` — `FileAccess.GetFileAsString` + `Image.LoadSvgFromString(text, scale)`; `scale` = желаемый масштаб × `DisplayServer.ScreenGetScale()`; кэш по `(path, scale)`.
  - `public sealed class DebugArgs { static DebugArgs Parse(string[] userArgs); string? ShotPath; double? QuitAfterSec; }` — из `OS.GetCmdlineUserArgs()` (`-- --shot=out.png --quit-after=3`). `Main` после `QuitAfterSec` сохраняет скриншот вьюпорта в `ShotPath` (если задан) и вызывает `GetTree().Quit()`.

- [ ] **Step 1:** Временная отладочная сцена-витрина (`scenes/SvgGallery.tscn` + скрипт): все SVG тестового пакета `TextureRect`-ами на светлом фоне, масштаб 1.5.
- [ ] **Step 2:** Run: `Godot_v4.7.2-stable_mono_win64_console.exe --path . res://scenes/SvgGallery.tscn -- --shot=C:/Users/SIAMS/AppData/Local/Temp/svg-gallery.png --quit-after=3` → PNG создан. Прочитать PNG (Read) и проверить глазами: дроби с чертой, ∫ с пределами, корень с показателем, индексы/степени, греческие буквы, `+C`.
- [ ] **Step 3: Решение по результату.** Если всё корректно — записать в «Ход выполнения» плана «SVG MathJax: ОК». Если нет — по порядку из спеки: (а) другие настройки вывода MathJax в `build-pack.mjs`; (б) растеризация в PNG генератором (`@resvg/resvg-js`) и загрузка PNG (`SvgTextures` → общий `FormulaTextures` с выбором по расширению); зафиксировать выбранный путь в плане. BLOCKED, если ни (а), ни (б) не дают результата.
- [ ] **Step 4:** Удалить витрину, если путь — SVG (оставить, если пригодится для проверки пакетов — на усмотрение, но не в главной сцене).
- [ ] **Step 5: Commit** `"Загрузка формул и отладочные аргументы; проверка SVG MathJax"`

---

### Task 8: Окно поверх рабочего стола, сцена, персонаж, дверь, «!»

**Модель:** opus

**Files:**
- Create: `core/MathSurvive.Core/Layout/Placement.cs`, `tests/MathSurvive.Core.Tests/PlacementTests.cs`, `scripts/DesktopOverlay.cs`, `scripts/Win32.cs`, `scripts/Stage.cs`, `scripts/PlayerView.cs`, `scripts/DoorView.cs`, `scripts/BubbleView.cs`; Modify: `project.godot`, `scenes/Main.tscn`, `scripts/Main.cs`

**Interfaces:**
- Consumes: `GameSession` (Task 5), `PackLoader` (Task 4), `DebugArgs` (Task 7).
- Produces:
  - `public readonly record struct PixelRect(int X, int Y, int W, int H)` (в `MathSurvive.Core.Layout`).
  - `public static class Placement { static (double x, double y) PlayerFeet(PixelRect usable); static (double x, double y) DoorFeet(PixelRect usable); static double FireLineX(PixelRect usable, double fireRangeFraction); static PixelRect Union(IEnumerable<PixelRect> rects); }` — координаты в пикселях окна (окно = рабочая область, начало в её левом верхнем углу): ноги персонажа `x = 70, y = usable.H`; дверь `x = 24, y = usable.H`; линия начала стрельбы `PlayerFeet.x + usable.W * fraction`.
  - `DesktopOverlay : Node` — `void RegisterHitArea(Func<Rect2?> area)`; каждый кадр (или по изменению) ставит `DisplayServer.WindowSetMousePassthrough` = прямоугольник-объединение видимых зон (`Placement.Union`); пустой набор → крошечный полигон в углу (всё насквозь).
  - `Stage : Node3D` — ортокамера; `Vector3 PixelToWorld(Vector2 px)`, `Vector2 WorldToPixel(Vector3 w)`.
  - `PlayerView`, `DoorView`, `BubbleView` — у каждого `Rect2 ScreenRect` и сигнал/событие `Clicked`.

- [ ] **Step 1: Тесты `PlacementTests`** (Review Focus 3): для `usable = (0, 0, 1920, 1040)` → `PlayerFeet == (70, 1040)`, `FireLineX(…, 1/3) == 70 + 640`; для рабочей области со смещением (панель слева, `X = 60`, `W = 1860`, `H = 1080`) → `PlayerFeet == (70, 1080)` (координаты окна не зависят от смещения); `Union` двух прямоугольников — охватывающий.
- [ ] **Step 2:** `dotnet test` → FAIL; реализовать `Placement`; → PASS.
- [ ] **Step 3: Окно.** `project.godot`: borderless, transparent, always on top, `display/window/per_pixel_transparency/allowed=true`, `rendering/viewport/transparent_background=true`, без растяжения содержимого (stretch disabled). В `DesktopOverlay._Ready`: позиция/размер окна = `DisplayServer.ScreenGetUsableRect(DisplayServer.GetPrimaryScreen())`; режим окна — обычное окно размером с рабочую область (не fullscreen — иначе перекроет панель задач). Нагрузка: `Engine.MaxFps = IdleMaxFps` в Idle/Paused/QuizOpen/Feedback/GameOver, `ActiveMaxFps` в Horde/HordeSummary.
- [ ] **Step 4: Без захвата фокуса.** По порядку, остановиться на первом рабочем: (1) `DisplayServer.WindowSetFlag(WindowFlags.NoFocus, true)`; (2) `Win32`: `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW` через `GetWindowLongPtrW/SetWindowLongPtrW` на `DisplayServer.WindowGetNativeHandle(HandleType.WindowHandle)`; (3) запасной: при клике по элементу игры запомнить `GetForegroundWindow()` до клика и вернуть `SetForegroundWindow` после обработки. Критерий — Review Focus 2 (проверка пользователем ниже); клики Godot при этом получает. Выбранный путь — строкой в «Ход выполнения».
- [ ] **Step 5: Сцена.** `Stage` с ортокамерой; `PlayerView` — капсула + коробка-«ружьё» вправо на `Placement.PlayerFeet`; `DoorView` — коричневый прямоугольник на `DoorFeet`; `BubbleView` — жёлтый круг «!» над головой (2D `Control`/`Label` поверх 3D допустимо), видим только при `Mode == Idle`, мигание — `Tween` 1 Гц. `Main`: грузит пакет `res://packs/math/test`, создаёт `GameSession(GameConfig.Default, pack, new Random())`, вызывает `Tick(delta, DateTime.UtcNow)` в `_Process`; клик по двери → `GetTree().Quit()`; клик по «!» → `OpenQuiz()` (квиз — Task 9). Зоны кликов (дверь, персонаж, «!») зарегистрированы в `DesktopOverlay`.
- [ ] **Step 6: Скриншот-проверка.** Run: `... --path . -- --shot=C:/Users/SIAMS/AppData/Local/Temp/overlay.png --quit-after=3` → PNG: прозрачный фон (видны только дверь, персонаж, «!» у нижнего левого края). Read PNG.
- [ ] **Step 7: Проверка пользователем** (координатор просит пользователя запустить `Godot_v4.7.2-stable_mono_win64.exe --path C:\wt\math-survive-s02` и проверить): персонаж стоит над панелью задач; клики по рабочему столу/окнам/панели задач рядом и вне персонажа проходят; печать в Блокноте не прерывается кликом по «!»; клик в другое окно не прячет игру; клик по двери закрывает игру. Результат — строкой в «Ход выполнения».
- [ ] **Step 8: Commit** `"Окно поверх рабочего стола, персонаж, дверь и «!»"`

---

### Task 9: Квиз-облачко

**Модель:** opus

**Files:**
- Create: `core/MathSurvive.Core/Layout/QuizLayout.cs`, `tests/MathSurvive.Core.Tests/QuizLayoutTests.cs`, `scripts/QuizPanel.cs`; Modify: `scenes/Main.tscn`, `scripts/Main.cs`

**Interfaces:**
- Consumes: `GameSession.CurrentQuiz/LastAnswer/Answer/CollapseQuiz/StartPause/Pause.Available`, `SvgTextures.Load`, `DesktopOverlay.RegisterHitArea`, `Placement`.
- Produces:
  - `public enum OptionsLayout { Grid2x2, Column }`
  - `public static class QuizLayout { static OptionsLayout Choose(IReadOnlyList<(int width, int textLength)> options, int maxGridCellWidth); static PixelRect Clamp(PixelRect panel, PixelRect usable, int margin); }` — `Grid2x2`, если у всех вариантов ширина SVG ≤ `maxGridCellWidth` и текст ≤ 24 символов; иначе `Column`. `Clamp` сдвигает панель внутрь рабочей области с отступом `margin`; если панель выше рабочей области — прижимает к верху (высота не меняется — прокрутка в `QuizPanel`).
  - `QuizPanel : PanelContainer` — `void Show(QuizView view)`, `void ShowFeedback(AnswerResult r)`, `void HideQuiz()`, события `OptionChosen(int displayIndex)`, `PauseClicked`, `Rect2 ScreenRect`.

- [ ] **Step 1: Тесты `QuizLayoutTests`** (Review Focus 5): 4 коротких SVG (ширина 120) при `maxGridCellWidth = 160` → `Grid2x2`; один вариант шириной 300 → `Column`; текстовый вариант 40 символов → `Column`; `Clamp((−50, 900, 400, 300), (0,0,1920,1040), 8)` → `(8, 732, 400, 300)`; панель 400×1200 → `Y == 8`.
- [ ] **Step 2:** `dotnet test` → FAIL; реализовать `QuizLayout`; → PASS.
- [ ] **Step 3: `QuizPanel`.** Облачко над персонажем (хвостик к «!»), светлый фон, скруглённые углы — как макет «вариант A» в спеке. Сверху текст `Prompt` (`Label`, перенос строк, ширина ≤ 560 px) и формула `PromptSvg` (`TextureRect`, масштаб 1.5). Варианты — `Button`ы с `Icon` (SVG) или текстом в порядке `QuizView.Order`, раскладка по `QuizLayout.Choose`. Внизу «⏸ Пауза 10 мин» — неактивна при `!Pause.Available`. Позиция — над головой персонажа, затем `QuizLayout.Clamp`. Если выше рабочей области — `ScrollContainer`.
- [ ] **Step 4: Связка в `Main`.** «!» → `OpenQuiz` → `QuizPanel.Show`; повторный клик по персонажу/«!»-зоне при раскрытом квизе → `CollapseQuiz`; `OptionChosen` → `Answer` → `ShowFeedback` (верный зелёным, выбранный неверный красным); выход из Feedback по `Tick` → `HideQuiz`; `PauseClicked` → `StartPause(DateTime.UtcNow)` → `HideQuiz`. Зона квиза зарегистрирована в `DesktopOverlay` только пока он видим.
- [ ] **Step 5: Скриншот-проверка.** Временный отладочный аргумент `--open-quiz` (в `DebugArgs`) раскрывает квиз при старте. Run с `--open-quiz --shot=…/quiz.png --quit-after=3` для формульного и для текстового вопроса (аргумент `--question=<id>`) → Read PNG: облачко над персонажем, формулы чёткие, длинный текст переносится, варианты столбиком, всё в пределах экрана.
- [ ] **Step 6: Commit** `"Квиз-облачко: формулы, варианты, обратная связь, пауза"`

---

### Task 10: Орда — зомби, автострельба, поражение

**Модель:** opus

**Files:**
- Create: `scripts/HordeDirector.cs`, `scripts/ZombieView.cs`; Modify: `scripts/Main.cs`, `scenes/Main.tscn`

**Interfaces:**
- Consumes: `GameSession.HordeStarted/Fire/FinishHorde/Mode`, `GameConfig.ZombieSpeedPx/ZombieSpawnIntervalSec/FireIntervalSec/FireRangeFraction`, `Placement.PlayerFeet/FireLineX`, `Stage.PixelToWorld`.
- Produces: `HordeDirector : Node3D` — `void Begin(HordeInfo info)`; по окончании сам вызывает `session.FinishHorde(survived, killed)`.

- [ ] **Step 1: Поведение.** `Begin`: спавнить `ZombieCount` зомби (зелёная капсула + голова) за правым краем (`x = usable.W + 40`) с интервалом `ZombieSpawnIntervalSec`; зомби идут влево со скоростью `ZombieSpeedPx`. Каждые `FireIntervalSec`, если ближайший зомби левее `FireLineX`: `session.Fire()`; `Hit` → ближайший зомби умирает (исчезает; короткая вспышка выстрела на конце ружья при любом выстреле, кроме `NoAmmo`); `Miss` → только вспышка. Зомби дошёл до `PlayerFeet.x + 30` → остановить орду, `FinishHorde(false, убито)`. Все заспавнены и убиты → `FinishHorde(true, убито)`. Зомби не кликабельны (в зоны `DesktopOverlay` не входят).
- [ ] **Step 2: Отладочный аргумент `--horde-now`**: при старте `GameConfig.Default with { HordeInterval = 1 с }` и сразу `OpenQuiz` — орда начнётся через секунду. Ещё `--ammo=<n>` для проверки поражения.
- [ ] **Step 3: Проверка.** Run `--horde-now --quit-after=40 --shot=…/horde.png` → Read PNG (зомби идут справа налево, персонаж стреляет). Run `--horde-now --ammo=0 --quit-after=60` и проверить по логу (`GD.Print` смены режима в `Main`) переход `Horde → GameOver`; с `--ammo=30` — `Horde → HordeSummary → Idle`.
- [ ] **Step 4: Commit** `"Орда: зомби, автострельба, победа и поражение"`

---

### Task 11: Сводка орды и экран статистики

**Модель:** sonnet

**Files:**
- Create: `scripts/SummaryToast.cs`, `scripts/StatsPanel.cs`; Modify: `scripts/Main.cs`, `scenes/Main.tscn`

**Interfaces:**
- Consumes: `GameSession.Mode/Run/Lifetime/CurrentHorde/LastHordeKills/Combat.Ammo/StartNewRun`, `RunStats` (Task 5).
- Produces: `SummaryToast.ShowFor(string text)`; `StatsPanel.Show(RunStats run, RunStats lifetime, int ammoLeft)`, событие `NewRunClicked`.

- [ ] **Step 1: `SummaryToast`** — при входе в `HordeSummary`: «Орда {CurrentHorde.Number} отбита, убито {LastHordeKills}» над персонажем, исчезает при выходе из режима.
- [ ] **Step 2: `StatsPanel`** — при `GameOver`, панель над персонажем (в зонах `DesktopOverlay`): «Задачи: верно X / неверно Y», по разделам (название раздела из `pack.Section.Title`), «Среднее время на задачу: m:ss», «Максимальный уровень: L», «Убито зомби: Z», «Отбито орд: H», «Патронов потрачено: S, осталось: A»; блок «За всё время» с теми же счётчиками; кнопка «Новая игра» → `StartNewRun()`.
- [ ] **Step 3: Проверка.** Run `--horde-now --ammo=0 --quit-after=70 --shot=…/stats.png` → Read PNG: панель статистики со всеми строками.
- [ ] **Step 4: Commit** `"Сводка орды и экран статистики"`

---

### Task 12: Сквозная проверка и слияние

**Модель:** — (координатор + пользователь)

- [ ] **Step 1:** `dotnet test` → все PASS; `--headless --build-solutions --quit` → без ошибок.
- [ ] **Step 2: Проверка пользователем** полного цикла (обычный запуск и с `--horde-now`): «!» → квиз → ответы (верный/неверный) → пауза → орда → сводка → снова «!»; поражение → статистика → «Новая игра»; Review Focus 1–2 повторно в режимах квиза, орды и статистики; дверь закрывает игру.
- [ ] **Step 3:** Рецензия всей ветки отдельным субагентом.
- [ ] **Step 4:** `git checkout master && git merge --ff-only stage0-2 && git push origin master`; удалить worktree.

---

## Ход выполнения

- 2026-10-30 — план составлен (координатор). Спека согласована пользователем.
