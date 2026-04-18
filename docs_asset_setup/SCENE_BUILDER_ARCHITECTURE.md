# Архитектура генерации сцены — Оркестратор + фазовые файлы

**Создано:** 2026-04-17 13:49:14 UTC
**Редактировано:** 2026-04-18 17:17:54 UTC
**Версия:** 2.0

---

## Принцип

Генерация сцены построена по паттерну **Оркестратор + отдельные файлы фаз**. Каждая фаза — изолированный класс в отдельном файле. Оркестратор только регистрирует фазы и управляет их запуском. Это предотвращает регрессионные баги и упрощает модификацию отдельных фаз.

---

## Структура файлов

```
Assets/Scripts/Editor/
├── FullSceneBuilder.cs                  # ОРКЕСТРАТОР (заморожен)
└── SceneBuilder/
    ├── IScenePhase.cs                   # Интерфейс фазы
    ├── SceneBuilderConstants.cs         # Общие константы
    ├── SceneBuilderUtils.cs             # Общие утилиты
    ├── Phase01Folders.cs                # Фаза 01: Папки
    ├── Phase02TagsLayers.cs             # Фаза 02: Теги и слои
    ├── Phase03SceneCreation.cs          # Фаза 03: Создание сцены
    ├── Phase04CameraLight.cs            # Фаза 04: Камера и свет
    ├── Phase05GameManager.cs            # Фаза 05: GameManager
    ├── Phase06Player.cs                 # Фаза 06: Игрок
    ├── Phase07UI.cs                     # Фаза 07: UI
    ├── Phase08Tilemap.cs                # Фаза 08: Tilemap
    ├── Phase09GenerateAssets.cs         # Фаза 09: Генерация ассетов
    ├── Phase10GenerateSprites.cs        # Фаза 10: Спрайты тайлов
    ├── Phase11GenerateUIPrefabs.cs      # Фаза 11: UI-префабы
    ├── Phase12TMPEssentials.cs          # Фаза 12: TMP Essentials
    ├── Phase13SaveScene.cs             # Фаза 13: Сохранение сцены
    ├── Phase14CreateTileAssets.cs       # Фаза 14: Tile .asset файлы
    └── Phase15ConfigureTestLocation.cs  # Фаза 15: Тестовая локация
```

---

## Оркестратор: FullSceneBuilder.cs — ЗАМОРОЖЕН 🧊

**Путь:** `Assets/Scripts/Editor/FullSceneBuilder.cs`
**Версия:** 2.0 (FROZEN)
**Строк:** ~224
**Статус:** ЗАМОРОЖЕН — редактирование ЗАПРЕЩЕНО

### Что делает

- Регистрирует 15 фаз в массиве `PHASES`
- Управляет запуском: `IsNeeded()` → `Execute()` → логирование
- Обрабатывает ошибки: try/catch с диалогом продолжения
- Предоставляет меню: `Tools → Full Scene Builder → Build All` и отдельные фазы

### Почему заморожен

- Оркестратор НЕ содержит логики создания объектов — только диспетчеризацию
- Любое изменение оркестратора может сломать ВСЮ сборку сцены
- Добавление новых фаз = добавление строки в массив `PHASES` — это безопасная операция, но требует подтверждения

### ⚠️ АБСОЛЮТНЫЙ ЗАПРЕТ

**НЕ переписывать оркестратор в монолит.** Не объединять фазовые файлы обратно в FullSceneBuilder.cs.
Не менять архитектуру «оркестратор + фазовые файлы» ни при каких обстоятельствах.
Это решение закреплено пользователем после подтверждения работоспособности.

---

## Интерфейс: IScenePhase

```csharp
public interface IScenePhase
{
    string Name { get; }       // Короткое имя (для логирования)
    string MenuPath { get; }   // Путь в меню
    int Order { get; }         // Порядковый номер (1-15)
    bool IsNeeded();           // Проверяет, нужно ли выполнение
    void Execute();            // Выполняет фазу
}
```

Каждая фаза идемпотентна — повторный запуск безопасен, пропускает уже выполненное.

---

## Вспомогательные файлы

### SceneBuilderConstants.cs

Общие константы для всех фаз:
- `SCENE_PATH`, `SCENE_NAME`
- `REQUIRED_FOLDERS` (33 папки)
- `REQUIRED_TAGS` (7 тегов)
- `REQUIRED_LAYERS` (8 слоёв)
- `REQUIRED_SORTING_LAYERS` (6 sorting layers в порядке Default < Background < Terrain < Objects < Player < UI)

### SceneBuilderUtils.cs

Общие утилиты:
- `EnsureSceneOpen()` — проверка/открытие сцены
- `SetupComponent<T>()` — настройка компонента через SerializedObject
- `SetProperty()` — установка свойства SerializedObject
- `CleanMissingPrefabs()` — очистка Missing Scripts/Prefabs

---

## Реестр фаз

