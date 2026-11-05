# Quiz Sections and Room Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development (recommended) or executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Добавить комнату с пятью иконками выбора, анимированный переход персонажа и 400 вопросов в четырёх разделах математики.

**Architecture:** `QuestionCatalog` хранит пакеты и независимый прогресс разделов, а `GameSession` управляет разрешёнными переходами и текущим вопросом. Godot-слой строит комнату, перемещает существующую модель с clip `walk` и только сообщает Core о выборе; контент остаётся статическими JSON/SVG-пакетами.

**Tech Stack:** Godot 4.7.2 .NET, .NET 8, C# 12, xUnit, Node.js, MathJax 3, JSON/SVG.

**Spec:** `docs/superpowers/specs/2026-10-30-quiz-sections-room-design.md`

## Global Constraints

- Core не зависит от Godot; все правила выбора вопросов и смены режимов проверяются через `dotnet test`.
- Ровно четыре новых раздела: `arithmetic`, `algebra`, `geometry`, `analysis`; случайный режим не имеет собственного пакета.
- По 100 вопросов на раздел: 40 уровня 1, 35 уровня 2, 25 уровня 3; четыре варианта, zero-based `correct`.
- Порядок случайного режима: `arithmetic → algebra → geometry → analysis → arithmetic`.
- У иконок символы `?`, `+ −`, `x²`, `△`, `∫`; персонаж использует существующий clip `walk`.
- Не добавлять внешние банки вопросов и новые runtime-зависимости; не менять боевой баланс и формат формул.
- Сохранить существующий `math/test` и проходящие тесты одиночного пакета.
- Не включать в коммиты текущие unrelated-изменения из рабочего дерева, кроме файлов `PlayerView.cs`, `HordeDirector.cs`, `ModelVisual.cs`, если задача действительно меняет их поверх существующей работы.

## Review Focus

- Сворачивание, орда и повторный клик не должны продвигать случайный цикл: тесты в Tasks 1–2.
- Смена неотвеченного вопроса не должна менять статистику/бой и не должна сбрасывать сложность других разделов: тесты в Task 2.
- После ходьбы квиз, hit areas, линия огня и остановка зомби должны использовать новую позицию: тесты layout в Tasks 3–4 и ручной сценарий Task 9.
- Математически эквивалентные варианты и вопросы-клоны могут пройти JSON-схему: нормализованные проверки и аудит контента в Tasks 5–8.
- Узкое окно и длинная подпись «Математический анализ» не должны сдвигать иконки за экран или делать их неоднозначными для клика: тесты `RoomLayout` и визуальная проверка в Tasks 3, 9.

---

### Task 1: Каталог пакетов и случайный цикл

**Files:**
- Create: `core/MathSurvive.Core/Questions/QuestionCatalog.cs`
- Test: `tests/MathSurvive.Core.Tests/QuestionCatalogTests.cs`

**Interfaces:**
- Consumes: `QuestionPack`, `QuestionPicker`, `DifficultyTracker`, `GameConfig`, `Random`.
- Produces:
  - `QuestionCatalog(IReadOnlyList<QuestionPack> packs, IReadOnlyList<string> randomOrder, Random rng)`
  - `string? SelectedSectionId { get; }` (`null` означает random)
  - `QuestionPack ActivePack { get; }`
  - `DifficultyTracker ActiveDifficulty { get; }`
  - `Question NextQuestion()`
  - `Question? FindActive(string id)`
  - `void SelectSection(string id)`
  - `void SelectRandom()`

- [ ] **Step 1: Написать failing-тесты каталога**

Создать четыре пакета с одним отличающимся вопросом каждый и проверить:
- начальное состояние `SelectedSectionId is null`;
- четыре вызова `NextQuestion()` возвращают ID в порядке `a1, b1, c1, d1`, пятый снова `a1`; после каждого вызова `ActivePack.Section.Id` соответствует именно выданному вопросу;
- `SelectSection("geometry")` фиксирует активный пакет и вопросы только этого пакета;
- `SelectRandom()` после конкретного раздела начинает цикл с первого ID;
- `FindActive` ищет только в активном пакете;
- дубликаты ID пакетов, пустой `randomOrder`, повторы и отсутствующие разделы в `randomOrder` вызывают `ArgumentException`.

