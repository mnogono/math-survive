# Quiz Sections and Room Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development (recommended) or executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить комнату с пятью иконками выбора, анимированный переход персонажа и 400 вопросов в четырёх разделах математики.

**Architecture:** `QuestionCatalog` хранит пакеты и независимый прогресс разделов, а `GameSession` управляет разрешёнными переходами и текущим вопросом. Неизменяемый snapshot и DTO сохранения остаются в Core; Godot-адаптер задаёт путь `save.json` и подключает файловое сохранение к ответам, выбору и выходу. Godot-слой строит комнату, перемещает существующую модель с clip `walk` и только сообщает Core о выборе; контент остаётся статическими JSON/SVG-пакетами.

**Tech Stack:** Godot 4.7.2 .NET, .NET 8, C# 12, xUnit, Node.js, MathJax 3, JSON/SVG.

**Spec:** `docs/superpowers/specs/2026-10-30-quiz-sections-room-design.md`

## Global Constraints

- Core не зависит от Godot; все правила выбора вопросов, смены режимов, сложности, сохранения и формата пакетов проверяются через `dotnet test`.
- Ровно четыре новых раздела: `arithmetic`, `algebra`, `geometry`, `analysis`; случайный режим не имеет собственного пакета.
- По 100 вопросов на раздел: 40 уровня 1, 35 уровня 2, 25 уровня 3; четыре варианта, zero-based `correct`.
- Вопросы выбираются случайно только на текущем уровне активного раздела, без повторов до исчерпания уровня; fallback на другие уровни запрещён.
- Порядок случайного режима: `arithmetic → algebra → geometry → analysis → arithmetic`.
- У иконок символы `?`, `+ −`, `x²`, `△`, `∫`; персонаж использует существующий clip `walk`.
- Для сложности использовать `GameConfig.LevelUpStreak` и `LevelDownStreak`: 3/2 по умолчанию, сброс противоположной и сработавшей серии, границы 1..3, обнуление серии на пороге даже у границы. Смена раздела, сворачивание и орда не меняют прогресс сложности.
- `save.json` содержит выбор, уровни, correct/wrong streaks и used IDs по уровням каждого раздела. Сохранять после ответа, применённой смены и при выходе; атомарно через временный файл в том же каталоге. Отсутствующий или повреждённый файл даёт defaults. Текущий вопрос, random-курсор и боевое состояние не сохранять.
- Допускаются оригинальные, заимствованные и адаптированные задания из общедоступных материалов, включая ЕГЭ разных лет. Каждое заимствованное или адаптированное задание вручную проверяется на математическую правильность, однозначность и время решения не более 5–10 минут.
- В вопросах сохраняются только итоговые условие, варианты, правильный ответ и необходимые SVG. Не добавлять source/provenance/license metadata в JSON, не вести реестр источников и не изменять `packs/SOURCES.md`.
- Условие может сочетать `prompt`, `prompt_tex` и необязательный `prompt_image` с относительным путём `images/<question-id>.svg`. Рисунки должны быть читаемыми; для геометрии обязательны однозначные подписи и все необходимые данные. Не создавать числовые клоны.
- Не добавлять новые runtime-зависимости; не менять боевой баланс и существующий формат TeX-формул.
- Сохранить существующий `math/test`, девять его вопросов и проходящие тесты одиночного пакета; старый `QuestionPicker.Next` остаётся для совместимости, а каталог использует новый строгий выбор уровня.
- Не включать в коммиты текущие unrelated-изменения из рабочего дерева, кроме файлов `PlayerView.cs`, `HordeDirector.cs`, `ModelVisual.cs`, если задача действительно меняет их поверх существующей работы.

## Review Focus

- Сворачивание, орда и повторный клик не должны продвигать случайный цикл: тесты в Tasks 1–2.
- Смена неотвеченного вопроса не должна менять статистику/бой и не должна сбрасывать сложность, серии или used IDs других разделов: тесты в Tasks 2–3.
- Строгий выбор текущего уровня и цикл без повторов не должен возвращаться к старому fallback `QuestionPicker.Next`: тесты в Task 1.
- Snapshot и файловое сохранение должны восстанавливать выбор, уровни, streaks и used IDs без сохранения боя, текущего вопроса и random-курсора: тесты в Tasks 3, 6 и ручной сценарий Task 12.
- После ходьбы квиз, hit areas, линия огня и остановка зомби должны использовать новую позицию: тесты layout в Tasks 4–5 и ручной сценарий Task 12.
- `prompt_image` не должен заменять или скрывать текст и TeX-формулу: модель, загрузчик, генератор и визуальная проверка в Task 7; существование всех SVG в Tasks 7–11.
- Математически эквивалентные варианты и вопросы-клоны могут пройти JSON-схему: нормализованные проверки и аудит контента в Tasks 8–11. Для заимствованных и адаптированных заданий обязательна ручная математическая проверка и оценка времени 5–10 минут.
- Узкое окно и длинная подпись «Математический анализ» не должны сдвигать иконки за экран или делать их неоднозначными для клика: тесты `RoomLayout` и визуальная проверка в Tasks 4, 12.

---

### Task 1: Каталог пакетов и случайный цикл

**Files:**
- Create: `core/MathSurvive.Core/Questions/QuestionCatalog.cs`
- Modify: `core/MathSurvive.Core/Questions/QuestionPicker.cs`
- Test: `tests/MathSurvive.Core.Tests/QuestionCatalogTests.cs`
- Test: `tests/MathSurvive.Core.Tests/QuestionPickerTests.cs`

**Interfaces:**
- Consumes: `QuestionPack`, `QuestionPicker`, `DifficultyTracker`, `GameConfig`, `Random`.
- Produces:
  - `QuestionCatalog(GameConfig config, IReadOnlyList<QuestionPack> packs, IReadOnlyList<string> randomOrder, Random rng)`
  - `string? SelectedSectionId { get; }` (`null` означает random)
  - `QuestionPack ActivePack { get; }`
  - `DifficultyTracker ActiveDifficulty { get; }`
  - `Question NextQuestion()`
  - `Question? FindActive(string id)`
  - `void SelectSection(string id)`
  - `void SelectRandom()`
  - `QuestionPicker.NextAtLevel(int level, ISet<string> used)` — строгий выбор текущего уровня для каталога.