| # | Класс | Имя | Описание | Объединённые патчи |
|---|-------|-----|----------|-------------------|
| 01 | Phase01Folders | Folders | Создание 33 папок | — |
| 02 | Phase02TagsLayers | Tags & Layers | Теги, Physics Layers, Sorting Layers | PATCH-001, PATCH-011, PATCH-012 |
| 03 | Phase03SceneCreation | Scene Creation | Создание сцены, удаление дефолтной камеры | — |
| 04 | Phase04CameraLight | Camera & Light | Camera2D, GlobalLight2D, Directional Light | PATCH-004, PATCH-009, PATCH-010 |
| 05 | Phase05GameManager | GameManager | GameInitializer + системные компоненты | — |
| 06 | Phase06Player | Player | Rigidbody2D + 8 компонентов + PlayerVisual | PATCH-003 |
| 07 | Phase07UI | UI | Canvas + EventSystem + HUD | — |
| 08 | Phase08Tilemap | Tilemap | Grid + Terrain/Objects + TileMapController | PATCH-002, PATCH-005, PATCH-006, PATCH-007 |
| 09 | Phase09GenerateAssets | Generate Assets | JSON → ScriptableObjects | — |
| 10 | Phase10GenerateSprites | Tile Sprites | Процедурные спрайты | — |
| 11 | Phase11GenerateUIPrefabs | Formation UI | UI-префабы формаций | — |
| 12 | Phase12TMPEssentials | TMP Essentials | Импорт TMP | — |
| 13 | Phase13SaveScene | Save Scene | EditorSceneManager.SaveScene | — |
| 14 | Phase14CreateTileAssets | Tile Assets | TerrainTile + ObjectTile для 15+ типов | — |
| 15 | Phase15ConfigureTestLocation | Test Location | Камера + коллайдеры + HarvestableSpawner | — |

---

## Устаревший файл: ScenePatchBuilder.cs — DEPRECATED ⛔

**Путь:** `Assets/Scripts/Editor/ScenePatchBuilder.cs`
**Статус:** УСТАРЕЛ, оставлен для обратной совместимости меню

Все 12 патчей (PATCH-001..PATCH-012) объединены в соответствующие фазовые файлы.
ScenePatchBuilder.cs перенаправляет пользователя к `Tools → Full Scene Builder → Build All`.

**НЕ добавлять новые патчи в ScenePatchBuilder.cs.**
Все изменения сцены вносятся в соответствующий фазовый файл.

---

## Как добавить изменение сцены

### Если изменение относится к существующей фазе:

1. Открыть нужный `PhaseNNXxx.cs`
2. Добавить логику в `Execute()` (с проверкой — уже применено?)
3. Добавить using для новых пространств имён при необходимости
4. Протестировать: `Tools → Full Scene Builder → [нужная фаза]`
5. Закоммитить с пометкой `PhaseNN: описание изменения`

### Если нужна новая фаза:

1. Создать `PhaseNNName.cs` в `Assets/Scripts/Editor/SceneBuilder/`
2. Реализовать `IScenePhase` (Name, MenuPath, Order, IsNeeded, Execute)
3. Добавить `using CultivationGame.Editor.SceneBuilder;`
4. Зарегистрировать в массиве `PHASES` в FullSceneBuilder.cs
5. Протестировать

### ❌ Чего НЕ делать:

- НЕ редактировать FullSceneBuilder.cs кроме добавления строки в `PHASES`
- НЕ объединять фазовые файлы обратно в монолит
- НЕ добавлять патчи в ScenePatchBuilder.cs
- НЕ менять интерфейс IScenePhase без согласования

---

## Меню Unity

- `Tools → Full Scene Builder → Build All (One Click)` — полная сборка
- `Tools → Full Scene Builder → Phase NN: ...` — отдельная фаза
- `Tools → Scene Patch Builder → ...` — УСТАРЕЛО, перенаправляет к Full Scene Builder

---

## Правило: НЕ РЕДАКТИРОВАТЬ FullSceneBuilder.cs

**Если агент собирается редактировать FullSceneBuilder.cs — ОСТАНОВИТЬСЯ и:**
1. Это добавление новой фазы в массив `PHASES`? → допустимо, но запросить подтверждение
2. Это рефакторинг оркестратора? → ЗАПРЕЩЕНО
3. Это объединение фаз в монолит? → ЗАПРЕЩЕНО
4. Это критический багфикс? → запросить подтверждение у пользователя

**Нарушение этого правила = регрессионный баг и потеря времени.**

---

## Известные проблемы

1. **Белые швы между terrain-тайлами** — Sprite.Create с FilterMode.Point может оставлять 1px артефакты. Возможное решение: pad-спрайты или Sprite.DrawMode.Sliced
2. **Hero rendering behind surface** — Player может рендериться за поверхностью при неправильном порядке Sorting Layers. Покрывается Phase02TagsLayers
