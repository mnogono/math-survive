# Low-poly персонажи и орда Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use subagent-driven-development (recommended) or executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Заменить примитивы на анимированных low-poly героя с ружьём и разных зомби, добавить выстрел, смерть и кровь без изменения игровых правил.

**Architecture:** Выбранные CC0 glTF/GLB хранятся локально и инстанцируются существующими Godot view-классами. `PlayerView` и `ZombieView` владеют моделями и `AnimationPlayer`; `HordeDirector` по-прежнему владеет боем и только вызывает визуальные методы. Трупы сразу исключаются из списка активных целей, но временно остаются в дереве сцены.

**Tech Stack:** Godot 4.7.2, C#/.NET 8, glTF/GLB, существующий xUnit-проект для Core.

**Spec:** `docs/superpowers/specs/2026-10-30-low-poly-horde-design.md`

## Global Constraints

- Не менять патроны, точность, темп стрельбы, размер орды, группы по 5–7 и случайные паузы 2–4 секунды.
- Использовать только CC0-файлы из согласованных источников; сохранить источники и лицензии в `assets/models/SOURCES.md`.
- Не добавлять зависимости, Mixamo, расчленение и новые игровые механики.
- Не коммитить без отдельного явного запроса пользователя.

## Review Focus

- Отсутствует ожидаемый клип: модель не должна исчезать или ронять игру; проверка импорта и списка клипов в Task 1, запуск сцен в Task 4.
- Убит последний зомби: орда завершается сразу, а corpse-анимация не остаётся активной целью; сценарий в Task 3 и Task 4.
- `Clear()` во время death: все активные зомби и трупы удаляются; реализация и проверка в Task 3.
- Масштаб/ориентация glTF: ноги остаются на базовой точке, герой и ружьё направлены вправо; снимки в Task 2 и Task 4.
- Случайный выбор повторяет модель: все доступные варианты остаются возможными, визуально разные зомби проверяются на группе в Task 4.

---

### Task 1: Выбрать и импортировать локальные ассеты

**Files:**
- Create: `assets/models/hero.glb`
- Create: `assets/models/zombie_1.glb`, `assets/models/zombie_2.glb`, `assets/models/zombie_3.glb`, `assets/models/zombie_4.glb`
- Create: `assets/models/shotgun.glb` только если у выбранного героя нет подходящего ружья
- Create: `assets/models/SOURCES.md`

**Interfaces:**
- Consumes: согласованные CC0-страницы Quaternius из спеки.
- Produces: локальные Godot-совместимые модели и фактический список имён клипов для Tasks 2–3.

- [ ] **Step 1: Загрузить бесплатные glTF/GLB из Zombie Apocalypse Kit**
  Выбрать одного выжившего и четыре разных врага; сохранить под указанными именами. Не добавлять остальные модели пакета. Проверить лицензию на официальной странице и записать URL, автора, CC0 и локальные файлы в `SOURCES.md`.

- [ ] **Step 2: Проверить ружьё и анимации по содержимому файлов**
  У героя должны быть клипы idle и shoot, у каждого зомби — walk и death. Если у героя нет ружья, загрузить CC0 Shotgun из спеки как `shotgun.glb`. Если обязательного клипа нет, выбрать другого персонажа из того же пакета; UAL2 использовать только как запасной вариант. Записать фактические имена клипов в `SOURCES.md`.

- [ ] **Step 3: Проверить импорт Godot**
  Run: `Godot_v4.7.2-stable_mono_win64.exe --headless --path . --import`
  Expected: exit 0; пять обязательных моделей импортированы без ошибок, отсутствующие текстуры и несовместимые клипы отсутствуют.

### Task 2: Анимированный герой с ружьём

**Files:**
- Modify: `scripts/PlayerView.cs`
- Modify: `scripts/HordeDirector.cs`
- Uses: `assets/models/hero.glb`, при необходимости `assets/models/shotgun.glb`

**Interfaces:**
- Consumes: модель и фактические idle/shoot клипы из Task 1; существующий `Stage.PixelToWorld`.
- Produces: `void PlayerView.PlayShot()` и существующий `Vector3 PlayerView.MuzzleWorld`, указывающий на дуло новой модели.

- [ ] **Step 1: Подключить модель в `PlayerView`**
  Заменить капсулу и BoxMesh на инстанс `hero.glb`; нормализовать масштаб до текущей высоты 0.6 мировой единицы, повернуть лицом/ружьём вправо и совместить ноги с `Position`. При отдельном ружье закрепить его на hand/weapon socket либо на кисти скелета. Сохранить `ScreenRect` и `HeadTopPx` в прежних координатах.

- [ ] **Step 2: Подключить idle, выстрел и дуло**
  В `_Ready` зациклить idle. Реализовать `PlayShot()`: проиграть shoot один раз и вернуться к idle. Обновить `MuzzleWorld` по узлу дула/ружья либо по фиксированной локальной точке, совпадающей с видимым стволом. В `HordeDirector.Shoot` вызывать `PlayShot()` при каждом фактическом выстреле (`Hit` или `Miss`), для чего сохранить `PlayerView` в поле при `Setup`.

