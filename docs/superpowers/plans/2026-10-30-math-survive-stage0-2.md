# Math Survive — этапы 0–2 (каркас, квиз, бой) — план реализации

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Играбельная версия на примитивах: прозрачное окно поверх рабочего стола, персонаж на панели задач, дверь-выход, «!» → квиз с формулами (SVG) из тестового пакета, скрытые патроны и точность, орда-«кино» по 30 минутам игрового времени, game over со статистикой.

**Architecture:** Вся игровая логика — в библиотеке `core/MathSurvive.Core` (чистый C#, без Godot), покрыта xUnit-тестами. Godot-проект (`MathSurvive.csproj`, корень репозитория) ссылается на неё и содержит только сцены и «вид»: окно, примитивы, квиз, зомби. Центр логики — машина состояний `GameSession`, которую сцена двигает вызовами `Tick`/`OpenQuiz`/`Answer`/… и читает её `Mode`.

**Tech Stack:** Godot 4.7.2 mono (`C:\w\godot\Godot_v4.7.2-stable_mono_win64`), рендер GL Compatibility, .NET SDK 8 (`C:\Program Files\dotnet\dotnet.exe`), C# 12, xUnit, System.Text.Json; Node.js 24 + `mathjax-full` (только инструмент подготовки SVG).

**Spec:** `docs/superpowers/specs/2026-10-30-math-survive-design.md`

**Вне этого плана (отдельные планы позже):** этап 3 — импортёр готовых квизов и генератор; этап 4 — меню «область → раздел», меню персонажа, `settings.ini`, `save.json`; этап 5 — Mixamo-графика и звук, сборка. Здесь настройки — значения по умолчанию в `GameConfig`, пакет — один тестовый раздел, сохранения нет.

## Global Constraints

- Godot 4.7.2 mono, C#. Редактор: `C:\w\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`, консольный: `...\Godot_v4.7.2-stable_mono_win64_console.exe` (ниже — `$GODOT_CON`). `dotnet` может отсутствовать в PATH уже открытых оболочек — тогда полный путь `C:\Program Files\dotnet\dotnet.exe`.
- Платформа Windows 10/11, только основной монитор; масштаб Windows 100–150% должен работать.
- Управление только мышью; окно игры не забирает фокус клавиатуры у рабочих окон.
- Клики мимо элементов игры проходят насквозь к окнам под ней; при этом всё, что игра рисует (зомби, вспышки, сводка), видно.
- Патроны во время игры нигде не показываются (только в статистике).
- Игровое время течёт только пока квиз раскрыт.
- Значения механики по умолчанию (из спеки): интервал орды 30 игровых минут; пауза 10 реальных минут; старт 30 патронов; +3 патрона за верный; точность старт 100%, +5% за верный (≤100%), −10% за неверный (≥20%); зомби в орде N = 5 + 3·(N−1); 1 попадание на зомби; стрельба с трети ширины экрана; уровень +1 после 3 верных подряд, −1 после 2 неверных подряд (≥1); орда длится 1–2 минуты.
- Game over: у персонажа стоит дошедший зомби и патронов 0 (дошедший зомби останавливается, персонаж стреляет в упор, пока есть патроны).
- Коммиты: в рабочую ветку `stage0-2` в worktree `C:\wt\math-survive-s02`; в конце — fast-forward в `master` и push (в этом проекте пользователь разрешил коммиты в `master`). Без подписи Claude и без trailer `Co-Authored-By`.
- В `core/` нет `using Godot`. Случайность в логике — только через переданный `System.Random`.
- Пакеты вопросов читаются как сырые файлы (`FileAccess`), Godot их не импортирует (`.gdignore`). Корень пакетов: в редакторе `res://packs`, в собранной игре — папка `packs` рядом с exe (`PacksRoot`, Task 7).

## Review Focus

1. **Клики насквозь и видимость в каждом режиме** (ожидание, квиз, орда, сводка, статистика): клик по рабочему столу/окну/панели задач вне нарисованных элементов игры доходит до них, а зомби и сводка видны. → проверка пользователем в Task 8 Step 8, Task 10 Step 4 и Task 12.
2. **Фокус клавиатуры:** печатаешь в Блокноте, кликаешь «!» и вариант ответа — ввод продолжает идти в Блокнот. → проверка пользователем в Task 8 Step 8 и Task 12.
3. **Масштаб Windows 125%/150%, панель задач не снизу или автоскрытие:** персонаж стоит на нижнем крае рабочей области, его размеры, отступы, текст и формулы масштабируются по DPI; автоскрытая панель выезжает. → тест `PlacementTests.Scales_with_ui_scale` (Task 8), `UiScale` из `ScreenGetDpi` (Task 7), окно на 1 px ниже экрана при совпадении рабочей области с экраном (Task 8).
4. **Двойной клик по варианту ответа:** засчитывается один ответ. → тест `Answer_in_feedback_mode_is_ignored` (Task 5).
5. **Длинная текстовая задача:** облачко помещается в рабочую область, варианты столбиком. → тесты `QuizLayoutTests` (Task 9).

---

## Структура файлов

```
math-survive/
  project.godot                 # Godot-проект (Task 1, ключи окна — Task 8)
  MathSurvive.csproj            # Godot.NET.Sdk, ссылка на core (Task 1)
  MathSurvive.sln               # только MathSurvive + Core, с конфигурациями Export* (Task 1)
  nuget.config                  # nuget.org + локальные nupkgs Godot (Task 1)
  .gitignore                    # .godot/, bin/, obj/, node_modules/, *.user, .superpowers/ (Task 1)
  core/.gdignore, tests/.gdignore, tools/.gdignore, packs-src/.gdignore, packs/.gdignore  (Task 1)
  core/MathSurvive.Core/
    MathSurvive.Core.csproj     # net8.0 classlib (Task 1)
    GameConfig.cs, CombatState.cs, ShotResolver.cs, HordePlanner.cs        (Task 2)
    DifficultyTracker.cs, PauseState.cs, GameClock.cs                      (Task 3)
    Questions/Question.cs, PackLoader.cs, QuestionPicker.cs                (Task 4)
    Stats/RunStats.cs, Session/GameMode.cs, Session/QuizView.cs, Session/GameSession.cs  (Task 5)
    Layout/Placement.cs         (Task 8)
    Layout/QuizLayout.cs        (Task 9)
  tests/MathSurvive.Core.Tests/ # xUnit, по файлу тестов на класс
  tools/tex2svg/package.json, build-pack.mjs                               (Task 6)
  packs-src/math/test/questions.src.json                                    (Task 6)
  packs/math/area.json, packs/math/test/{section.json,questions.json,svg/*.svg}, packs/SOURCES.md  (Task 6)
  scenes/Main.tscn              # корневая сцена (Task 1, дополняется 7–11)
  scripts/
    Main.cs                     # корень: держит GameSession, связывает виды
    DebugArgs.cs, DebugCapture.cs (автозагрузка), UiScale.cs, PacksRoot.cs, SvgTextures.cs  (Task 7)
    DesktopOverlay.cs, Win32.cs, Stage.cs, PlayerView.cs, DoorView.cs, BubbleView.cs        (Task 8)
    QuizPanel.cs                (Task 9)
    HordeDirector.cs, ZombieView.cs                                          (Task 10)
    SummaryToast.cs, StatsPanel.cs                                           (Task 11)
```

---

### Task 0 (предусловие, координатор): .NET SDK 8 и worktree

**Модель:** — (координатор)

- [x] **Step 1:** Установить .NET SDK 8: `winget install Microsoft.DotNet.SDK.8` — с согласия пользователя.
- [x] **Step 2:** Проверить: `dotnet --list-sdks` → строка `8.0.x`.
- [ ] **Step 3:** Создать worktree: `git -c core.longpaths=true worktree add -b stage0-2 C:/wt/math-survive-s02 master`, затем в нём `git config core.longpaths true`.

---

### Task 1: Каркас решения — Godot-проект, Core, тесты

**Модель:** sonnet

**Files:**
- Create: `project.godot`, `MathSurvive.csproj`, `MathSurvive.sln`, `nuget.config`, `core/MathSurvive.Core/MathSurvive.Core.csproj`, `core/MathSurvive.Core/AssemblyMarker.cs`, `tests/MathSurvive.Core.Tests/MathSurvive.Core.Tests.csproj`, `tests/MathSurvive.Core.Tests/SmokeTests.cs`, `scenes/Main.tscn`, `scripts/Main.cs`, пустые `.gdignore` в `core/`, `tests/`, `tools/`, `packs-src/`, `packs/`
- Modify: `.gitignore`

**Interfaces:**
- Produces: сборка `MathSurvive.Core` (namespace `MathSurvive.Core`); тестовый проект; Godot-сборка `MathSurvive` (namespace `MathSurvive.Game`); главная сцена `res://scenes/Main.tscn` с корнем `Node3D` «Main» и скриптом `Main`.

- [ ] **Step 1: Файлы проекта.**
  - `project.godot`: первая строка `config_version=5`; `[application] config/name="Math Survive"`, `run/main_scene="res://scenes/Main.tscn"`, `config/features=PackedStringArray("4.7", "C#", "GL Compatibility")`; `[dotnet] project/assembly_name="MathSurvive"`; `[rendering] renderer/rendering_method="gl_compatibility"`, `renderer/rendering_method.mobile="gl_compatibility"`.
  - `MathSurvive.csproj`: `<Project Sdk="Godot.NET.Sdk/4.7.2">`, `TargetFramework net8.0`, `EnableDynamicLoading true`, `RootNamespace MathSurvive.Game`, `<Compile Remove="core/**;tests/**;tools/**;packs-src/**" />`, `<ProjectReference Include="core/MathSurvive.Core/MathSurvive.Core.csproj" />`.
  - `MathSurvive.Core.csproj`: `Microsoft.NET.Sdk`, net8.0, `Nullable enable`, `LangVersion 12`. `AssemblyMarker.cs`: `public static class AssemblyMarker {}`.
  - Тестовый проект: net8.0, пакеты `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio` (последние стабильные через `dotnet add package`), ссылка на Core. **В `MathSurvive.sln` не входит** (Godot собирает `.sln`).
  - `MathSurvive.sln`: проекты `MathSurvive` и `MathSurvive.Core`; конфигурации решения `Debug`, `ExportDebug`, `ExportRelease`; для `MathSurvive` — одноимённые, для Core: `Debug→Debug`, `ExportDebug→Debug`, `ExportRelease→Release`.
  - `nuget.config`: источники `nuget.org` и `C:\w\godot\Godot_v4.7.2-stable_mono_win64\GodotSharp\Tools\nupkgs`.
  - `.gitignore`: добавить `.godot/`, `bin/`, `obj/`, `node_modules/`, `*.user`.
  - `scenes/Main.tscn`: корень `Node3D` «Main» со скриптом `res://scripts/Main.cs` (`public partial class Main : Node3D` в namespace `MathSurvive.Game`, пустой `_Ready`).
- [ ] **Step 2: Smoke-тест** `SmokeTests.Core_assembly_loads`: `Assert.Equal("MathSurvive.Core", typeof(MathSurvive.Core.AssemblyMarker).Assembly.GetName().Name);`
- [ ] **Step 3: Сборка и тесты.** Run: `dotnet build MathSurvive.sln` → `Build succeeded`, 0 ошибок. Run: `dotnet test tests/MathSurvive.Core.Tests` → `Passed! … Failed: 0`.
- [ ] **Step 4: Godot.** Run: `$GODOT_CON --headless --path . --build-solutions --quit` → без `ERROR`. Затем `$GODOT_CON --headless --path . --quit-after 60` → без `ERROR`/`SCRIPT ERROR`.
- [ ] **Step 5: Commit** `git add -A && git commit -m "Каркас: Godot C#-проект, Core и тесты"`

---

### Task 2: Core — настройки, бой, выстрел, размер орды

**Модель:** sonnet

**Files:**
- Create: `core/MathSurvive.Core/GameConfig.cs`, `CombatState.cs`, `ShotResolver.cs`, `HordePlanner.cs`
- Test: `tests/MathSurvive.Core.Tests/CombatStateTests.cs`, `ShotResolverTests.cs`, `HordePlannerTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record GameConfig` (свойства `init`, значения по умолчанию): `TimeSpan HordeInterval = 30 мин`, `TimeSpan PauseDuration = 10 мин`, `int StartAmmo = 30`, `int AmmoPerCorrect = 3`, `double StartAccuracy = 1.0`, `double AccuracyUp = 0.05`, `double AccuracyDown = 0.10`, `double MinAccuracy = 0.20`, `int HordeBase = 5`, `int HordeGrowth = 3`, `double FireRangeFraction = 1.0/3`, `int LevelUpStreak = 3`, `int LevelDownStreak = 2`, `TimeSpan FeedbackDuration = 1.5 с`, `TimeSpan HordeSummaryDuration = 4 с`, `double ZombieSpeedPx = 40` (логические px/с), `double ZombieSpawnIntervalSec = 8`, `double FireIntervalSec = 0.8`, `int IdleMaxFps = 15`, `int ActiveMaxFps = 60`. `public static GameConfig Default { get; } = new();` (Орда 1 при 1920 px: ≈ 4·8 + 31 ≈ 63 с.)
  - `public sealed class CombatState { CombatState(GameConfig c); int Ammo {get;} double Accuracy {get;} void ApplyAnswer(bool correct); bool TryConsumeAmmo(); }`
  - `public enum ShotOutcome { NoAmmo, Miss, Hit }`
  - `public static class ShotResolver { static ShotOutcome Fire(CombatState s, Random rng); }` — тратит патрон; попадание, если `rng.NextDouble() < s.Accuracy`.
  - `public static class HordePlanner { static int ZombieCount(int hordeNumber, GameConfig c); }` — `hordeNumber` с 1.

- [ ] **Step 1: Тесты.**
  - `CombatStateTests.Starts_with_config_values`: `Ammo == 30`, `Accuracy == 1.0`.
  - `Correct_answer_adds_ammo_and_accuracy_capped`: `ApplyAnswer(false)` (0.9), `ApplyAnswer(true)` → `Ammo == 33`, `Accuracy` ≈ 0.95 (допуск 1e-9); ещё 2× `true` → `Accuracy == 1.0`.
  - `Wrong_answer_reduces_accuracy_floored`: 10× `ApplyAnswer(false)` → `Accuracy` ≈ 0.20, `Ammo == 30`.
  - `TryConsumeAmmo_stops_at_zero`: `StartAmmo = 2` → true, true, false; `Ammo == 0`.
  - `ShotResolverTests.No_ammo_returns_NoAmmo` (`StartAmmo = 0`).
  - `Full_accuracy_always_hits`: `StartAmmo = 100`, 100 выстрелов, `new Random(1)` → все `Hit`.
  - `Accuracy_drives_hit_rate`: `StartAccuracy = 0.2`, `StartAmmo = 10000`, 10000 выстрелов, `new Random(42)` → доля `Hit` в [0.17, 0.23].
  - `HordePlannerTests`: `ZombieCount(1)==5`, `(2)==8`, `(5)==17`.
- [ ] **Step 2:** `dotnet test tests/MathSurvive.Core.Tests` → FAIL (типов нет).
- [ ] **Step 3:** Реализовать типы по Interfaces.
- [ ] **Step 4:** `dotnet test tests/MathSurvive.Core.Tests` → все PASS.
- [ ] **Step 5: Commit** `"Core: настройки, патроны, точность, выстрел, размер орды"`

---

### Task 3: Core — уровень сложности, пауза, игровое время

**Модель:** sonnet

**Files:**
- Create: `core/MathSurvive.Core/DifficultyTracker.cs`, `PauseState.cs`, `GameClock.cs`
- Test: `tests/MathSurvive.Core.Tests/DifficultyTrackerTests.cs`, `PauseStateTests.cs`, `GameClockTests.cs`

**Interfaces:**
- Consumes: `GameConfig` (Task 2).
- Produces:
  - `public sealed class DifficultyTracker { DifficultyTracker(GameConfig c, int maxLevel, int level = 1); int Level {get;} int MaxLevel {get;} void RecordAnswer(bool correct); }` — серия сбрасывается при смене знака ответа и после смены уровня; уровень в [1, maxLevel].
  - `public sealed class PauseState { bool Available {get;} (изначально true); DateTime? Until {get;} bool TryStart(DateTime nowUtc, TimeSpan duration); bool IsActive(DateTime nowUtc); void OnCorrectAnswer(); }` — `TryStart` при `Available` ставит `Until = now + duration`, `Available = false`, возвращает true; иначе false.
  - `public sealed class GameClock { TimeSpan Elapsed {get;} void Advance(TimeSpan delta); bool IsHordeDue(TimeSpan interval); void Reset(); }`

- [ ] **Step 1: Тесты.**
  - `Three_correct_in_a_row_raise_level` (`maxLevel: 5`): 3× true → 2; ещё 2× true → 2; третий → 3.
  - `Streak_breaks_on_wrong`: true, true, false, true → 1.
  - `Two_wrong_lower_level_not_below_one`: `level: 3` → false, false → 2; false, false → 1; false, false → 1.
  - `Level_capped_at_max`: `maxLevel: 2`, 9× true → 2.
  - `Pause_is_single_use_until_correct`: `TryStart(t0, 10мин)` → true; `IsActive(t0+5мин)` true; `IsActive(t0+10мин)` false; `TryStart(t0+11мин, …)` false; `OnCorrectAnswer()`; `TryStart(…)` true.
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
  - `public sealed class PackFormatException(string message) : Exception(message);`
  - `public static class PackLoader { static QuestionPack Parse(string sectionJson, string questionsJson); }` — JSON snake_case как в спеке (`prompt`, `prompt_svg`, `options[].text|svg`, `correct`, `level`, `id`); `questions.json` — массив. `PackFormatException` (сообщение содержит `id`), если: не 4 варианта; `correct` вне 0..3; у варианта нет ни `text`, ни `svg`; у вопроса нет ни `prompt`, ни `prompt_svg`; повтор `id`. Пути SVG остаются относительными к папке раздела.
  - `public sealed class QuestionPicker { QuestionPicker(IReadOnlyList<Question> all, Random rng); Question Next(int level, ISet<string> used); Question? Find(string id); }` — случайный вопрос уровня, чьего `Id` нет в `used`, и добавляет `Id` в `used`; если все вопросы уровня в `used` — удаляет их id из `used` и выбирает заново; если на уровне вопросов нет — ищет уровни `level−1 … 1` по убыванию, затем `level+1 … max` по возрастанию; пустой пакет → `InvalidOperationException`.

- [ ] **Step 1: Тесты.**
  - `Parses_svg_and_text_questions`: JSON с SVG-вопросом (пример из спеки) и текстовым (`"prompt":"2+2=?"`, варианты `text` "3","4","5","6", `correct` 1) → 2 вопроса; `Options[2].Svg == "svg/fractions-L3-0042-a2.svg"`, `Correct == 2`; у второго `Options[0].Text == "3"`.
  - `Rejects_three_options`, `Rejects_correct_out_of_range`, `Rejects_duplicate_id`, `Rejects_option_without_text_and_svg` → `Assert.Throws<PackFormatException>`, `Message` содержит id.
  - `Picks_all_level_questions_before_repeating`: 3 вопроса уровня 1, `new Random(7)`: 3 вызова → 3 разных id; 4-й не бросает, после него `used.Count == 1`.
  - `Falls_back_lower_levels_first`: вопросы уровней 1 и 4; `Next(3, …)` → уровень 1. Только уровень 4 → `Next(2, …)` → уровень 4.
  - `Find_returns_by_id_or_null`.
  - `Empty_pack_throws`.
- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Реализовать (System.Text.Json, явные `JsonPropertyName` или `JsonNamingPolicy.SnakeCaseLower`).
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
- Produces (namespaces `MathSurvive.Core.Session`, `MathSurvive.Core.Stats`):
  - `public enum GameMode { Idle, QuizOpen, Feedback, Paused, Horde, HordeSummary, GameOver }`
  - `public sealed record QuizView(Question Question, IReadOnlyList<int> Order);` — `Order[displayIndex]` = индекс варианта в `Question.Options`.
  - `public sealed record AnswerResult(bool Correct, int ChosenDisplay, int CorrectDisplay);`
  - `public sealed record HordeInfo(int Number, int ZombieCount);`
  - `public sealed class SectionStats { int Correct {get;set;} int Wrong {get;set;} int MaxLevel {get;set;} }`
  - `public sealed class RunStats { int Correct {get;} int Wrong {get;} TimeSpan AnswerTime {get;} TimeSpan AverageAnswerTime {get;} IReadOnlyDictionary<string, SectionStats> Sections {get;} int ZombiesKilled {get;} int HordesSurvived {get;} int AmmoSpent {get;} void RecordAnswer(string sectionId, bool correct, TimeSpan time, int level); void RecordShot(); void RecordKills(int n); void RecordHordeSurvived(); void MergeInto(RunStats lifetime); }` — `AverageAnswerTime` = 0 при отсутствии ответов.
  - `public sealed class GameSession`:
    - `GameSession(GameConfig config, QuestionPack pack, Random rng)` — сам создаёт `DifficultyTracker(config, pack.Section.Levels)`, `QuestionPicker(pack.Questions, rng)` и внутреннее множество `used`.
    - `GameConfig Config {get;}`, `QuestionPack Pack {get;}`, `GameMode Mode {get;}`, `QuizView? CurrentQuiz {get;}`, `AnswerResult? LastAnswer {get;}`, `HordeInfo? CurrentHorde {get;}`, `int LastHordeKills {get;}`, `int HordeNumber {get;}` (номер следующей/текущей орды, с 1), `CombatState Combat {get;}`, `DifficultyTracker Difficulty {get;}`, `PauseState Pause {get;}`, `GameClock Clock {get;}`, `RunStats Run {get;}`, `RunStats Lifetime {get;}`
    - `void OpenQuiz(string? forcedQuestionId = null)` (forced — для отладки: если `CurrentQuiz == null` и id найден, берёт его), `void CollapseQuiz()`, `AnswerResult? Answer(int displayIndex)`, `bool StartPause(DateTime nowUtc)`, `void Tick(TimeSpan delta, DateTime nowUtc)`, `ShotOutcome Fire()`, `void FinishHorde(bool survived, int killed)`, `void StartNewRun()`, `void ForceHordeNow()` (отладка: в Idle/QuizOpen — сразу начать орду по правилу строки «Clock.IsHordeDue»)
    - `event Action<HordeInfo>? HordeStarted`, `event Action<GameMode>? ModeChanged`

- [ ] **Step 1: Таблица переходов (реализовать ровно её).** Вызов в неподходящем режиме ничего не делает (`Answer` → `null`, `StartPause` → `false`, `Fire` → `NoAmmo` без траты). При каждой смене режима — `ModeChanged`.

| Из | Вызов / условие | В | Эффект |
|---|---|---|---|
| Idle | `OpenQuiz(f)` | QuizOpen | если `CurrentQuiz == null` — вопрос `f` (если задан и найден) или `Picker.Next(Difficulty.Level, used)`; порядок вариантов — перестановка через `rng` |
| QuizOpen | `CollapseQuiz()` | Idle | вопрос и накопленное на него время сохраняются |
| QuizOpen | `Tick(d)` | QuizOpen | `Clock.Advance(d)`, время на текущий вопрос += d; затем проверка следующей строки |
| QuizOpen / Idle | `Clock.IsHordeDue(HordeInterval)` в `Tick`, или `ForceHordeNow()` | Horde | вопрос не засчитывается и остаётся `CurrentQuiz`; `CurrentHorde = new(HordeNumber, HordePlanner.ZombieCount(HordeNumber, Config))`; `HordeStarted` |
| QuizOpen | `Answer(i)` | Feedback | `Combat.ApplyAnswer`, `Difficulty.RecordAnswer`, при верном `Pause.OnCorrectAnswer()`, `Run.RecordAnswer(Pack.Section.Id, correct, времяНаВопрос, Difficulty.Level)`; `LastAnswer` |
| QuizOpen | `StartPause(now)` и `Pause.TryStart(now, PauseDuration)` | Paused | вопрос сохраняется |
| Feedback | `Tick`, суммарно ≥ `FeedbackDuration` | Idle | `CurrentQuiz = null`, `LastAnswer = null` |
| Paused | `Tick(_, now)` и `!Pause.IsActive(now)` | Idle | |
| Horde | `Fire()` | Horde | `ShotResolver.Fire(Combat, rng)`; если не `NoAmmo` — `Run.RecordShot()` |
| Horde | `FinishHorde(true, k)` | HordeSummary | `LastHordeKills = k`, `Run.RecordKills(k)`, `Run.RecordHordeSurvived()`, `Clock.Reset()`, `HordeNumber++` |
| HordeSummary | `Tick`, суммарно ≥ `HordeSummaryDuration` | Idle | `CurrentHorde = null` |
| Horde | `FinishHorde(false, k)` | GameOver | `LastHordeKills = k`, `Run.RecordKills(k)`, `Run.MergeInto(Lifetime)` |
| GameOver | `StartNewRun()` | Idle | новые `Combat`, `Pause`, `Clock`, `Run`; `HordeNumber = 1`; `CurrentQuiz = null`; `CurrentHorde = null`; `Difficulty` и `Lifetime` сохраняются |

  В Idle/Paused/Feedback/HordeSummary/Horde/GameOver игровое время не идёт.

- [ ] **Step 2: Тесты.** Общий помощник: пакет `SectionInfo("test", "Тест", 3)`, 10 вопросов уровня 1 (`q1..q10`, у всех `Correct = 0`) и 2 уровня 2; сессия `new GameSession(GameConfig.Default with {...}, pack, new Random(1))`; помощник `AnswerCorrect(s)` выбирает display-индекс, где `Order[i] == 0`.
  - `Starts_idle_and_time_does_not_flow`: `Tick(1ч)` → `Idle`, `Clock.Elapsed == 0`.
  - `Open_collapse_keeps_same_question`.
  - `Forced_question_is_opened`: `OpenQuiz("q7")` → `CurrentQuiz.Question.Id == "q7"`.
  - `Correct_answer_flow`: `OpenQuiz`, `AnswerCorrect` → `Correct`, `Feedback`, `Combat.Ammo == 33`; `Tick(1.5с)` → `Idle`, `CurrentQuiz == null`.
  - `Wrong_answer_reports_correct_display_index`: ответ с `Order[i] != 0` → `!Correct`, `Order[CorrectDisplay] == 0`, `Combat.Accuracy` ≈ 0.9.
  - `Answer_in_feedback_mode_is_ignored` (Review Focus 4): два `Answer` подряд → второй `null`, `Run.Correct + Run.Wrong == 1`.
  - `Options_are_shuffled_but_consistent`: при `new Random(3)` у хотя бы одного из 10 вопросов `Order` ≠ `[0,1,2,3]`; каждый `Order` — перестановка 0..3.
  - `Pause_hides_quiz_until_time_passes_and_is_single_use`: `OpenQuiz`, `StartPause(t0)` → true, `Paused`; `Tick(_, t0+9мин)` → Paused; `Tick(_, t0+10мин)` → Idle; `OpenQuiz`, `StartPause(…)` → false.
  - `Horde_starts_when_game_time_reaches_interval_and_question_is_kept`: `HordeInterval = 1 мин`; `OpenQuiz`; `Tick(61с)` → `Horde`, событие `(1, 5)`, `Run.Correct + Run.Wrong == 0`, `CurrentQuiz != null`.
  - `Survived_horde_returns_to_idle_and_next_horde_is_bigger`: `FinishHorde(true, 5)` → HordeSummary, `LastHordeKills == 5`; `Tick(4с)` → Idle; `Clock.Elapsed == 0`; `HordeNumber == 2`; следующая орда → событие `(2, 8)`.
  - `Lost_horde_is_game_over_and_new_run_resets_combat_keeps_difficulty`: 3 верных → `Difficulty.Level == 2`; `ForceHordeNow()`; `FinishHorde(false, 3)` → GameOver, `Lifetime.ZombiesKilled == 3`; `StartNewRun()` → Idle, `Combat.Ammo == 30`, `Difficulty.Level == 2`, `Run.Correct == 0`.
  - `Fire_spends_ammo_and_records_shots`: `StartAmmo = 2`, `ForceHordeNow()`; `Fire()` ×3 → третий `NoAmmo`, `Run.AmmoSpent == 2`.
  - `RunStatsTests.Average_and_sections`: ответы ("test", 10с, верно, ур.1) и ("test", 20с, неверно, ур.2) → `AverageAnswerTime == 15с`, `Sections["test"]` = Correct 1, Wrong 1, MaxLevel 2; `MergeInto` суммирует счётчики и берёт max по `MaxLevel`.
- [ ] **Step 3:** `dotnet test` → FAIL.
- [ ] **Step 4:** Реализовать по таблице.
- [ ] **Step 5:** `dotnet test` → все PASS.
- [ ] **Step 6: Commit** `"Core: машина состояний игры и статистика"`

---

### Task 6: Инструмент LaTeX → SVG и тестовый пакет

**Модель:** sonnet

**Files:**
- Create: `tools/tex2svg/package.json`, `tools/tex2svg/build-pack.mjs`, `packs-src/math/test/questions.src.json`, `packs/math/area.json` (`{"id":"math","title":"Математика"}`), `packs/math/test/section.json` (`{"id":"test","title":"Тест","levels":3}`), сгенерированные `packs/math/test/questions.json` и `packs/math/test/svg/*.svg`, `packs/SOURCES.md`
- Test: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`

**Interfaces:**
- Consumes: формат пакета (Task 4).
- Produces: `node tools/tex2svg/build-pack.mjs <src.json> <outDir>`; пакет в `packs/math/test/`.

- [ ] **Step 1: Исходник `questions.src.json`** — массив вопросов в формате пакета, но вместо `prompt_svg`/`svg` поля `prompt_tex`/`tex` (LaTeX без `$`). 9 вопросов, по 3 на уровни 1–3: `\frac{3}{4}+\frac{1}{6}`; степень с отрицательным показателем; `\sqrt[3]{27}`; производная `f(x)=3x^{4}-\frac{2}{x}`; производная `\sin(\alpha x)`; `\int_{0}^{\pi}\sin x\,dx`; неопределённый интеграл с `+C`; `\sum_{k=1}^{n}k`; и один текстовый вопрос с условием ≥ 200 символов и текстовыми вариантами. Id вида `test-L<уровень>-<nn>`. Верные ответы проверить вручную.
- [ ] **Step 2: `build-pack.mjs`.** Для каждого `*_tex`: MathJax `tex2svg` (`mathjax-full`, `fontCache: 'none'`), оставить самодостаточный `<svg>` без обёртки `mjx-container`; заменить `width`/`height` в `ex` на `px` (1ex = 8px); заменить `currentColor` на `#1a1a1a`; записать `svg/<id>-q.svg`, `svg/<id>-a<k>.svg`; записать `questions.json` с путями `svg/...`. Текстовые поля копировать как есть.
- [ ] **Step 3: Сборка.** Run: `cd tools/tex2svg && npm install && node build-pack.mjs ../../packs-src/math/test/questions.src.json ../../packs/math/test` → 9 записей. Проверка: `grep -l 'width="[0-9.]*ex"\|height="[0-9.]*ex"\|currentColor' ../../packs/math/test/svg/*.svg` → пустой вывод.
- [ ] **Step 4: Тест `BuiltPackTests.Parses_built_test_pack`**: найти корень репозитория (вверх от `AppContext.BaseDirectory` до файла `MathSurvive.sln`), `PackLoader.Parse` для `packs/math/test/section.json` и `questions.json` → 9 вопросов, `Section.Levels == 3`, все SVG-пути существуют.
- [ ] **Step 5:** `dotnet test` → PASS. `packs/SOURCES.md`: «math/test — составлено вручную».
- [ ] **Step 6: Commit** `"Инструмент LaTeX→SVG и тестовый пакет"`

---

### Task 7: Отладочный захват, масштаб, загрузка формул; проверка SVG MathJax в Godot

**Модель:** opus

**Files:**
- Create: `scripts/DebugArgs.cs`, `scripts/DebugCapture.cs`, `scripts/UiScale.cs`, `scripts/PacksRoot.cs`, `scripts/SvgTextures.cs`, `scenes/SvgGallery.tscn` + `scripts/SvgGallery.cs`
- Modify: `project.godot` (автозагрузка `DebugCapture`)

**Interfaces:**
- Produces:
  - `public sealed class DebugArgs { static DebugArgs Current {get;} string? ShotPath; double? QuitAfterSec; bool OpenQuiz; string? QuestionId; bool HordeNow; int? Ammo; }` — из `OS.GetCmdlineUserArgs()`: `--shot=<png>`, `--quit-after=<сек>`, `--open-quiz`, `--question=<id>`, `--horde-now`, `--ammo=<n>`.
  - `DebugCapture : Node` (автозагрузка): если задан `QuitAfterSec` — по таймеру ждёт `RenderingServer.FramePostDraw`, сохраняет `GetViewport().GetTexture().GetImage().SavePng(ShotPath)` (если задан) и `GetTree().Quit()`. Работает для любой сцены.
  - `public static class UiScale { static float Value {get;} }` — `DisplayServer.ScreenGetDpi(DisplayServer.GetPrimaryScreen()) / 96f` (на Windows `ScreenGetScale` всегда 1.0 — не использовать).
  - `public static class PacksRoot { static string Path {get;} }` — `res://packs`, если `OS.HasFeature("editor")`, иначе `<папка exe>/packs`.
  - `public static class SvgTextures { static ImageTexture Load(string absPath, float scale); }` — `var img = new Image(); img.LoadSvgFromString(FileAccess.GetFileAsString(absPath), scale * UiScale.Value)` (проверить `Error.Ok`, иначе `GD.PushError` и текстура-заглушка), затем `ImageTexture.CreateFromImage(img)`; кэш по `(path, scale)`.

- [ ] **Step 1:** Реализовать `DebugArgs`, `DebugCapture` (+ автозагрузка в `project.godot`), `UiScale`, `PacksRoot`, `SvgTextures`.
- [ ] **Step 2:** Витрина `SvgGallery.tscn`: все SVG тестового пакета (`PacksRoot.Path + "/math/test/svg"`) `TextureRect`-ами на светлом фоне, масштаб 1.5.
- [ ] **Step 3:** Run: `$GODOT_CON --path . res://scenes/SvgGallery.tscn -- --shot=C:/Users/SIAMS/AppData/Local/Temp/svg-gallery.png --quit-after=3` → PNG создан. Прочитать PNG (Read) и проверить: дроби с чертой, ∫ с пределами, корень с показателем, индексы/степени, греческие буквы, `+C`, цвет тёмный.
- [ ] **Step 4: Решение.** Всё корректно — строкой в «Ход выполнения»: «SVG MathJax: ОК». Нет — по порядку: (а) другие настройки вывода MathJax в `build-pack.mjs`; (б) растеризация в PNG генератором (`@resvg/resvg-js`, ×2 для чёткости) и загрузка PNG (`SvgTextures` выбирает по расширению); выбранный путь — в «Ход выполнения». Ни (а), ни (б) — BLOCKED.
- [ ] **Step 5: Commit** `"Отладочный захват, масштаб DPI, загрузка формул; проверка SVG MathJax"` (витрина остаётся как инструмент проверки пакетов).

---

### Task 8: Окно поверх рабочего стола, сцена, персонаж, дверь, «!»

**Модель:** opus

**Files:**
- Create: `core/MathSurvive.Core/Layout/Placement.cs`, `tests/MathSurvive.Core.Tests/PlacementTests.cs`, `scripts/DesktopOverlay.cs`, `scripts/Win32.cs`, `scripts/Stage.cs`, `scripts/PlayerView.cs`, `scripts/DoorView.cs`, `scripts/BubbleView.cs`
- Modify: `project.godot`, `scenes/Main.tscn`, `scripts/Main.cs`

**Interfaces:**
- Consumes: `GameSession` (Task 5), `PackLoader` (Task 4), `DebugArgs`, `UiScale`, `PacksRoot` (Task 7).
- Produces:
  - `public readonly record struct PixelRect(int X, int Y, int W, int H)` (namespace `MathSurvive.Core.Layout`).
  - `public static class Placement { static (double x, double y) PlayerFeet(int windowW, int windowH, double scale); static (double x, double y) DoorFeet(int windowW, int windowH, double scale); static double FireLineX(int windowW, int windowH, double scale, double fireRangeFraction); }` — координаты окна (начало — левый верхний угол окна): ноги персонажа `x = 70·scale, y = windowH`; дверь `x = 24·scale, y = windowH`; линия начала стрельбы `PlayerFeet.x + windowW·fraction`.
  - `DesktopOverlay : Node` — `void RegisterHitArea(Func<Rect2?> area)` (зоны, где клики принимает игра; `null` — зона скрыта); обеспечивает: клики вне зон проходят насквозь, отрисовка всего окна не обрезается.
  - `Stage : Node3D` — ортокамера; `Vector3 PixelToWorld(Vector2 px)`, `Vector2 WorldToPixel(Vector3 w)`.
  - `PlayerView`, `DoorView`, `BubbleView` — `Rect2 ScreenRect {get;}`, событие `Clicked`.

- [ ] **Step 1: Тесты `PlacementTests`:** `PlayerFeet(1920, 1040, 1.0) == (70, 1040)`; `FireLineX(1920, 1040, 1.0, 1.0/3)` ≈ 710 (допуск 1e-6); `Scales_with_ui_scale`: `PlayerFeet(2560, 1400, 1.5) == (105, 1400)`, `DoorFeet(2560, 1400, 1.5) == (36, 1400)`.
- [ ] **Step 2:** `dotnet test` → FAIL; реализовать `Placement`; → PASS.
- [ ] **Step 3: Окно.** `project.godot`: `display/window/size/borderless=true`, `display/window/size/transparent=true`, `display/window/size/always_on_top=true`, `display/window/per_pixel_transparency/allowed=true`, `rendering/viewport/transparent_background=true`, `display/window/stretch/mode="disabled"` (сверить имена ключей с документацией 4.7). `application/run/low_processor_mode=true`. В `DesktopOverlay._Ready`: проверить `DisplayServer.IsWindowTransparencyAvailable()` (false → `GD.PushError` и строка в «Ход выполнения»); окно = `DisplayServer.ScreenGetUsableRect(GetPrimaryScreen())`, обычный оконный режим; если рабочая область совпадает с размером экрана (автоскрытие панели) — высота на 1 px меньше; `GetWindow().ContentScaleFactor = UiScale.Value` (проверить, что UI масштабируется при `stretch=disabled`; если нет — масштаб темы). `Engine.MaxFps`: `IdleMaxFps` в Idle/Paused/QuizOpen/Feedback/GameOver, `ActiveMaxFps` в Horde/HordeSummary (по `ModeChanged`).
- [ ] **Step 4: Клики насквозь без обрезки отрисовки.** `DisplayServer.WindowSetMousePassthrough` **не использовать** (на Windows обрезает отрисовку вне региона). Путь (а): `Win32` — на HWND (`DisplayServer.WindowGetNativeHandle(HandleType.WindowHandle)`) после всей настройки окна Godot добавить `WS_EX_LAYERED` (+ `SetLayeredWindowAttributes(hwnd, 0, 255, LWA_ALPHA)`, если без него окно исчезает) и переключать `WS_EX_TRANSPARENT`: таймер ~30 Гц (не зависит от `MaxFps`) по `GetCursorPos` (в координатах экрана → окна): курсор в одной из зон `RegisterHitArea` — снять `WS_EX_TRANSPARENT`, иначе — поставить. Проверить, что прозрачность фона и альфа сохраняются. Если (а) ломает прозрачность — путь (б): регион `WindowSetMousePassthrough` = один многоугольник из всех **видимых** прямоугольников (зоны кликов + зомби + вспышки + тост), соединённых перемычками нулевой ширины, без перекрытий, обновлять каждый кадр; для (б) вынести построение многоугольника в `core/MathSurvive.Core/Layout/PassthroughPolygon.cs` с тестами (2 непересекающихся прямоугольника → обход без внутренних площадей). Выбранный путь — строкой в «Ход выполнения».
- [ ] **Step 5: Без захвата фокуса.** По порядку, до первого рабочего: (1) `DisplayServer.WindowSetFlag(WindowFlags.NoFocus, true)`; (2) `Win32`: добавить `WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW`; (3) запасной: при клике по зоне игры запомнить `GetForegroundWindow()` до клика и вернуть `SetForegroundWindow` после обработки. Условие: клики Godot получает. Выбранный путь — в «Ход выполнения». Ex-стили выставлять одним местом в `Win32`, не затирая стили Godot (OR к текущему значению).
- [ ] **Step 6: Сцена.** `Stage` с ортокамерой (1 мировая единица = 100 логических px). `PlayerView` — капсула + коробка-«ружьё» вправо на `Placement.PlayerFeet`, высота ≈ 60·scale px; `DoorView` — коричневый прямоугольник ≈ 30×56·scale px на `DoorFeet`; `BubbleView` — жёлтый круг «!» (2D `Control` поверх 3D) над головой, видим только в `Idle`, мигание — переключение видимости таймером 1 Гц (не непрерывный `Tween`). `Main`: грузит пакет `PacksRoot.Path + "/math/test"` через `PackLoader.Parse`, создаёт `GameSession(config, pack, new Random())`, где `config = GameConfig.Default` с поправками из `DebugArgs` (`Ammo` → `StartAmmo`), в `_Process` вызывает `Tick(delta, DateTime.UtcNow)`; `GD.Print` при `ModeChanged` (`"mode: <Mode>"`); клик по двери → `GetTree().Quit()`; клик по «!» → `OpenQuiz(DebugArgs.QuestionId)`. Зоны кликов двери, персонажа и «!» зарегистрированы в `DesktopOverlay`.
- [ ] **Step 7: Скриншот.** Run: `$GODOT_CON --path . -- --shot=C:/Users/SIAMS/AppData/Local/Temp/overlay.png --quit-after=3` → Read PNG: прозрачный фон, видны дверь, персонаж, «!» у нижнего левого края.
- [ ] **Step 8: Проверка пользователем** (координатор просит запустить `Godot_v4.7.2-stable_mono_win64.exe --path C:\wt\math-survive-s02` и проверить): персонаж над панелью задач; клики по рабочему столу/окнам/панели задач рядом и вне персонажа проходят; печать в Блокноте не прерывается кликом по «!»; клик в другое окно не прячет игру; клик по двери закрывает игру; при масштабе Windows ≠ 100% (если есть возможность) размеры пропорциональны. Результат — в «Ход выполнения».
- [ ] **Step 9: Commit** `"Окно поверх рабочего стола, персонаж, дверь и «!»"`

---

### Task 9: Квиз-облачко

**Модель:** opus

**Files:**
- Create: `core/MathSurvive.Core/Layout/QuizLayout.cs`, `tests/MathSurvive.Core.Tests/QuizLayoutTests.cs`, `scripts/QuizPanel.cs`
- Modify: `scenes/Main.tscn`, `scripts/Main.cs`

**Interfaces:**
- Consumes: `GameSession` (`CurrentQuiz`, `LastAnswer`, `Answer`, `CollapseQuiz`, `StartPause`, `Pause.Available`, `Config.PauseDuration`, `HordeStarted`, `ModeChanged`), `SvgTextures.Load`, `PacksRoot`, `DesktopOverlay.RegisterHitArea`, `Placement`, `UiScale`.
- Produces:
  - `public enum OptionsLayout { Grid2x2, Column }`
  - `public static class QuizLayout { static OptionsLayout Choose(IReadOnlyList<(int width, int textLength)> options, int maxGridCellWidth); static PixelRect Clamp(PixelRect panel, PixelRect window, int margin); }` — `Grid2x2`, если у всех вариантов ширина ≤ `maxGridCellWidth` и текст ≤ 24 символов; иначе `Column`. `Clamp`: `window` — в координатах окна `(0, 0, W, H)`; сдвигает панель внутрь с отступом `margin`; если панель выше окна — `Y = margin` (высота не меняется — прокрутка в `QuizPanel`).
  - `QuizPanel : PanelContainer` — `void ShowQuiz(QuizView view, string sectionDir, bool pauseAvailable, TimeSpan pauseDuration)`, `void ShowFeedback(AnswerResult r)`, `void HideQuiz()`, события `OptionChosen(int displayIndex)`, `PauseClicked`, `CollapseClicked`, `Rect2 ScreenRect {get;}`.

- [ ] **Step 1: Тесты `QuizLayoutTests`** (Review Focus 5): 4 SVG шириной 120 при `maxGridCellWidth = 160` → `Grid2x2`; один шириной 300 → `Column`; текстовый вариант 40 символов → `Column`; `Clamp((−50, 900, 400, 300), (0, 0, 1920, 1040), 8)` → `(8, 732, 400, 300)`; панель 400×1200 → `Y == 8`.
- [ ] **Step 2:** `dotnet test` → FAIL; реализовать `QuizLayout`; → PASS.
- [ ] **Step 3: `QuizPanel`.** Облачко над персонажем с хвостиком к голове, светлый фон, скруглённые углы (макет «вариант A» в спеке). Сверху `Prompt` (`Label`, перенос, ширина ≤ 560 логических px) и формула `PromptSvg` (`TextureRect`, `SvgTextures.Load(sectionDir + "/" + path, 1.5f)`). Варианты — `Button`ы с `Icon` (SVG) или текстом в порядке `QuizView.Order`, раскладка по `QuizLayout.Choose` (`maxGridCellWidth = 200·UiScale`). Внизу «⏸ Пауза {pauseDuration.TotalMinutes} мин» — неактивна при `!pauseAvailable`. В правом верхнем углу «▾» — свернуть (`CollapseClicked`). Позиция — над головой персонажа, затем `QuizLayout.Clamp`; если выше окна — `ScrollContainer`.
- [ ] **Step 4: Связка в `Main`.** «!» → `OpenQuiz` → `ShowQuiz(CurrentQuiz, PacksRoot.Path + "/math/test", Pause.Available, Config.PauseDuration)`; `CollapseClicked` → `CollapseQuiz` → `HideQuiz`; `OptionChosen` → `Answer` → `ShowFeedback` (верный — зелёным, выбранный неверный — красным); переход Feedback → Idle (`ModeChanged`) → `HideQuiz`; `PauseClicked` → `StartPause(DateTime.UtcNow)` → `HideQuiz`; `HordeStarted` → `HideQuiz`. Зона квиза в `DesktopOverlay` активна, только пока квиз видим. Отладка: `DebugArgs.OpenQuiz` → `OpenQuiz(DebugArgs.QuestionId)` при старте.
- [ ] **Step 5: Скриншоты.** Run: `$GODOT_CON --path . -- --open-quiz --question=test-L2-01 --shot=C:/Users/SIAMS/AppData/Local/Temp/quiz-formula.png --quit-after=3` (id формульного вопроса из пакета) и то же с id текстового вопроса → `quiz-text.png`. Read PNG: облачко над персонажем, формулы чёткие, длинный текст переносится, варианты столбиком, всё в пределах экрана.
- [ ] **Step 6: Commit** `"Квиз-облачко: формулы, варианты, обратная связь, пауза"`

---

### Task 10: Орда — зомби, автострельба, победа и поражение

**Модель:** opus

**Files:**
- Create: `scripts/HordeDirector.cs`, `scripts/ZombieView.cs`
- Modify: `scripts/Main.cs`, `scenes/Main.tscn`, `scripts/DesktopOverlay.cs` (только если выбран путь (б) Task 8 — регистрация видимых прямоугольников зомби/вспышек)

**Interfaces:**
- Consumes: `GameSession` (`HordeStarted`, `Fire`, `FinishHorde`, `Combat.Ammo`, `Config`, `ForceHordeNow`), `Placement.PlayerFeet/FireLineX`, `Stage.PixelToWorld`, `UiScale`, `DebugArgs.HordeNow`.
- Produces: `HordeDirector : Node3D` — `void Begin(HordeInfo info)`; по окончании сам вызывает `session.FinishHorde(survived, killed)`.

- [ ] **Step 1: Поведение.** `Begin`: спавнить `ZombieCount` зомби (зелёная капсула + голова, рост ≈ 56·scale px) за правым краем (`x = windowW + 40·scale`) с интервалом `ZombieSpawnIntervalSec`; зомби идут влево со скоростью `ZombieSpeedPx·UiScale`. Каждые `FireIntervalSec`, если ближайший зомби левее `FireLineX`: `session.Fire()`; `Hit` → ближайший зомби умирает (исчезает); вспышка на конце ружья при `Hit` и `Miss`. Зомби, дошедший до `PlayerFeet.x + 30·scale`, останавливается («атакует»). Если у персонажа стоит зомби и `Combat.Ammo == 0` → остановить орду, `FinishHorde(false, убито)`. Все заспавнены и убиты → `FinishHorde(true, убито)`. Зомби не кликабельны.
- [ ] **Step 2: Отладка.** `DebugArgs.HordeNow` → `Main` сразу вызывает `session.ForceHordeNow()`.
- [ ] **Step 3: Проверка.** Расчёт при 1920 px и масштабе 1: первый зомби доходит до линии огня ≈ 31 с, до персонажа ≈ 46 с; орда из 5 при полной точности ≈ 64 с.
  - Run `$GODOT_CON --path . -- --horde-now --shot=C:/Users/SIAMS/AppData/Local/Temp/horde.png --quit-after=36` → Read PNG: зомби на экране, первые у линии огня.
  - Run `$GODOT_CON --path . -- --horde-now --ammo=0 --quit-after=60` → в выводе `mode: Horde`, затем `mode: GameOver`.
  - Run `$GODOT_CON --path . -- --horde-now --ammo=30 --quit-after=80` → `mode: Horde`, `mode: HordeSummary`, `mode: Idle`.
- [ ] **Step 4: Проверка пользователем:** запуск с `-- --horde-now`: зомби видны на всём пути, клики по рабочему столу во время орды проходят. Результат — в «Ход выполнения».
- [ ] **Step 5: Commit** `"Орда: зомби, автострельба, победа и поражение"`

---

### Task 11: Сводка орды и экран статистики

**Модель:** sonnet

**Files:**
- Create: `scripts/SummaryToast.cs`, `scripts/StatsPanel.cs`
- Modify: `scripts/Main.cs`, `scenes/Main.tscn`

**Interfaces:**
- Consumes: `GameSession` (`ModeChanged`, `Run`, `Lifetime`, `CurrentHorde`, `LastHordeKills`, `Combat.Ammo`, `Pack.Section.Title`, `StartNewRun`), `RunStats`, `SectionStats` (Task 5), `DesktopOverlay.RegisterHitArea`.
- Produces: `SummaryToast.ShowFor(string text)`, `SummaryToast.HideToast()`; `StatsPanel.ShowStats(RunStats run, RunStats lifetime, int ammoLeft, string sectionTitle)`, событие `NewRunClicked`.

- [ ] **Step 1: `SummaryToast`** — при входе в `HordeSummary`: «Орда {CurrentHorde.Number} отбита, убито {LastHordeKills}» над персонажем; скрыть при выходе из режима.
- [ ] **Step 2: `StatsPanel`** — при `GameOver`, панель над персонажем (зона кликов в `DesktopOverlay`): «Задачи: верно X / неверно Y»; по разделам (`sectionTitle`): верно/неверно, максимальный уровень; «Среднее время на задачу: m:ss»; «Убито зомби: Z»; «Отбито орд: H»; «Патронов потрачено: S, осталось: A»; блок «За всё время» с теми же счётчиками; кнопка «Новая игра» → `StartNewRun()` и скрыть панель.
- [ ] **Step 3: Проверка.** Run `$GODOT_CON --path . -- --horde-now --ammo=0 --shot=C:/Users/SIAMS/AppData/Local/Temp/stats.png --quit-after=55` → Read PNG: панель статистики со всеми строками.
- [ ] **Step 4: Commit** `"Сводка орды и экран статистики"`

---

### Task 12: Сквозная проверка и слияние

**Модель:** — (координатор + пользователь)

- [ ] **Step 1:** `dotnet test tests/MathSurvive.Core.Tests` → все PASS; `$GODOT_CON --headless --path . --build-solutions --quit` → без ошибок.
- [ ] **Step 2: Проверка пользователем** полного цикла (обычный запуск и с `-- --horde-now`): «!» → квиз → ответы (верный/неверный) → «▾» свернуть → пауза → орда → сводка → снова «!»; поражение → статистика → «Новая игра»; Review Focus 1–2 в режимах квиза, орды и статистики; дверь закрывает игру.
- [ ] **Step 3:** Рецензия всей ветки отдельным субагентом.
- [ ] **Step 4:** `git checkout master && git merge --ff-only stage0-2 && git push origin master` (в основной копии `C:\w\godot\math-survive`); удалить worktree `git worktree remove C:/wt/math-survive-s02`.

---

## Ход выполнения

- 2026-10-30 — план составлен (координатор). Спека согласована пользователем.
- 2026-10-30 — выполнено: Task 0 шаги 1–2 — .NET SDK 8.0.427 установлен (winget, с согласия пользователя), `C:\Program Files\dotnet`. В уже открытых оболочках `dotnet` может отсутствовать в PATH — вызывать по полному пути.
- 2026-10-30 — рецензия плана независимым агентом: 17 замечаний, исправлены: клики насквозь без `WindowSetMousePassthrough` (Win32 `WS_EX_TRANSPARENT` по курсору, запасной — многоугольник видимых элементов); масштаб по `ScreenGetDpi/96` вместо `ScreenGetScale`; орда ≈ 1 мин (`ZombieSpeedPx = 40`, `ZombieSpawnIntervalSec = 8`); game over по спеке (зомби у персонажа и 0 патронов; спека уточнена); свернуть квиз — кнопка «▾» (спека уточнена); полный `DebugArgs` + автозагрузка `DebugCapture`; `OpenQuiz(forcedId)`, `ForceHordeNow`, `ModeChanged`; уровни тестового пакета; `StartAmmo = 10000` в тесте точности; порядок поиска уровня в `QuestionPicker`; интерфейс `QuizPanel` (`sectionDir`, пауза, переименование `Show*`, скрытие на орду, вызов `LoadSvgFromString` у экземпляра); `config_version=5`, `EnableDynamicLoading`, GL Compatibility, ключи окна, `IsWindowTransparencyAvailable`; `currentColor` и проверка `grep -l`; `.gdignore` и `PacksRoot`; `low_processor_mode` и мигание таймером; автоскрытая панель; тесты вне `MathSurvive.sln`.