- Preserve: существующий `QuestionPicker.Next` и его тесты для одиночного отладочного пакета.

- [ ] **Step 1: Написать failing-тесты строгого выбора и каталога**

В `QuestionPickerTests` проверить seeded `Random`: `NextAtLevel` выбирает только вопросы запрошенного уровня, не повторяет их до исчерпания, затем очищает только переданный набор этого уровня и начинает новый цикл; при отсутствующем уровне выбрасывается `InvalidOperationException` вместо fallback.

Создать четыре пакета с вопросами разных уровней и конфигурацию `GameConfig.Default with { LevelUpStreak = 4, LevelDownStreak = 3 }`, явно передать её в каждый вызов конструктора каталога и проверить:
- начальное состояние `SelectedSectionId is null`;
- четыре вызова `NextQuestion()` возвращают ID в порядке `a1, b1, c1, d1`, пятый снова `a1`; после каждого вызова `ActivePack.Section.Id` соответствует именно выданному вопросу;
- во всех разделах повышение происходит только после четырёх верных, а понижение — после трёх неверных, что подтверждает применение одного нестандартного config ко всем `DifficultyTracker`;
- после повышения или понижения уровня выбор идёт только на новом текущем уровне, а при исчерпании уровня не переключается на соседний;
- `SelectSection("geometry")` фиксирует активный пакет и восстанавливает его независимые used IDs;
- `SelectRandom()` после конкретного раздела начинает цикл с первого ID;
- `FindActive` ищет только в активном пакете;
- дубликаты ID пакетов, пустой `randomOrder`, повторы и отсутствующие разделы в `randomOrder` вызывают `ArgumentException`.

- [ ] **Step 2: Запустить тест и подтвердить красный результат**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter "QuestionCatalogTests|QuestionPickerTests" -nologo -v:minimal`
Expected: FAIL по отсутствующим `QuestionCatalog` и `NextAtLevel`.

- [ ] **Step 3: Реализовать строгий выбор и `QuestionCatalog`**

Добавить `NextAtLevel`, не меняя старый `Next`: случайно выбирать из fresh только заданного уровня; при пустом fresh очистить used только для вопросов этого уровня и повторить выбор. Конструктор каталога сохраняет полученный `GameConfig` и передаёт его без изменений в `DifficultyTracker` каждого раздела. Хранить для каждого раздела отдельные `QuestionPicker`, `DifficultyTracker` и `Dictionary<int, HashSet<string>>` показанных ID. `NextQuestion()` передаёт в `NextAtLevel` уровень `ActiveDifficulty` и набор именно этого уровня.

В random `NextQuestion()` выбирает состояние текущего элемента `randomOrder`, затем сдвигает курсор следующего выбора по модулю; `ActivePack` остаётся у только что выданного вопроса, чтобы ответ и статистика относились к нему. В конкретном разделе random-курсор не меняется. Конструктор сразу выбирает random и делает активным первый раздел до первого вопроса. Все вызовы в тестах, `GameSession` и `Main` передают один и тот же config явно.

- [ ] **Step 4: Прогнать новые и старые Core-тесты**

Run: `dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`
Expected: PASS, включая прежние тесты `QuestionPicker.Next`.

- [ ] **Step 5: Commit**

```cmd
git add core\MathSurvive.Core\Questions\QuestionCatalog.cs core\MathSurvive.Core\Questions\QuestionPicker.cs tests\MathSurvive.Core.Tests\QuestionCatalogTests.cs tests\MathSurvive.Core.Tests\QuestionPickerTests.cs docs\superpowers\plans\2026-10-30-quiz-sections-room.md
git commit -m "Core: каталог разделов и случайный цикл"
```

### Task 2: Смена раздела в `GameSession`

**Files:**
- Modify: `core/MathSurvive.Core/Session/GameSession.cs`
- Test: `tests/MathSurvive.Core.Tests/GameSessionTests.cs`

**Interfaces:**
- Consumes: все члены `QuestionCatalog` из Task 1.
- Produces:
  - `GameSession(GameConfig config, QuestionCatalog catalog, Random rng)`
  - `string? SelectedSectionId { get; }`
  - `bool CanChangeSection { get; }`
  - `bool SelectSection(string id)`
  - `bool SelectRandom()`
- Preserve: существующий `GameSession(GameConfig, QuestionPack, Random)`, `Pack`, `Difficulty`, `OpenQuiz(string? forcedQuestionId = null)`.

- [ ] **Step 1: Добавить failing-тесты переходов**

Проверить:
- `SelectSection` в `QuizOpen` очищает `CurrentQuiz`/`LastAnswer`, переводит в `Idle`, возвращает `true` и не меняет `Run`, `Combat`, `Pause` и игровой `Clock`;
- повторный выбор активного режима возвращает `false` и не сбрасывает random-цикл;
- в `Horde`, `Feedback`, `Paused`, `HordeSummary`, `GameOver` оба метода возвращают `false`;
- три верных ответа повышают уровень первого раздела, после перехода второй начинает с уровня 1, при возврате первый снова имеет уровень 2;
- в тестах с нестандартными порогами `GameSession` и `QuestionCatalog` получают один и тот же config, а все разделы применяют именно его;
- сворачивание и орда сохраняют текущий вопрос и не продвигают цикл; новый вопрос после ответа берёт следующий раздел;
- старый конструктор одиночного пакета работает без изменения ожиданий текущих тестов.

- [ ] **Step 2: Запустить тесты и подтвердить красный результат**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter GameSessionTests -nologo -v:minimal`
Expected: FAIL по отсутствующим конструктору и методам выбора.

- [ ] **Step 3: Перевести сессию на каталог**

Заменить поля `_picker`, `_used` и единственный `Difficulty` на `QuestionCatalog`; `Pack` и `Difficulty` делать делегирующими свойствами. Разрешить выбор только в `Idle`/`QuizOpen`; при реальной смене очистить текущий вопрос и время ответа, вызвать `SetMode(Idle)`. Не сбрасывать боевые ресурсы, паузу, статистику или `GameClock`. Старый конструктор создаёт `QuestionCatalog(config, ...)` с тем же `config`, который получает `GameSession`, и выбирает конкретный ID пакета; тестовые и игровые вызовы нового конструктора создают каталог с этим же config до передачи в сессию.