- [ ] **Step 2: Запустить тест и подтвердить красный результат**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter QuestionCatalogTests -nologo -v:minimal`
Expected: FAIL, тип `QuestionCatalog` отсутствует.

- [ ] **Step 3: Реализовать `QuestionCatalog`**

Хранить для каждого раздела отдельные `QuestionPicker`, `DifficultyTracker` и `HashSet<string>` показанных ID. В random `NextQuestion()` выбирает состояние текущего элемента `randomOrder`, затем сдвигает курсор следующего выбора по модулю; `ActivePack` при этом остаётся у только что выданного вопроса, чтобы ответ и статистика относились к нему. В конкретном разделе random-курсор не меняется. Конструктор сразу выбирает random и делает активным первый раздел до первого вопроса.

- [ ] **Step 4: Прогнать новые и старые Core-тесты**

Run: `dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`
Expected: PASS, все прежние тесты остаются зелёными.

- [ ] **Step 5: Commit**

```cmd
git add core\MathSurvive.Core\Questions\QuestionCatalog.cs tests\MathSurvive.Core.Tests\QuestionCatalogTests.cs docs\superpowers\plans\2026-10-30-quiz-sections-room.md
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
- сворачивание и орда сохраняют текущий вопрос и не продвигают цикл; новый вопрос после ответа берёт следующий раздел;
- старый конструктор одиночного пакета работает без изменения ожиданий текущих тестов.

