# Чекпоинт: План фиксов рендер-пайплайна v4

**Дата:** 2026-04-18
**Фаза:** Планирование (код НЕ пишем)
**Статус:** plan_approved
**Цель:** Итоговый план фиксов на основе полного аудита (04-16, 04-17) и анализа кода

---

## Аудит 04-14 — ЗАКРЫТ

**Решение:** Принят к сведению, отложен. Аудит 04-14 содержал корректные фиксы своего времени (8/9 багов), но визуальные регрессии появились позже (04-16) из-за внедрения Harvest System. Проблемы 04-14 не являются причиной текущих визуальных багов.

---

## КОРНЕВАЯ ПРИЧИНА ВСЕХ ВИЗУАЛЬНЫХ ПРОБЛЕМ

### 🔴 BUG-A: Sorting Layer "Objects" НЕ СУЩЕСТВУЕТ

**Это единственная причина, почему ВСЕ предыдущие фиксы (FIX-1..FIX-6) не сработали.**

Unity имеет **ДВЕ РАЗНЫЕ системы слоёв**:

| Система | Где настраивается | Что делает | FullSceneBuilder |
|---------|------------------|-----------|-----------------|
| **Physics Layers** | Tag Manager → Layers | Коллизии, Physics2D, go.layer | ✅ Создаёт (Phase 02) |
| **Sorting Layers** | Tag Manager → m_SortingLayers | Порядок рендеринга 2D, sortingLayerName | ❌ НЕ создаёт! |

**Что происходит при `sortingLayerName = "Objects"` когда слой не существует (Unity 6+):**
- Unity логирует: `SortingLayer with name 'Objects' does not exist`
- SpriteRenderer **игнорирует** невалидное имя
- Спрайт **НЕ РЕНДЕРИТСЯ** — становится полностью невидимым
- Это отличается от Unity 5, где fallback был на "Default"

### Почему terrain тайлы ВИДНЫ, а игрок/ресурсы — НЕТ

| Компонент | sortingLayerName | Слой существует? | Видимость |
|-----------|-----------------|------------------|-----------|
| TilemapRenderer (Terrain) | "Default" (по умолчанию) | ✅ Да | ✅ Видим |
| TilemapRenderer (Objects) | "Default" (по умолчанию) | ✅ Да | ✅ Видим |
| PlayerVisual.mainSprite | "Objects" | ❌ Нет | ❌ Невидим |
| PlayerVisual.shadowSprite | "Objects" | ❌ Нет | ❌ Невидим |
| HarvestableSpawner SpriteRenderer | "Objects" | ❌ Нет | ❌ Невидим |
| ResourceSpawner SpriteRenderer | "Objects" | ❌ Нет | ❌ Невидим |
| DestructibleObjectController SpriteRenderer | "Objects" | ❌ Нет | ❌ Невидим |

---

## ПОЧЕМУ ПРЕДЫДУЩИЕ ФИКСЫ НЕ СРАБОТАЛИ

| FIX | Что было сделано | Почему не сработал |
|-----|-----------------|-------------------|
| FIX-1 (Unlit shader) | ✅ Шейдер правильный | НО Sorting Layer "Objects" не существует → спрайт не рендерится |
| FIX-2 (sortingLayerName="Objects") | ❌ Слой не существует | Unity 6+ игнорирует невалидный слой → спрайт невидим |
| FIX-3 (PPU=31) | ⚠️ Недостаточное перекрытие | 1.6% мало для Bilinear, нужно PPU=30 (6.7%) |
| FIX-4 (EnsureSpriteImportSettings) | ✅ Работает корректно | Но спрайты всё равно не видны из-за Sorting Layer |
| FIX-6 (EnsureTileSpriteImportSettings) | ✅ Работает для terrain | Но белая сетка из-за PPU=31 |

---

## ПОЛНЫЙ СПИСОК БАГОВ (приоритизированный)

### 🔴 P0: BUG-A — Sorting Layers не созданы (КРИТИЧЕСКИЙ)

**Файл:** `FullSceneBuilder.cs` — Phase 02 (ExecuteTagsLayers)

**Проблема:** `ExecuteTagsLayers()` создаёт только Physics Layers через `TagManager.asset → layers[]`. Sorting Layers находятся в `TagManager.asset → m_SortingLayers` — этот массив **никогда не заполняется**.

**Решение:** В FullSceneBuilder Phase 02 добавить создание Sorting Layers через `SerializedObject`:

```
Sorting Layers (порядок важен — снизу вверх):
  0. "Default"     (уже существует — дефолтный)
  1. "Background"  — фоновые элементы
  2. "Terrain"     — terrain TilemapRenderer
  3. "Objects"     — объекты TilemapRenderer, SpriteRenderer объектов
  4. "Player"      — игрок и его тень
  5. "UI"          — UI элементы в мировом пространстве
```

**Метод:** Через `TagManager.asset → m_SortingLayers` (SerializedProperty array). Каждый элемент имеет `name` (string) и `uniqueID` (int). Дефолтный "Default" уже имеет ID=0.

**Влияет на:** ВСЕ спрайты с `sortingLayerName = "Objects"` — игрока, harvestable, ресурсы, деструктивы.

---

### 🟡 P1: BUG-B — sortOrder vs sortingOrder в FullSceneBuilder (ВЫСОКИЙ)

**Файл:** `FullSceneBuilder.cs` строки 1074, 1091

**Проблема:**
```csharp
terrainRenderer.sortOrder = (TilemapRenderer.SortOrder)0;  // ← SortOrder enum!
objectRenderer.sortOrder = (TilemapRenderer.SortOrder)1;   // ← НЕ sortingOrder!
```

`TilemapRenderer.SortOrder` — это **enum направления сортировки**:
- 0 = Bottom Left, 1 = Top Right, 2 = Top Left, 3 = Bottom Right

Это **НЕ** свойство `Renderer.sortingOrder` (int), которое определяет приоритет рендеринга!

**Решение:**
```csharp
// Было (неправильно):
terrainRenderer.sortOrder = (TilemapRenderer.SortOrder)0;
objectRenderer.sortOrder = (TilemapRenderer.SortOrder)1;

// Стало (правильно):
terrainRenderer.sortingLayerName = "Terrain";
terrainRenderer.sortingOrder = 0;
objectRenderer.sortingLayerName = "Objects";
objectRenderer.sortingOrder = 0;
```

---

### 🟡 P2: BUG-C — PPU=31 недостаточно для устранения белой сетки (ВЫСОКИЙ)

**Текущее состояние:**
- Grid.cellSize = (2.0, 2.0, 1.0)
- AI terrain спрайты: 64×64, PPU=31 → 64/31 = 2.065u → перекрытие 0.032u (1.6%)
- Процедурные: 68×68, PPU=31 → 68/31 = 2.194u → перекрытие 0.194u (9.7%)

**Проблема:** 1.6% перекрытия недостаточно для Bilinear фильтрации. Субпиксельные зазоры видны при определённых условиях камеры.

**Решение:** PPU=30 для terrain AI-спрайтов → 64/30 = 2.133u (6.7% перекрытие). Это надёжнее.

**Файлы для изменения PPU 31→30:**
1. `TileMapController.cs` — EnsureTileSpriteImportSettings (строка 267)
2. `TileMapController.cs` — CreateProceduralTileSprite (строка 303)
3. `HarvestableSpawner.cs` — EnsureSpriteImportSettings (строка 400)
4. `FullSceneBuilder.cs` — ReimportTileSprites (строка 1809)
5. `TileSpriteGenerator.cs` — TERRAIN_PPU (строка 43)

---

### 🟢 P3: BUG-D — Нет AssetDatabase.Refresh() после реимпорта (СРЕДНИЙ)

**Файлы:** TileMapController.cs, HarvestableSpawner.cs — EnsureSpriteImportSettings

**Проблема:** После `ImportAsset()` нет `AssetDatabase.Refresh()`. Спрайт может ещё не обновиться в кэше AssetDatabase при следующем `LoadAssetAtPath`.

**Решение:** Добавить после `ImportAsset()`:
```csharp
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
```

---

### 🟢 P4: BUG-E — TilemapRenderer не на Sorting Layer "Terrain" (НИЗКИЙ)

**Файл:** FullSceneBuilder.cs — Phase 08

**Проблема:** TilemapRenderer для Terrain использует дефолтный "Default" sorting layer. После создания Sorting Layers (BUG-A), Terrain TilemapRenderer должен быть на "Terrain" слое, а Objects TilemapRenderer на "Objects" слое.

**Решение:** В FullSceneBuilder Phase 08 (ExecuteTilemap):
```csharp
terrainRenderer.sortingLayerName = "Terrain";
terrainRenderer.sortingOrder = 0;
objectRenderer.sortingLayerName = "Objects";
objectRenderer.sortingOrder = 0;
```

---

### 🟢 P5: BUG-F — PlayerVisual sorting layer должен быть "Player" (НИЗКИЙ)