- [ ] **Step 4: Прогнать полный набор**

Run: `dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`
Expected: PASS.

- [ ] **Step 5: Commit**

```cmd
git add core\MathSurvive.Core\Session\GameSession.cs tests\MathSurvive.Core.Tests\GameSessionTests.cs
git commit -m "Core: переключение разделов без потери прогресса"
```

### Task 3: Snapshot и восстановление прогресса в Core

**Files:**
- Create: `core/MathSurvive.Core/Progress/QuizProgressSnapshot.cs`
- Create: `core/MathSurvive.Core/Progress/ProgressSaveData.cs`
- Create: `core/MathSurvive.Core/Progress/ProgressSaveCodec.cs`
- Modify: `core/MathSurvive.Core/DifficultyTracker.cs`
- Modify: `core/MathSurvive.Core/Questions/QuestionCatalog.cs`
- Test: `tests/MathSurvive.Core.Tests/DifficultyTrackerTests.cs`
- Test: `tests/MathSurvive.Core.Tests/QuestionCatalogTests.cs`
- Test: `tests/MathSurvive.Core.Tests/GameSessionTests.cs`
- Create: `tests/MathSurvive.Core.Tests/ProgressSaveCodecTests.cs`

**Interfaces:**
- Produces immutable Core snapshot:
  - `QuizProgressSnapshot(string? SelectedSectionId, IReadOnlyList<SectionProgressSnapshot> Sections)` (`null` означает random);
  - `SectionProgressSnapshot(string SectionId, int Level, int CorrectStreak, int WrongStreak, IReadOnlyDictionary<int, IReadOnlyList<string>> UsedQuestionIdsByLevel)`.
- Produces Core save DTO/codec без Godot: `ProgressSaveData`, `SectionProgressSaveData`, `ProgressSaveCodec.Serialize(...)`, `ProgressSaveCodec.Deserialize(string json, IReadOnlyList<QuestionPack> packs, GameConfig config) : QuizProgressSnapshot?`; пакеты задают допустимые разделы и границы уровней, config — допустимые streaks.
- Produces catalog: `QuizProgressSnapshot CaptureProgress()` и `void RestoreProgress(QuizProgressSnapshot? snapshot)`; `null` или недопустимый snapshot атомарно восстанавливает defaults без исключения.
- Produces tracker: `CorrectStreak`, `WrongStreak` только для чтения и `Restore(int level, int correctStreak, int wrongStreak)`; codec, каталог и tracker используют общую Core-валидацию с config.
- Preserve: `RecordAnswer`, `GameConfig.LevelUpStreak`, `LevelDownStreak`; текущий вопрос, random-курсор и боевое состояние в snapshot не входят.

- [ ] **Step 1: Расширить failing-тесты сложности**

В `DifficultyTrackerTests` проверить 3 верных подряд и 2 неверных подряд, сброс wrong после верного и correct после неверного, обнуление сработавшей серии и ограничение 1..MaxLevel. Отдельно на max проверить, что третий верный оставляет max и обнуляет correct streak; на min — что второй неверный оставляет min и обнуляет wrong streak. Использовать `GameConfig.Default` для 3/2 и конфигурацию с другими порогами через `with`, чтобы исключить дублирование констант. Проверить чтение и восстановление обеих серий.