- [ ] **Step 2: Запустить тесты и подтвердить красный результат**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter GameSessionTests -nologo -v:minimal`
Expected: FAIL по отсутствующим конструктору и методам выбора.

- [ ] **Step 3: Перевести сессию на каталог**

Заменить поля `_picker`, `_used` и единственный `Difficulty` на `QuestionCatalog`; `Pack` и `Difficulty` делать делегирующими свойствами. Разрешить выбор только в `Idle`/`QuizOpen`; при реальной смене очистить текущий вопрос и время ответа, вызвать `SetMode(Idle)`. Не сбрасывать боевые ресурсы, паузу, статистику или `GameClock`. Старый конструктор оборачивает один пакет в каталог и выбирает его конкретный ID.

- [ ] **Step 4: Прогнать полный набор**

Run: `dotnet test tests\MathSurvive.Core.Tests -nologo -v:minimal`
Expected: PASS.

- [ ] **Step 5: Commit**

```cmd
git add core\MathSurvive.Core\Session\GameSession.cs tests\MathSurvive.Core.Tests\GameSessionTests.cs
git commit -m "Core: переключение разделов без потери прогресса"
```

### Task 3: Раскладка и визуальная комната

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

### Task 4: Ходьба и подключение комнаты к игре

**Files:**
- Modify: `scripts/PlayerView.cs`
- Modify: `scripts/Main.cs`
- Modify: `scripts/HordeDirector.cs`
- Modify: `scripts/StatsPanel.cs`
- Modify: `core/MathSurvive.Core/Layout/Placement.cs`
- Test: `tests/MathSurvive.Core.Tests/PlacementTests.cs`

**Interfaces:**
- Consumes: `QuestionCatalog`/выбор из Tasks 1–2, `RoomView` из Task 3.
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

Загрузить четыре пакета через существующий `PackLoader.Parse`, создать каталог и сессию, начать под `random`. Зарегистрировать каждую иконку в `DesktopOverlay` через `RegisterHitArea(() => icon.HitArea())`, чтобы неактивные иконки и стена пропускали клики. При допустимом клике вызвать `SelectRandom`/`SelectSection`, скрыть квиз, заблокировать повторные клики и запустить `WalkTo`; из `Arrived` открыть квиз. В `ApplyMode` учитывать `CanChangeSection && !IsWalking`. Обновлять позиции Bubble/Quiz/Toast/Stats при каждом перемещении и передавать в `QuizPanel.ShowQuiz` каталог активного пакета.

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

### Task 5: Пакет «Арифметика»

**Files:**
- Create: `packs-src/math/arithmetic/questions.src.json`
- Create: `packs/math/arithmetic/section.json`
- Create: `packs/math/arithmetic/questions.json` и `packs/math/arithmetic/svg/*.svg` через генератор
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`
- Modify: `scripts/SvgGallery.cs`

**Interfaces:**
- Consumes: `node build-pack.mjs <src.json> <outDir>` без изменения генератора.
- Produces: пакет `arithmetic`, 100 вопросов, уровни 40/35/25.

- [ ] **Step 1: Обобщить pack-тест и добавить красный `InlineData`**

Создать `AssertPack(string id, int count, int l1, int l2, int l3)`; проверять схему, количество, распределение и уникальные ID внутри пакета. По исходному `questions.src.json` нормализовать whitespace в `text`/`tex` и требовать четыре разных варианта; в производном пакете проверять существование SVG. Сохранить отдельный тест девяти вопросов `math/test`; добавить вызов для `arithmetic, 100, 40, 35, 25`.

- [ ] **Step 2: Подтвердить отсутствие пакета красным тестом**

Run: `dotnet test tests\MathSurvive.Core.Tests --filter BuiltPackTests -nologo -v:minimal`
Expected: FAIL для `arithmetic`.

- [ ] **Step 3: Составить и сгенерировать 100 вопросов**

Использовать ID `arithmetic-L<level>-NNN`, темы и ограничения сложности из Spec. Распределить вопросы по всем перечисленным темам, не создавать числовые клоны одного шаблона. Создать `section.json` с `{"id":"arithmetic","title":"Арифметика","levels":3}`. Обобщить `SvgGallery`: перечислять все существующие разделы `packs/math` и группировать строки по разделу и вопросу, сохранив работу с `math/test`.

Run: `cd tools\tex2svg && npm ci && node build-pack.mjs ..\..\packs-src\math\arithmetic\questions.src.json ..\..\packs\math\arithmetic`

- [ ] **Step 4: Проверить пакет и аудит ответов**

Run из корня: `dotnet test tests\MathSurvive.Core.Tests --filter BuiltPackTests -nologo -v:minimal`
Expected: PASS. Дополнительно проверить правильность `correct` для всех 100 вопросов и просмотреть SVG всех формул в обобщённой `SvgGallery.tscn`; исправить эквивалентные варианты и неоднозначные условия до commit.

- [ ] **Step 5: Commit**

```cmd
git add packs-src\math\arithmetic packs\math\arithmetic tests\MathSurvive.Core.Tests\BuiltPackTests.cs scripts\SvgGallery.cs
git commit -m "Вопросы: 100 задач по арифметике"
```

### Task 6: Пакет «Алгебра»

**Files:**
- Create: `packs-src/math/algebra/questions.src.json`
- Create: `packs/math/algebra/section.json`, `questions.json`, `svg/*.svg`
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`

**Interfaces:**
- Consumes: `AssertPack` из Task 5.
- Produces: пакет `algebra`, 100 вопросов, уровни 40/35/25.

- [ ] **Step 1: Добавить красный вызов `AssertPack("algebra", 100, 40, 35, 25)` и запустить `BuiltPackTests`**

Expected: FAIL только из-за отсутствующего нового пакета.

- [ ] **Step 2: Составить 100 вопросов по алгебре**

ID `algebra-L<level>-NNN`; охватить все темы Spec, включая несложные текстовые задачи и графики по свойствам без обязательных изображений. Проверить отсутствие клонов и эквивалентных вариантов.

- [ ] **Step 3: Сгенерировать SVG и прогнать тесты**

Run: `node tools\tex2svg\build-pack.mjs packs-src\math\algebra\questions.src.json packs\math\algebra` после `npm ci` в `tools\tex2svg`; затем `dotnet test tests\MathSurvive.Core.Tests --filter BuiltPackTests`.
Expected: PASS; вручную проверить все `correct` и рендеринг формул.

- [ ] **Step 4: Commit**

```cmd
git add packs-src\math\algebra packs\math\algebra tests\MathSurvive.Core.Tests\BuiltPackTests.cs
git commit -m "Вопросы: 100 задач по алгебре"
```

### Task 7: Пакет «Геометрия»

**Files:**
- Create: `packs-src/math/geometry/questions.src.json`
- Create: `packs/math/geometry/section.json`, `questions.json`, `svg/*.svg`
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`

**Interfaces:**
- Consumes: `AssertPack` из Task 5.
- Produces: пакет `geometry`, 100 вопросов, уровни 40/35/25.

- [ ] **Step 1: Добавить красный вызов `AssertPack("geometry", 100, 40, 35, 25)` и запустить `BuiltPackTests`**

Expected: FAIL только из-за отсутствующего нового пакета.

- [ ] **Step 2: Составить 100 вопросов по геометрии**

ID `geometry-L<level>-NNN`; условия должны содержать все числовые данные и обозначения, необходимые без рисунка. Не использовать неоднозначные «найдите остальные элементы» или доказательства без одного ясного ответа.

- [ ] **Step 3: Сгенерировать, протестировать и проверить математику/рендеринг**

Run генератор для `geometry`, затем `BuiltPackTests`. Expected: PASS; вручную проверить все `correct`, единицы измерения и SVG.

- [ ] **Step 4: Commit**

```cmd
git add packs-src\math\geometry packs\math\geometry tests\MathSurvive.Core.Tests\BuiltPackTests.cs
git commit -m "Вопросы: 100 задач по геометрии"
```

### Task 8: Пакет «Математический анализ»

**Files:**
- Create: `packs-src/math/analysis/questions.src.json`
- Create: `packs/math/analysis/section.json`, `questions.json`, `svg/*.svg`
- Modify: `tests/MathSurvive.Core.Tests/BuiltPackTests.cs`

**Interfaces:**
- Consumes: `AssertPack` из Task 5.
- Produces: пакет `analysis`, 100 вопросов, уровни 40/35/25.

- [ ] **Step 1: Добавить красный вызов `AssertPack("analysis", 100, 40, 35, 25)` и запустить `BuiltPackTests`**

Expected: FAIL только из-за отсутствующего нового пакета.

- [ ] **Step 2: Составить 100 вопросов по математическому анализу**

ID `analysis-L<level>-NNN`; охватить пределы, производные, касательные, экстремумы и интегралы из Spec. Не выходить за несложные элементарные функции и аккуратные вычисления.

- [ ] **Step 3: Сгенерировать, протестировать и проверить математику/рендеринг**

Run генератор для `analysis`. Добавить в `BuiltPackTests` тест `All_pack_question_ids_are_unique_across_sections`, который загружает `test` и четыре новых пакета и сравнивает общее число ID с размером `HashSet`. Затем запустить полный `dotnet test tests\MathSurvive.Core.Tests`. Expected: PASS; вручную проверить все `correct`, области определения, константу интегрирования и SVG.

- [ ] **Step 4: Commit**

```cmd
git add packs-src\math\analysis packs\math\analysis tests\MathSurvive.Core.Tests\BuiltPackTests.cs
git commit -m "Вопросы: 100 задач по математическому анализу"
```

### Task 9: Документация и end-to-end проверка

**Files:**
- Modify: `AGENTS.md`
- Modify: `packs/SOURCES.md`
- Modify: `docs/superpowers/plans/2026-10-30-quiz-sections-room.md` (отметки выполненных шагов)

**Interfaces:**
- Consumes: весь функционал Tasks 1–8.

- [ ] **Step 1: Обновить проектные инструкции**

В `AGENTS.md` заменить описание одного текущего пакета на четыре раздела и random-цикл, добавить комнату/выбор в архитектуру и команды генерации для любого `<section>`. В `packs/SOURCES.md` указать, что все четыре пакета оригинальные и составлены для проекта, внешние наборы не используются.

- [ ] **Step 2: Прогнать полный build и экспорт**

Run: `build.cmd`
Expected: solution собирается, все тесты PASS, `build\MathSurvive.exe` создан, четыре пакета скопированы в `build\packs\math`; пользовательские файлы в `build` не удалены.

- [ ] **Step 3: Проверить реальные сценарии**

Через `start.cmd` вручную проверить:
- клик по каждой из пяти иконок вызывает `walk`, повторные клики во время движения игнорируются, квиз открывается только по прибытии и соответствует выбранному разделу;
- random даёт новые вопросы строго в порядке четырёх разделов, сворачивание и орда не сдвигают порядок;
- смена неотвеченного вопроса не добавляет ответ в статистику; после game over видны русские названия разделов;
- Bubble/Quiz остаются над персонажем, клики мимо иконок проходят насквозь, зомби останавливаются у новой позиции;
- при системном масштабе 150% иконки и длинная подпись остаются доступными; раскладка 1024×700 дополнительно проверена unit-тестом в Task 3.

- [ ] **Step 4: Проверить итоговый diff и отсутствие мусора**

Run: `git status --short`
Expected: только запланированные изменения; нет PNG-снимков, логов, `node_modules` или файлов экспорта. Не включать unrelated-файлы, перечисленные в Global Constraints, если они не менялись задачами.

- [ ] **Step 5: Commit**

```cmd
git add AGENTS.md packs\SOURCES.md docs\superpowers\plans\2026-10-30-quiz-sections-room.md
git commit -m "Документация: разделы и комната выбора"
```