**Файл:** PlayerVisual.cs строки 96, 126

**Проблема:** Сейчас `sortingLayerName = "Objects"`. После создания полноценных Sorting Layers, игрок должен быть на собственном слое "Player" — выше всех объектов.

**Решение:**
```csharp
// Было:
mainSprite.sortingLayerName = "Objects";
shadowSprite.sortingLayerName = "Objects";

// Стало:
mainSprite.sortingLayerName = "Player";
mainSprite.sortingOrder = 0;
shadowSprite.sortingLayerName = "Player";
shadowSprite.sortingOrder = -1;  // Тень чуть ниже игрока
```

---

## ПОРЯДОК РЕАЛИЗАЦИИ (ПЛАН ФИКСОВ)

### FIX-V2-1: Создание Sorting Layers в FullSceneBuilder [BUG-A] — P0

**Файл:** `FullSceneBuilder.cs` — Phase 02 (ExecuteTagsLayers)

**Что добавить:** После создания Physics Layers — создание Sorting Layers через SerializedObject.

**Sorting Layers (порядок важен — снизу вверх):**
1. "Default" (уже существует — дефолтный, ID=0)
2. "Background" (ID=1)
3. "Terrain" (ID=2)
4. "Objects" (ID=3)
5. "Player" (ID=4)
6. "UI" (ID=5)

**Метод:** Через `TagManager.asset → m_SortingLayers` (SerializedProperty array).

**Риск:** Низкий. Идемпотентно — повторный запуск безопасен. "Default" уже существует, новые слои добавляются в конец.

**Консольный лог:** `[FullSceneBuilder] Sorting Layers созданы: Default, Background, Terrain, Objects, Player, UI`

---

### FIX-V2-2: Исправление sortOrder → sortingOrder [BUG-B + BUG-E] — P1

**Файл:** `FullSceneBuilder.cs` — Phase 08 (ExecuteTilemap), строки 1074, 1091

**Заменить:**
```csharp
// БЫЛО (неправильно):
terrainRenderer.sortOrder = (TilemapRenderer.SortOrder)0;
objectRenderer.sortOrder = (TilemapRenderer.SortOrder)1;

// СТАЛО (правильно):
terrainRenderer.sortingLayerName = "Terrain";
terrainRenderer.sortingOrder = 0;
objectRenderer.sortingLayerName = "Objects";
objectRenderer.sortingOrder = 0;
```

**Риск:** Низкий. sortOrder (направление) оставляем дефолтным (Bottom Left).

**Консольный лог:** `[FullSceneBuilder] TilemapRenderer: Terrain→"Terrain" order=0, Objects→"Objects" order=0`

---

### FIX-V2-3: PPU=30 для terrain спрайтов [BUG-C] — P2

**Файлы (5 мест):**
1. `TileMapController.cs` EnsureTileSpriteImportSettings — `31 → 30`
2. `TileMapController.cs` CreateProceduralTileSprite — `31 → 30`
3. `HarvestableSpawner.cs` EnsureSpriteImportSettings — `31 → 30`
4. `FullSceneBuilder.cs` ReimportTileSprites — `31 → 30`
5. `TileSpriteGenerator.cs` TERRAIN_PPU — `31 → 30`

**Риск:** Низкий. PPU=30 даёт 6.7% перекрытие вместо 1.6%. Процедурные 68×68 при PPU=30 = 68/30 = 2.267u — тоже с bleed.

**Консольный лог:** `[TileMapController] Terrain PPU=30: 64/30=2.133u (6.7% bleed)`

---

### FIX-V2-4: AssetDatabase.Refresh() после реимпорта [BUG-D] — P3

**Файлы:**
1. `TileMapController.cs` EnsureTileSpriteImportSettings — добавить Refresh
2. `HarvestableSpawner.cs` EnsureSpriteImportSettings — добавить Refresh

**Риск:** Минимальный. Refresh — стандартная практика после ImportAsset.

**Консольный лог:** `[TileMapController] AssetDatabase.Refresh() после реимпорта {assetPath}`

---

### FIX-V2-5: PlayerVisual sorting layer "Player" [BUG-F] — P4

**Файл:** `PlayerVisual.cs` строки 96, 126

**Заменить:**
```csharp
// Было:
mainSprite.sortingLayerName = "Objects";
shadowSprite.sortingLayerName = "Objects";

// Стало:
mainSprite.sortingLayerName = "Player";
mainSprite.sortingOrder = 0;
shadowSprite.sortingLayerName = "Player";
shadowSprite.sortingOrder = -1;
```