- [ ] **Step 2: Подтвердить красный результат**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter DifficultyTrackerTests -nologo -v:minimal`
Expected: FAIL по отсутствующим свойствам серий и `Restore`.

- [ ] **Step 3: Реализовать восстановление `DifficultyTracker`**

Открыть `CorrectStreak`/`WrongStreak` только для чтения, не меняя алгоритм `RecordAnswer`: сброс противоположной серии, сброс сработавшей серии даже у min/max и ограничение уровня. `Restore` проверяет уровень 1..MaxLevel и серии в допустимых диапазонах от нуля до соответствующего порога `GameConfig` минус один; недопустимые значения отклоняются, пороги не копируются в DTO. Вынести эту проверку в общий Core-валидатор, который используют `DifficultyTracker.Restore`, `ProgressSaveCodec` и `QuestionCatalog.RestoreProgress`, чтобы правила не расходились.

- [ ] **Step 4: Добавить failing-тесты snapshot каталога**

В `QuestionCatalogTests` проверить capture/restore выбора конкретного раздела и random, разных уровней, correct/wrong streaks и used IDs по нескольким уровням. После restore следующий вопрос идёт с восстановленного уровня и не повторяет сохранённые ID до исчерпания уровня; прогресс остальных разделов остаётся независимым. Проверить defaults при `null`, отсутствии restore и недопустимом snapshot: random, уровни 1, нулевые серии, пустые used IDs, random-курсор на первом разделе, без исключения и без частично применённых данных. В `GameSessionTests` подтвердить, что смена раздела, сворачивание и орда не меняют snapshot сложности и used IDs.

Run: `dotnet test tests\MathSurvive.Core.Tests --filter "QuestionCatalogTests|GameSessionTests" -nologo -v:minimal`
Expected: FAIL по отсутствующим snapshot API.

- [ ] **Step 5: Реализовать snapshot, DTO и round-trip**

`CaptureProgress` возвращает неизменяемые копии данных всех разделов. `RestoreProgress` сначала проверяет весь snapshot по разделам, уровням и сериям через общий валидатор, затем копирует значения в независимые коллекции и вызывает `DifficultyTracker.Restore`; при `null` или любой ошибке валидации атомарно применяет defaults ко всему каталогу и не выбрасывает исключение. Текущий вопрос и random-курсор в snapshot не добавляются. Неизменяемый snapshot отделён от mutable DTO с snake_case JSON. `ProgressSaveCodec` сериализует только выбор, уровни, серии и used IDs по уровням; при повреждённом JSON, дубликатах разделов, неизвестных разделах или недопустимых значениях возвращает `null`, а не выбрасывает наружу ошибку загрузки.

В `ProgressSaveCodecTests` и `QuestionCatalogTests` проверить round-trip выбора random/раздела, уровней, streaks и used IDs, отсутствие боевых полей, текущего вопроса и random-курсора, повреждённый JSON и недопустимую структуру. Отдельно использовать `GameConfig.Default with { LevelUpStreak = 4, LevelDownStreak = 3 }`: убедиться, что все разделы применяют 4/3, streaks 3/2 проходят codec и восстанавливаются, следующие ответы вызывают повышение/понижение по 4/3, а значения вне этих порогов дают `null` из codec и defaults при `RestoreProgress` без исключения. Прогнать полный `dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`. Expected: PASS.

- [ ] **Step 6: Commit**

```cmd
git add core\MathSurvive.Core\DifficultyTracker.cs core\MathSurvive.Core\Questions\QuestionCatalog.cs core\MathSurvive.Core\Progress tests\MathSurvive.Core.Tests\DifficultyTrackerTests.cs tests\MathSurvive.Core.Tests\QuestionCatalogTests.cs tests\MathSurvive.Core.Tests\GameSessionTests.cs tests\MathSurvive.Core.Tests\ProgressSaveCodecTests.cs
git commit -m "Core: snapshot и восстановление прогресса"
```

### Task 4: Раскладка и визуальная комната

**Files:**
- Create: `core/MathSurvive.Core/Layout/RoomLayout.cs`
- Create: `scripts/RoomView.cs`
- Create: `scripts/RoomIconView.cs`
- Modify: `scenes/Main.tscn`
- Test: `tests/MathSurvive.Core.Tests/RoomLayoutTests.cs`

**Interfaces:**
- Produces Core:
  - `RoomLayout.IconRects(int windowW, int windowH, double scale) : IReadOnlyList<PixelRect>`
  - `RoomLayout.PlayerFeet(int windowW, int windowH, double scale, int iconIndex) : (double x, double y)`
- Produces Godot:
  - `RoomView.IconClicked : event Action<string>?` (ID `random`, `arithmetic`, `algebra`, `geometry`, `analysis`)
  - `RoomView.Icons : IReadOnlyList<RoomIconView>`
  - `RoomView.PlayerFeetPx(string id) : Vector2`
  - `RoomView.SetSelected(string id)` и `RoomView.SetInteractive(bool enabled)`
  - `RoomIconView.ScreenRect : Rect2` и `RoomIconView.HitArea() : Rect2?` (`null`, если иконка неактивна)
- Порядок пяти rect соответствует порядку ID выше.

- [ ] **Step 1: Написать failing-тесты `RoomLayout`**

Для 1920×1040 и scale 1 проверить пять непересекающихся rect, порядок слева направо, координаты внутри окна и совпадение `PlayerFeet(...).x` с центром соответствующей иконки. Повторить для scale 1.5 и окна 1024×700; проверить `ArgumentOutOfRangeException` для iconIndex −1 и 5.

- [ ] **Step 2: Запустить layout-тесты**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter RoomLayoutTests -nologo -v:minimal`
Expected: FAIL, тип отсутствует.

- [ ] **Step 3: Реализовать раскладку**

Разместить центры по 10/30/50/70/90% ширины окна, нижнюю стену и иконки задавать в логических px с учётом scale. Все размеры и hit rect возвращать в физических px; не использовать координаты Godot в Core.

- [ ] **Step 4: Построить комнату и иконки**

`RoomView` создаёт нижнюю стену и пол из простых материалов без текстурных зависимостей. `RoomIconView` использует `Label3D` для символа и подписи, показывает selected/disabled состояния и ловит левый клик только внутри `ScreenRect` через `DesktopOverlay.IsLeftClickIn`. В `Main.tscn` добавить `Room` до `Player`, чтобы персонаж оставался перед стеной. Точные размеры подобрать по макету, сохранив символы `?`, `+ −`, `x²`, `△`, `∫` и полные названия разделов.

- [ ] **Step 5: Собрать и прогнать тесты**

Run: `dotnet build MathSurvive.sln -nologo -v:minimal && dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`
Expected: сборка без ошибок, тесты PASS.

- [ ] **Step 6: Commit**

```cmd
git add core\MathSurvive.Core\Layout\RoomLayout.cs scripts\RoomView.cs scripts\RoomIconView.cs scenes\Main.tscn tests\MathSurvive.Core.Tests\RoomLayoutTests.cs
git commit -m "UI: комната с иконками разделов"
```

### Task 5: Ходьба и подключение комнаты к игре

**Files:**
- Modify: `scripts/PlayerView.cs`
- Modify: `scripts/Main.cs`
- Modify: `scripts/HordeDirector.cs`
- Modify: `scripts/StatsPanel.cs`
- Modify: `core/MathSurvive.Core/Layout/Placement.cs`
- Test: `tests/MathSurvive.Core.Tests/PlacementTests.cs`

**Interfaces:**
- Consumes: `QuestionCatalog`/выбор из Tasks 1–3, `RoomView` из Task 4.
- Produces:
  - `PlayerView.IsWalking : bool`
  - `PlayerView.FeetPx : Vector2`
  - `PlayerView.Arrived : event Action?`
  - `PlayerView.WalkTo(Stage stage, Vector2 feetPx)`
  - `Placement.FireLineX(double playerX, int windowW, double fireRangeFraction) : double`
- Main использует порядок `arithmetic, algebra, geometry, analysis` и словарь путей `packs/math/<id>`.

- [ ] **Step 1: Добавить failing-тест динамической линии огня**

В `PlacementTests` проверить, что новый `FireLineX(500, 1920, 1.0/3)` возвращает `1140` и не зависит от старой фиксированной позиции игрока; сохранить старый overload как делегирующий для совместимости.