- [ ] **Step 3: Проверить героя в игре**
  Run Godot с `--shot=.godot/hero.png --quit-after=2` и открыть PNG.
  Expected: герой стоит ногами на прежнем месте, ружьё видно и направлено вправо, idle работает, нет ошибок импорта/анимации. Удалить временный PNG после проверки.

### Task 3: Разные зомби, смерть и кровь

**Files:**
- Create: `scripts/BloodBurst.cs`
- Modify: `scripts/ZombieView.cs`
- Modify: `scripts/HordeDirector.cs`
- Uses: четыре `assets/models/zombie_*.glb`

**Interfaces:**
- Consumes: `HordePlanner` и существующий `_random` в `HordeDirector`; walk/death клипы из Task 1.
- Produces: `int ZombieView.Variant { get; set; }`, `void ZombieView.Die()`; `BloodBurst` самоудаляется после короткого эффекта.

- [ ] **Step 1: Подключить варианты и walk**
  `Variant` задаётся до `AddChild`; `_Ready` инстанцирует соответствующий `zombie_N.glb`, нормализует высоту до 0.56, ставит ноги в локальный ноль и зацикливает walk. В `HordeDirector.Spawn` выбирать вариант через `_random.Next(1, 5)` до добавления узла. На `AtPlayer` остановить walk, не меняя координаты и правила боя.

- [ ] **Step 2: Добавить `Die()` и `BloodBurst`**
  `Die()` прекращает walk, проигрывает death один раз, создаёт 12–18 красных low-poly осколков с коротким разлётом и удаляет визуальный узел примерно через 1 секунду. Эффект не регистрирует hit area и не изменяет `GameSession`.

- [ ] **Step 3: Отделить труп от активных целей**
  При `ShotOutcome.Hit` сразу удалить зомби из `_zombies`, увеличить `_killed`, затем вызвать `Die()` вместо немедленного `QueueFree`. `Nearest`, `CheckEnd` и стрельба продолжают использовать только `_zombies`. `Clear()` должен удалять все дочерние `ZombieView`, включая уже удалённые из `_zombies` трупы.

- [ ] **Step 4: Проверить смерть и завершение орды**
  Run Godot с `--horde-now --ammo=10000 --quit-after=40`.
  Expected: разные зомби идут группами, попадания вызывают кровь и death, трупы не получают повторные выстрелы, последний kill завершает орду без ожидания corpse-анимации, ошибок нет.

### Task 4: Полная проверка deliverable

**Files:**
- Modify только при обнаружении дефекта: файлы Tasks 1–3.

**Interfaces:**
- Consumes: полностью подключённые view и локальные ассеты.

- [ ] **Step 1: Запустить тесты и сборку**
  Run: `dotnet test tests/MathSurvive.Core.Tests/MathSurvive.Core.Tests.csproj --nologo && dotnet build MathSurvive.sln --nologo`
  Expected: все существующие тесты PASS; сборка с 0 ошибок и 0 предупреждений.

- [ ] **Step 2: Проверить обычный режим и орду на снимках**
  Снять обычный режим после 2 секунд, группу после 4 секунд и бой после 32–34 секунд с `--ammo=10000`. Открыть каждый PNG.
  Expected: idle героя, ружьё, разные walk-зомби, выстрел/кровь/death видимы; ноги и дуло выровнены, модели не обрезаны.

- [ ] **Step 3: Проверить чистоту результата**
  Удалить временные PNG. Run: `git diff --check && git status --short`.
  Expected: только нужные ассеты, manifest, спека/план и изменённые исходники; временных файлов и whitespace-ошибок нет.

### Task 5: Публикация и перенос в master

- [x] **Step 1: Проверить solution и Core**
  Run: `dotnet build MathSurvive.sln -nologo -v:minimal && dotnet test tests/MathSurvive.Core.Tests -nologo -v:minimal`
  Result: сборка успешна, 0 ошибок и 0 предупреждений; 65/65 тестов пройдено.

- [x] **Step 2: Проверить реальный запуск орды**
  Run: `start.cmd -- --horde-now --ammo=10000 --quit-after=40 --shot=.godot/horde-final.png`
  Result: сцена орды отработала 40 секунд, повторная сборка успешна, снимок сохранён и визуально проверен; временные PNG удалены.
  Note: из Git Bash всю команду `cmd.exe /c` нужно заключать в кавычки, иначе аргументы не доходят до `start.cmd`.
  Next: закоммитить все изменения и отправить `stage0-2`.

- [ ] **Step 3: Закоммитить и отправить `stage0-2`**
  Run: `git add -A && git commit && git push origin stage0-2`
  Expected: все текущие изменения сохранены, локальная и удалённая ветки синхронизированы.

- [ ] **Step 4: Слить в `master` и отправить**
  Run: `git checkout master && git pull --ff-only origin master && git merge stage0-2 && dotnet test tests/MathSurvive.Core.Tests -nologo -v:minimal && git push origin master`
  Expected: слияние успешно, тесты на результате зелёные, `master` и `origin/master` указывают на один коммит.