**Риск:** Низкий. При условии, что FIX-V2-1 создал Sorting Layer "Player".

**Консольный лог:** `[PlayerVisual] Sorting: layer="Player", mainOrder=0, shadowOrder=-1`

---

### FIX-V2-6: RenderPipelineLogger — диагностическая система [NEW]

**Новый файл:** `Assets/Scripts/Core/RenderPipelineLogger.cs`

**Назначение:** Статический класс с методами логирования для каждого этапа рендер-пайплайна. Выводит в Unity Console (видно пользователю).

**Методы:**

| Метод | Что логирует | Где вызывается |
|-------|-------------|---------------|
| `LogSortingLayers()` | Все Sorting Layers: имя, ID | FullSceneBuilder Phase 02 |
| `LogTilemapState()` | cellSize, cellGap, sortingLayer, sortingOrder, sortOrder | FullSceneBuilder Phase 08 |
| `LogSpriteImport(string path)` | PPU, alphaIsTransparency, textureType, filterMode | TileMapController.EnsureTileAssets |
| `LogTileRendered(int count)` | Кол-во тайлов, TileBase!=null, sprite!=null | TileMapController.RenderMap |
| `LogPlayerVisual(SpriteRenderer sr)` | sortingLayer, order, shader, sprite bounds, PPU | PlayerVisual.CreateVisual |
| `LogHarvestableSpawn(List<GameObject> spawned)` | Кол-во, sprite!=null, sortingLayer, shader | HarvestableSpawner.SpawnHarvestables |
| `LogCameraState()` | orthographic, z, size, backgroundColor | PlayerVisual.Start |
| `LogLightState()` | GlobalLight2D presence, type, intensity, color | FullSceneBuilder Phase 04 |
| `LogFullDiagnostics()` | Все этапы разом | ContextMenu на любом объекте |

**Формат лога (единообразный):**
```
[RPL-L1] ====== SORTING LAYERS ======
[RPL-L1]   [0] "Default" (id=0)
[RPL-L1]   [1] "Background" (id=1)
[RPL-L1]   [2] "Terrain" (id=2)
[RPL-L1]   [3] "Objects" (id=3) ← HarvestableSpawner, ResourceSpawner
[RPL-L1]   [4] "Player" (id=4) ← PlayerVisual
[RPL-L1]   [5] "UI" (id=5)
[RPL-L1] ✅ Sorting layer "Objects" exists (id=3)

[RPL-L5] ====== PLAYER VISUAL ======
[RPL-L5] Sprite: loaded=true, PPU=64, bounds=(2.0, 2.0), name="player_variant1_cultivator"
[RPL-L5] Sorting: layer="Player"(id=4), order=0
[RPL-L5] Material: shader="Universal Render Pipeline/2D/Sprite-Unlit-Default"
[RPL-L5] ✅ Player should be visible

[RPL-L6] ====== HARVESTABLE SPAWN ======
[RPL-L6] Spawned: 427 objects, 0 skipped
[RPL-L6] Sample[0]: Harvestable_Tree_Oak_10_20 → layer="Objects"(id=3), order=5, shader="Sprite-Unlit-Default", sprite=loaded
[RPL-L6] Sample[1]: Harvestable_Rock_Small_30_40 → layer="Objects"(id=3), order=3, shader="Sprite-Unlit-Default", sprite=loaded
[RPL-L6] ✅ Harvestable objects should be visible
```

**Риск:** Минимальный. Только Debug.Log, не влияет на рендеринг.

**Вызовы логирования (точки вставки):**
- `FullSceneBuilder.ExecuteTagsLayers()` → `RenderPipelineLogger.LogSortingLayers()`
- `FullSceneBuilder.ExecuteTilemap()` → `RenderPipelineLogger.LogTilemapState()`
- `FullSceneBuilder.ExecuteCameraLight()` → `RenderPipelineLogger.LogLightState()`
- `TileMapController.EnsureTileAssets()` → `RenderPipelineLogger.LogSpriteImport()` для каждого спрайта
- `TileMapController.RenderMap()` → `RenderPipelineLogger.LogTileRendered()`
- `PlayerVisual.CreateVisual()` → `RenderPipelineLogger.LogPlayerVisual()`
- `HarvestableSpawner.SpawnHarvestables()` → `RenderPipelineLogger.LogHarvestableSpawn()`
- `PlayerVisual.Start()` → `RenderPipelineLogger.LogCameraState()`