- [ ] **Step 2: Подтвердить красный результат**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter PlacementTests -nologo -v:minimal`
Expected: FAIL по отсутствующему overload.

- [ ] **Step 3: Реализовать ходьбу существующей модели**

В `PlayerView` хранить модель, stage, текущие/целевые feet. `WalkTo` включает clip `walk` в loop, разворачивает модель по направлению и движется со скоростью 240 логических px/s с учётом `UiScale`; каждый кадр обновляет `FeetPx`, `ScreenRect`, `HeadTopPx`. По прибытии вызвать одноразовый `Arrived` и вернуть `holding-both`; `Place` отменяет движение. Существующий `PlayShot` и возврат после `holding-both-shoot` не ломать.

- [ ] **Step 4: Подключить четыре пакета и иконки в `Main`**

Загрузить четыре пакета через существующий `PackLoader.Parse`, создать `QuestionCatalog(config, packs, randomOrder, rng)` и `GameSession(config, catalog, rng)` с одним config, начать под `random`. Зарегистрировать каждую иконку в `DesktopOverlay` через `RegisterHitArea(() => icon.HitArea())`, чтобы неактивные иконки и стена пропускали клики. При допустимом клике вызвать `SelectRandom`/`SelectSection`, скрыть квиз, заблокировать повторные клики и запустить `WalkTo`; из `Arrived` открыть квиз. В `ApplyMode` учитывать `CanChangeSection && !IsWalking`. Обновлять позиции Bubble/Quiz/Toast/Stats при каждом перемещении и передавать в `QuizPanel.ShowQuiz` каталог активного пакета.

- [ ] **Step 5: Привязать орду и статистику к новым данным**

В начале орды пересчитывать stop/fire line и feet по `PlayerView.FeetPx`, чтобы зомби шли к актуальному персонажу. В `StatsPanel` заменить один `sectionTitle` на `IReadOnlyDictionary<string,string> sectionTitles` и выводить русские названия для каждого ID; поведение одиночного пакета сохранить.

- [ ] **Step 6: Собрать, протестировать и проверить первый кадр**

Run: `dotnet build MathSurvive.sln -nologo -v:minimal && dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`
Expected: PASS.

Run: `start.cmd -- --quit-after=3 --shot=room.png`
Expected: игра запускается без отсутствующих ресурсов; на снимке видны стена, пять иконок и персонаж под `?`. Удалить `room.png` после просмотра.

- [ ] **Step 7: Commit**

```cmd
git add scripts\PlayerView.cs scripts\Main.cs scripts\HordeDirector.cs scripts\StatsPanel.cs core\MathSurvive.Core\Layout\Placement.cs tests\MathSurvive.Core.Tests\PlacementTests.cs
git commit -m "Игра: выбор раздела с анимацией ходьбы"
```

### Task 6: Атомарный `save.json` и подключение к игре

**Files:**
- Create: `core/MathSurvive.Core/Progress/ProgressFileStore.cs`
- Create: `scripts/ProgressSaveStore.cs`
- Modify: `scripts/Main.cs`
- Create: `tests/MathSurvive.Core.Tests/ProgressFileStoreTests.cs`

**Interfaces:**
- Consumes: snapshot/DTO/codec и восстановление из Task 3, подключённые пакеты/комната/`Main` из Task 5.
- Produces Core: `ProgressFileStore(string path)`, `QuizProgressSnapshot? Load(IReadOnlyList<QuestionPack> packs, GameConfig config)` и `void Save(QuizProgressSnapshot snapshot)` на `System.IO`, без Godot.
- Produces Godot adapter: `ProgressSaveStore` по образцу `PacksRoot` выбирает `ProjectSettings.GlobalizePath("user://save.json")` в редакторе и `OS.GetExecutablePath().GetBaseDir() + "/save.json"` в собранной игре, затем делегирует чтение/запись `ProgressFileStore` с тем же config.
- Preserve: в сохранении нет боя, текущего вопроса и random-курсора; выбор random представляется `selected_section_id: null`.

- [ ] **Step 1: Написать failing-тесты файлового сохранения**

В `ProgressFileStoreTests` использовать отдельный временный каталог и проверить: отсутствующий файл возвращает `null` без исключения; повреждённый JSON возвращает `null`; round-trip через `Save`/`Load` с одним config восстанавливает выбор, уровни, streaks и used IDs; `Save` создаёт временный файл в том же каталоге и атомарно заменяет существующий `save.json`, после успеха временный файл отсутствует. Добавить round-trip и отклонение невалидных streaks с нестандартными порогами config. Удалять тестовый каталог после каждого теста.

Run: `dotnet test tests\MathSurvive.Core.Tests --filter ProgressFileStoreTests -nologo -v:minimal`
Expected: FAIL по отсутствующему `ProgressFileStore`.

- [ ] **Step 2: Реализовать безопасное чтение и атомарную запись**

`Load(packs, config)` возвращает `null` при отсутствии файла и делегирует повреждённый JSON/структуру и проверку серий `ProgressSaveCodec` с теми же пакетами и config, не позволяя ошибке чтения или разбора ронять игру. `Save` сериализует snapshot, пишет `save.json.tmp` рядом с целевым файлом, сбрасывает содержимое на диск и атомарно заменяет целевой файл средствами `System.IO`; при первой записи создаёт `save.json`. Временный файл всегда остаётся в каталоге, выбранном адаптером для данного режима запуска. Не хранить второй файл прогресса и не добавлять боевые данные.

- [ ] **Step 3: Подключить восстановление и сохранение в `Main`**

При запуске вычислить config, загрузить четыре пакета, создать `QuestionCatalog(config, packs, randomOrder, rng)` с defaults и вызвать `RestoreProgress(ProgressSaveStore.Load(packs, config))`: действительный snapshot восстанавливается, а `null` оставляет defaults без исключения. В `GameSession` передать тот же config и каталог. До первого кадра применить восстановленный выбор к `RoomView` и поставить персонажа под соответствующей иконкой без фиксации random при каждом запуске.

Сохранять snapshot: в `OnOptionChosen` только если `GameSession.Answer` вернул результат; после `SelectSection`/`SelectRandom` только при возвращённом `true`; в `Main._ExitTree` при любом штатном выходе, включая дверь, закрытие окна и `DebugCapture`. Не сохранять повторный клик по активному режиму как новую смену. Ошибки записи логировать через Godot, не прерывая игровой цикл.

- [ ] **Step 4: Прогнать тесты и ручной сценарий перезапуска**

Run: `dotnet build MathSurvive.sln -nologo -v:minimal && dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`
Expected: PASS.

Через `start.cmd` ответить на вопрос, закрыть игру, проверить наличие `save.json` и отсутствие временного файла, затем снова запустить: восстанавливаются выбранная иконка, уровни, серии и used IDs; текущий вопрос заново не открывается. Повысить уровень одного раздела, переключиться на другой с уровнем 1 и вернуться: каждый раздел сохраняет собственные уровень, серии и историю. Отдельно применить смену раздела, закрыть игру **без ответа** и снова запустить: новый выбор и его независимый уровень восстановлены. Повторить перезапуск при отсутствующем и повреждённом `save.json`: игра стартует с defaults без падения. Сохранить исходный пользовательский файл перед ручными экспериментами и восстановить после них.

- [ ] **Step 5: Commit**

```cmd
git add core\MathSurvive.Core\Progress\ProgressFileStore.cs scripts\ProgressSaveStore.cs scripts\Main.cs tests\MathSurvive.Core.Tests\ProgressFileStoreTests.cs
git commit -m "Игра: сохранение прогресса между запусками"
```

### Task 7: Рисунки условия в формате пакета и интерфейсе

**Files:**
- Modify: `core/MathSurvive.Core/Questions/Question.cs`
- Modify: `core/MathSurvive.Core/Questions/PackLoader.cs`
- Modify: `scripts/QuizPanel.cs`
- Modify: `tools/tex2svg/build-pack.mjs`
- Modify: `tests/MathSurvive.Core.Tests/PackLoaderTests.cs`
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`
- Modify: `packs-src/math/test/questions.src.json`
- Create: `packs-src/math/test/images/test-L3-03.svg`
- Modify: `packs/math/test/questions.json` и `packs/math/test/svg/*.svg` через генератор
- Create: `packs/math/test/images/test-L3-03.svg` через генератор

**Interfaces:**
- Produces Core: `Question.PromptImage : string?`, соответствующее JSON-полю `prompt_image`.
- Preserve: `Prompt`, `PromptSvg`, варианты и `PackLoader.Parse(string sectionJson, string questionsJson)`.
- Produces source format: `prompt_image: "images/<question-id>.svg"`; путь относителен к корню пакета и в исходнике, и в производном JSON.
- Produces UI: `QuizPanel` отображает `Prompt`, `PromptSvg` и `PromptImage` вместе, если одновременно заданы все три.

- [ ] **Step 1: Написать failing-тесты модели и загрузчика**

В `PackLoaderTests` проверить: вопрос только с `prompt_image`; сочетание `prompt`, `prompt_svg` и `prompt_image` без потери любого поля; отсутствие всех трёх частей условия по-прежнему вызывает `PackFormatException`. Для `prompt_image` проверить отклонение абсолютного пути, `..`, пути вне `images/`, пустой строки и расширения не `.svg`; валидный путь `images/geometry-L1-001.svg` сохраняется без нормализации в другое поле.

- [ ] **Step 2: Запустить тесты и подтвердить красный результат**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter PackLoaderTests -nologo -v:minimal`
Expected: FAIL по отсутствующему `PromptImage` и новым проверкам.

- [ ] **Step 3: Расширить модель и `PackLoader`**

Добавить в `Question` последний необязательный параметр `string? PromptImage = null` с учётом `SnakeCaseLower`, чтобы существующие вызовы конструктора не требовали изменений. Условие считать заданным, если присутствует хотя бы одно из `Prompt`, `PromptSvg`, `PromptImage`; разрешить любое их сочетание. Добавить в `PackLoader` проверку относительного SVG-пути `prompt_image` без файлового ввода-вывода: `/` как разделитель, префикс `images/`, отсутствие абсолютного пути и сегментов `..`. Оставить Core независимым от Godot.

- [ ] **Step 4: Научить генератор копировать рисунки**

В `build-pack.mjs` оставить преобразование `prompt_tex` → `prompt_svg` и `tex` → `svg` без изменений. Для `prompt_image` проверить тот же относительный путь и существование исходного файла относительно `path.dirname(srcPath)`, создать `<outDir>/images`, удалить устаревшие `.svg` в нём и скопировать файл в тот же относительный путь; в `questions.json` сохранить `prompt_image` без обработки MathJax. Не удалять исходные изображения.

В `test-L3-03` добавить несложную схему встречного движения `images/test-L3-03.svg` и `prompt_tex` с обозначением расстояния, чтобы вопрос одновременно содержал текст, формулу и рисунок. Сохранить девять вопросов `math/test` и прежний правильный ответ.

Run: `cd tools\tex2svg && npm ci && node build-pack.mjs ..\..\packs-src\math\test\questions.src.json ..\..\packs\math\test`

- [ ] **Step 5: Добавить рисунок в `QuizPanel`**

Создать отдельный `TextureRect` после формулы и перед вариантами. Загружать `PromptImage` через `SvgTextures.LoadUi(sectionDir + "/" + question.PromptImage, ...)`, показывать узел только при наличии пути и очищать текстуру при следующем вопросе. Ограничить рисунок областью не шире `PromptMaxWidth` и не выше 320 логических px, сохраняя пропорции и видимость всех подписей; не применять к нему обрезку. Учесть `CustomMinimumSize` в существующих `MinimumSizeChanged`, `ScrollContainer` и `Relayout`, чтобы текст, формула, рисунок и варианты не перекрывались.

- [ ] **Step 6: Проверить файлы, тесты и реальный кадр**

В `BuiltPackTests` выделить проверку всех SVG ссылок: `PromptSvg`, `PromptImage` и каждый `Option.Svg` разрешать через `Path.Combine(packDir, relativePath)` и требовать `File.Exists`; добавить отдельную проверку несуществующего `prompt_image` и сохранить ожидание девяти вопросов `math/test`.

Run: `dotnet build MathSurvive.sln -nologo -v:minimal && dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`
Expected: PASS; все скопированные и сгенерированные SVG существуют.

Run: `start.cmd -- --open-quiz --question=test-L3-03 --quit-after=3 --shot=quiz-image.png`
Expected: текст, формула и схема читаемы одновременно, рисунок не обрезан, варианты доступны, облачко остаётся в окне; удалить `quiz-image.png` после просмотра.

- [ ] **Step 7: Commit**

```cmd
git add core\MathSurvive.Core\Questions\Question.cs core\MathSurvive.Core\Questions\PackLoader.cs scripts\QuizPanel.cs tools\tex2svg\build-pack.mjs tests\MathSurvive.Core.Tests\PackLoaderTests.cs tests\MathSurvive.Core.Tests\BuiltPackTests.cs packs-src\math\test packs\math\test
git commit -m "Вопросы: SVG-рисунки в условиях"
```

### Task 8: Пакет «Арифметика»

**Files:**
- Create: `packs-src/math/arithmetic/questions.src.json`
- Create: `packs-src/math/arithmetic/images/*.svg` при наличии рисунков
- Create: `packs/math/arithmetic/section.json`
- Create: `packs/math/arithmetic/questions.json`, `svg/*.svg` и `images/*.svg` через генератор
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`
- Modify: `scripts/SvgGallery.cs`

**Interfaces:**
- Consumes: генератор и `prompt_image` из Task 7.
- Produces: пакет `arithmetic`, 100 вопросов, уровни 40/35/25.

- [ ] **Step 1: Обобщить pack-тест и добавить красный `InlineData`**

Создать `AssertPack(string id, int count, int l1, int l2, int l3)`; проверять схему, количество, распределение и уникальные ID внутри пакета. По исходному `questions.src.json` нормализовать whitespace в `text`/`tex` и требовать четыре разных варианта; в производном пакете проверять существование SVG формул, вариантов и `prompt_image`. Сохранить отдельный тест девяти вопросов `math/test`; добавить вызов для `arithmetic, 100, 40, 35, 25`.

- [ ] **Step 2: Подтвердить отсутствие пакета красным тестом**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter BuiltPackTests -nologo -v:minimal`
Expected: FAIL для `arithmetic`.

- [ ] **Step 3: Составить и сгенерировать 100 вопросов**

Использовать ID `arithmetic-L<level>-NNN`, темы и ограничения сложности из Spec. Допускаются оригинальные, заимствованные и адаптированные общедоступные задания, включая ЕГЭ; сохранять только итоговое содержимое и нужные SVG, без сведений об источнике. Распределить вопросы по всем перечисленным темам, не создавать числовые клоны одного шаблона. Создать `section.json` с `{"id":"arithmetic","title":"Арифметика","levels":3}`. Обобщить `SvgGallery`: перечислять `svg/` и `images/` всех существующих разделов `packs/math` и группировать строки по разделу и вопросу, сохранив работу с `math/test`.

Run: `cd tools\tex2svg && npm ci && node build-pack.mjs ..\..\packs-src\math\arithmetic\questions.src.json ..\..\packs\math\arithmetic`

- [ ] **Step 4: Проверить пакет и аудит ответов**

Run из корня: `dotnet test tests\MathSurvive.Core.Tests --filter BuiltPackTests -nologo -v:minimal`
Expected: PASS. Проверить правильность `correct` для всех 100 вопросов и просмотреть формулы и рисунки в обобщённой `SvgGallery.tscn`; исправить эквивалентные варианты и неоднозначные условия до commit. Каждое заимствованное или адаптированное задание вручную решить и подтвердить однозначность и время не более 5–10 минут.

- [ ] **Step 5: Commit**

```cmd
git add packs-src\math\arithmetic packs\math\arithmetic tests\MathSurvive.Core.Tests\BuiltPackTests.cs scripts\SvgGallery.cs
git commit -m "Вопросы: 100 задач по арифметике"
```

### Task 9: Пакет «Алгебра»

**Files:**
- Create: `packs-src/math/algebra/questions.src.json` и `images/*.svg` при наличии рисунков
- Create: `packs/math/algebra/section.json`, `questions.json`, `svg/*.svg` и `images/*.svg` через генератор
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`

**Interfaces:**
- Consumes: `AssertPack` из Task 8.
- Produces: пакет `algebra`, 100 вопросов, уровни 40/35/25.

- [ ] **Step 1: Добавить красный вызов `AssertPack("algebra", 100, 40, 35, 25)` и запустить `BuiltPackTests`**

Expected: FAIL только из-за отсутствующего нового пакета.

- [ ] **Step 2: Составить 100 вопросов по алгебре**

ID `algebra-L<level>-NNN`; охватить все темы Spec, включая несложные текстовые задачи и графики. Использовать оригинальные, заимствованные или адаптированные общедоступные задания без метаданных источника. Для условий, где график необходим или улучшает наглядность, добавить `prompt_image` и читаемый SVG с подписями осей и нужными значениями. Проверить отсутствие клонов и эквивалентных вариантов.

- [ ] **Step 3: Сгенерировать SVG и прогнать тесты**

Run: `node tools\tex2svg\build-pack.mjs packs-src\math\algebra\questions.src.json packs\math\algebra` после `npm ci` в `tools\tex2svg`; затем `dotnet test tests\MathSurvive.Core.Tests --filter BuiltPackTests`.
Expected: PASS; вручную проверить все `correct`, рендеринг формул и читаемость рисунков. Заимствованные и адаптированные задания вручную решить и проверить ограничение 5–10 минут.

- [ ] **Step 4: Commit**

```cmd
git add packs-src\math\algebra packs\math\algebra tests\MathSurvive.Core.Tests\BuiltPackTests.cs
git commit -m "Вопросы: 100 задач по алгебре"
```

### Task 10: Пакет «Геометрия»

**Files:**
- Create: `packs-src/math/geometry/questions.src.json` и `images/*.svg` для задач с рисунками
- Create: `packs/math/geometry/section.json`, `questions.json`, `svg/*.svg` и `images/*.svg` через генератор
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`

**Interfaces:**
- Consumes: `AssertPack` из Task 8.
- Produces: пакет `geometry`, 100 вопросов, уровни 40/35/25.

- [ ] **Step 1: Добавить красный вызов `AssertPack("geometry", 100, 40, 35, 25)` и запустить `BuiltPackTests`**

Expected: FAIL только из-за отсутствующего нового пакета.

- [ ] **Step 2: Составить 100 вопросов по геометрии**

ID `geometry-L<level>-NNN`; допускаются задачи по тексту и по рисунку из оригинальных или общедоступных материалов, включая адаптированные задания ЕГЭ. Все числовые данные, обозначения и единицы измерения должны быть в тексте или на рисунке. Подписи вершин, точек, углов и величин обязаны однозначно соответствовать условию; рисунок не заменяет отсутствующие данные. Не использовать неоднозначные «найдите остальные элементы», доказательства без одного ясного ответа или числовые клоны.

- [ ] **Step 3: Сгенерировать, протестировать и проверить математику/рендеринг**

Run генератор для `geometry`, затем `BuiltPackTests`. Expected: PASS; вручную проверить все `correct`, единицы измерения, соответствие каждого рисунка условию, отсутствие перекрытий и читаемость подписей в реальном размере квиза. Все заимствованные и адаптированные задания вручную решить и проверить ограничение 5–10 минут.

- [ ] **Step 4: Commit**

```cmd
git add packs-src\math\geometry packs\math\geometry tests\MathSurvive.Core.Tests\BuiltPackTests.cs
git commit -m "Вопросы: 100 задач по геометрии"
```

### Task 11: Пакет «Математический анализ»

**Files:**
- Create: `packs-src/math/analysis/questions.src.json` и `images/*.svg` при наличии рисунков
- Create: `packs/math/analysis/section.json`, `questions.json`, `svg/*.svg` и `images/*.svg` через генератор
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`

**Interfaces:**
- Consumes: `AssertPack` из Task 8.
- Produces: пакет `analysis`, 100 вопросов, уровни 40/35/25.

- [ ] **Step 1: Добавить красный вызов `AssertPack("analysis", 100, 40, 35, 25)` и запустить `BuiltPackTests`**

Expected: FAIL только из-за отсутствующего нового пакета.

- [ ] **Step 2: Составить 100 вопросов по математическому анализу**

ID `analysis-L<level>-NNN`; охватить пределы, производные, касательные, экстремумы и интегралы из Spec. Использовать оригинальные, заимствованные или адаптированные общедоступные задания без метаданных источника; для графиков функций и площадей допускаются читаемые `prompt_image`. Не выходить за несложные элементарные функции и аккуратные вычисления, не создавать числовые клоны.

- [ ] **Step 3: Сгенерировать, протестировать и проверить математику/рендеринг**

Run генератор для `analysis`. Добавить в `BuiltPackTests` тест `All_pack_question_ids_are_unique_across_sections`, который загружает `test` и четыре новых пакета и сравнивает общее число ID с размером `HashSet`. Затем запустить полный `dotnet test tests\MathSurvive.Core.Tests`. Expected: PASS; вручную проверить все `correct`, области определения, константу интегрирования, формулы и рисунки. Заимствованные и адаптированные задания вручную решить и проверить ограничение 5–10 минут.

- [ ] **Step 4: Commit**

```cmd
git add packs-src\math\analysis packs\math\analysis tests\MathSurvive.Core.Tests\BuiltPackTests.cs
git commit -m "Вопросы: 100 задач по математическому анализу"
```

### Task 12: Документация и end-to-end проверка

**Files:**
- Modify: `AGENTS.md`
- Modify: `docs/superpowers/plans/2026-10-30-quiz-sections-room.md` (отметки выполненных шагов)

**Interfaces:**
- Consumes: весь функционал Tasks 1–11.

- [ ] **Step 1: Обновить проектные инструкции**

В `AGENTS.md` заменить описание одного текущего пакета на четыре раздела и random-цикл, добавить комнату/выбор в архитектуру и команды генерации для любого `<section>`. Описать строгий выбор текущего уровня, независимые streaks/used IDs и `save.json`: состав snapshot, передачу одного config в каталог/кодек, путь `user://save.json` в редакторе и `save.json` рядом с exe в собранной игре, defaults, сохранение после ответа/применённого выбора/выхода и атомарную замену в том же каталоге без боевого состояния. Описать совместимые `prompt`, `prompt_tex`, `prompt_image`, относительные пути `images/*.svg`, копирование рисунков генератором и проверку существования всех SVG. Не добавлять метаданные или реестр источников и не изменять `packs/SOURCES.md`.

- [ ] **Step 2: Прогнать полный build и экспорт**

Run: `build.cmd`
Expected: solution собирается, все тесты PASS, `build\MathSurvive.exe` создан, четыре пакета со всеми `svg/` и `images/` скопированы в `build\packs\math`; пользовательские файлы в `build` не удалены.

- [ ] **Step 3: Проверить реальные сценарии**

Через `start.cmd` вручную проверить:
- клик по каждой из пяти иконок вызывает `walk`, повторные клики во время движения игнорируются, квиз открывается только по прибытии и соответствует выбранному разделу;
- random даёт новые вопросы строго в порядке четырёх разделов, сворачивание и орда не сдвигают порядок и не меняют сложность/серии;
- обычный выбор остаётся на текущем уровне и не повторяет вопросы до исчерпания уровня;
- смена неотвеченного вопроса не добавляет ответ в статистику; после game over видны русские названия разделов;
- после ответа, закрытия и повторного запуска восстанавливаются выбор, уровень, серии и used IDs, но текущий вопрос не восстанавливается; при переключении между разделами с разными уровнями каждый сохраняет своё состояние;
- после применённой смены раздела и закрытия **без ответа** повторный запуск восстанавливает новый выбор и его независимый уровень;
- отсутствующий и повреждённый `save.json` не роняют игру и дают defaults; после успешного сохранения временного файла нет;
- Bubble/Quiz остаются над персонажем, клики мимо иконок проходят насквозь, зомби останавливаются у новой позиции;
- вопрос `test-L3-03` одновременно показывает текст, формулу и рисунок; вопросы геометрии с `prompt_image` имеют читаемые подписи, не обрезаются и при необходимости прокручиваются;
- при системном масштабе 150% иконки, длинная подпись и рисунки остаются доступными; раскладка 1024×700 дополнительно проверена unit-тестом в Task 4.

- [ ] **Step 4: Проверить итоговый diff и отсутствие мусора**

Run: `git status --short`
Expected: только запланированные изменения; нет PNG-снимков, логов, `node_modules` или файлов экспорта. Не включать unrelated-файлы, перечисленные в Global Constraints, если они не менялись задачами.

- [ ] **Step 5: Commit**

```cmd
git add AGENTS.md docs\superpowers\plans\2026-10-30-quiz-sections-room.md
git commit -m "Документация: разделы и комната выбора"
```