**ContextMenu для ручной диагностики:**
```csharp
[ContextMenu("Render Pipeline: Full Diagnostics")]
// На любом объекте в Scene — логирует всё разом
```

---

## СВОДНАЯ ТАБЛИЦА ФИКСОВ

| FIX | Баг | Приоритет | Файлы | Сложность | Зависимости |
|-----|-----|-----------|-------|-----------|-------------|
| FIX-V2-1 | BUG-A: Sorting Layers | P0 🔴 | FullSceneBuilder.cs | Средняя | — |
| FIX-V2-2 | BUG-B+E: sortOrder + TilemapRenderer layers | P1 🟡 | FullSceneBuilder.cs | Простой | FIX-V2-1 |
| FIX-V2-3 | BUG-C: PPU=30 | P2 🟡 | 5 файлов | Простой | — |
| FIX-V2-4 | BUG-D: Refresh после реимпорта | P3 🟢 | 2 файла | Простой | — |
| FIX-V2-5 | BUG-F: Player layer "Player" | P4 🟢 | PlayerVisual.cs | Простой | FIX-V2-1 |
| FIX-V2-6 | Система логирования | P5 🟢 | Новый файл + 7 вставок | Средняя | FIX-V2-1 |

---

## ГРАФ ЗАВИСИМОСТЕЙ

```
FIX-V2-1 (Sorting Layers) ←──── ROOT FIX, всё остальное зависит от него
├── FIX-V2-2 (TilemapRenderer layers) — зависит от V2-1 (слои должны существовать)
├── FIX-V2-5 (Player layer) — зависит от V2-1 (слой "Player" должен существовать)
└── FIX-V2-6 (Логирование) — зависит от V2-1 (логирует созданные слои)

FIX-V2-3 (PPU=30) — НЕЗАВИСИМ, можно делать параллельно
FIX-V2-4 (Refresh) — НЕЗАВИСИМ, можно делать параллельно
```

**Рекомендуемый порядок:**
1. FIX-V2-1 (Sorting Layers) — ПЕРВЫМ, это корневая причина
2. FIX-V2-3 (PPU=30) — параллельно с V2-1
3. FIX-V2-4 (Refresh) — параллельно с V2-1
4. FIX-V2-2 (TilemapRenderer layers) — после V2-1
5. FIX-V2-5 (Player layer) — после V2-1
6. FIX-V2-6 (Логирование) — после всех фиксов, для верификации

---

## ОЖИДАЕМЫЙ РЕЗУЛЬТАТ

После применения ВСЕХ фиксов:

| Проблема | До фикса | После фикса | Какой FIX решает |
|----------|----------|-------------|-----------------|
| Белая сетка между тайлами | ❌ Видна | ✅ Устранена (PPU=30, 6.7% bleed) | FIX-V2-3 |
| Игрок невидим | ❌ Не виден | ✅ Видим (слой "Player" существует) | FIX-V2-1 + V2-5 |
| Ресурсы невидимы | ❌ Не видны | ✅ Видимы (слой "Objects" существует) | FIX-V2-1 |
| Тень игрока невидима | ❌ Не видна | ✅ Видна (слой "Player", order=-1) | FIX-V2-1 + V2-5 |
| Terrain/Objects порядок | ❌ Негарантирован | ✅ Terrain→"Terrain", Objects→"Objects" | FIX-V2-2 |
| Спрайты не загружаются | ⚠️ Ненадёжно | ✅ Refresh после реимпорта | FIX-V2-4 |
| Диагностика | ❌ Нет логов | ✅ Полный лог рендер-пайплайна | FIX-V2-6 |

---

## РИСКИ И ОТКАТ

**Максимальный риск:** FIX-V2-1 (Sorting Layers). Если создание через SerializedObject не сработает (редкий случай — отсутствие прав на TagManager.asset), спрайты останутся невидимыми.

**Откат:** Удалить Sorting Layers из TagManager вручную (Edit → Project Settings → Tags and Layers), вернуть sortingLayerName = "Default" во всех файлах.

**Минимальный набор для тестирования:** Только FIX-V2-1. Если Sorting Layers созданы, а спрайты на "Objects"/"Player" слоях — они станут видимыми. PPU и Refresh — вторичны.

---

*Создано: 2026-04-18*
*Аудит 04-14: ЗАКРЫТ (принят к сведению, отложен)*
*Аудит 04-16: ЗАКРЫТ (корневая причина найдена в 04-17)*
*Аудит 04-17: ЗАКРЫТ (корневая причина подтверждена — BUG-A)*
*План фиксов: УТВЕРЖДЁН, ожидает реализации*
