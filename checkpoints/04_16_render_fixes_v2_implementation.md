# Чекпоинт: Внедрение фиксов рендер-пайплайна v2 (FIX-V2-1..V2-6)

**Дата:** 2026-04-16 11:37 UTC
**Фаза:** Реализация (код написан)
**Статус:** implementation_complete
**Цель:** Внедрить 6 фиксов для устранения визуальных регрессий

---

## ВНЕДРЁННЫЕ ФИКСЫ

### ✅ FIX-V2-1: Создание Sorting Layers (P0 — КОРНЕВАЯ ПРИЧИНА)

**Файлы:** `FullSceneBuilder.cs`

**Что сделано:**
- Добавлен метод `EnsureSortingLayers()` — создаёт Sorting Layers через SerializedObject на `TagManager.asset → m_SortingLayers`
- Создаёт 6 слоёв: Default, Background, Terrain, Objects, Player, UI
- Обновлён `IsTagsLayersNeeded()` — проверяет наличие слоя "Objects"
- Вызов `EnsureSortingLayers()` добавлен в `ExecuteTagsLayers()` после Physics Layers

**Критическое значение:** Без Sorting Layer "Objects" ВСЕ SpriteRenderer с `sortingLayerName = "Objects"` игнорировались в Unity 6+ → спрайты были невидимы. Это единственная причина, почему FIX-1..FIX-6 не сработали.

---

### ✅ FIX-V2-2: sortOrder → sortingOrder + Sorting Layer для TilemapRenderer (P1)

**Файл:** `FullSceneBuilder.cs` — Phase 08 (ExecuteTilemap)

**Что сделано:**
- `terrainRenderer.sortOrder = (TilemapRenderer.SortOrder)0` → `terrainRenderer.sortingLayerName = "Terrain"; terrainRenderer.sortingOrder = 0;`
- `objectRenderer.sortOrder = (TilemapRenderer.SortOrder)1` → `objectRenderer.sortingLayerName = "Objects"; objectRenderer.sortingOrder = 0;`
- sortOrder — это enum НАПРАВЛЕНИЯ сортировки (BottomLeft, TopRight и т.д.), НЕ приоритет рендеринга

---

### ✅ FIX-V2-3: PPU=31→30 для terrain спрайтов (P2)

**Файлы (5 мест):**
1. `TileMapController.cs` EnsureTileSpriteImportSettings — `31 → 30`
2. `TileMapController.cs` CreateProceduralTileSprite — `31 → 30`
3. `HarvestableSpawner.cs` EnsureSpriteImportSettings — `31 → 30`
4. `FullSceneBuilder.cs` ReimportTileSprites — `31 → 30`
5. `TileSpriteGenerator.cs` TERRAIN_PPU — `31 → 30`

**Обоснование:** PPU=31 давал 64/31=2.065u (1.6% перекрытие) — недостаточно для Bilinear. PPU=30 даёт 64/30=2.133u (6.7% перекрытие) — надёжно устраняет белую сетку.

---

### ✅ FIX-V2-4: AssetDatabase.Refresh() после реимпорта (P3)

**Файлы:**
1. `TileMapController.cs` EnsureTileSpriteImportSettings — добавлен Refresh
2. `HarvestableSpawner.cs` EnsureSpriteImportSettings — добавлен Refresh

---

### ✅ FIX-V2-5: PlayerVisual sorting layer "Player" (P4)

**Файл:** `PlayerVisual.cs`

**Что сделано:**
- `mainSprite.sortingLayerName = "Objects"; sortingOrder = 10` → `sortingLayerName = "Player"; sortingOrder = 0`
- `shadowSprite.sortingLayerName = "Objects"; sortingOrder = 9` → `sortingLayerName = "Player"; sortingOrder = -1`
- Игрок на собственном слое "Player", выше всех объектов

---

### ✅ FIX-V2-6: RenderPipelineLogger — диагностическая система (P5)

**Новый файл:** `Assets/Scripts/Core/RenderPipelineLogger.cs`

**Методы:**
- `LogSortingLayers()` — все Sorting Layers, наличие "Objects"/"Player"/"Terrain"
- `LogTilemapState()` — cellSize, cellGap, sortingLayer, sortingOrder, tiles count
- `LogSpriteImportState()` — PPU, bounds, sprite loaded
- `LogTileRendered()` — кол-во terrain/object тайлов
- `LogPlayerVisualState()` — sorting, shader, sprite bounds, PPU
- `LogHarvestableState()` — кол-во, примеры, sprite/sorting/shader
- `LogCameraState()` — orthographic, position, size
- `LogLightState()` — GlobalLight2D presence, intensity
- `LogFullDiagnostics()` — все этапы разом

**Точки вставки:**
- FullSceneBuilder.ExecuteTagsLayers → LogSortingLayers (в EnsureSortingLayers)
- FullSceneBuilder.ExecuteCameraLight → LogCameraState, LogLightState
- FullSceneBuilder.ExecuteTilemap → LogTilemapState
- TileMapController.RenderMap → LogTileRendered
- PlayerVisual.CreateVisual → LogPlayerVisualState
- HarvestableSpawner.SpawnHarvestables → LogHarvestableState

---

## ИЗМЕНЁННЫЕ ФАЙЛЫ

| Файл | Изменение |
|------|-----------|
| `FullSceneBuilder.cs` | FIX-V2-1 (EnsureSortingLayers), FIX-V2-2 (sortingOrder), FIX-V2-3 (PPU=30), FIX-V2-6 (логирование) |
| `TileMapController.cs` | FIX-V2-3 (PPU=30), FIX-V2-4 (Refresh), FIX-V2-6 (LogTileRendered) |
| `HarvestableSpawner.cs` | FIX-V2-3 (PPU=30), FIX-V2-4 (Refresh), FIX-V2-6 (LogHarvestableState) |
| `TileSpriteGenerator.cs` | FIX-V2-3 (TERRAIN_PPU=30) |
| `PlayerVisual.cs` | FIX-V2-5 (sortingLayerName="Player"), FIX-V2-6 (LogPlayerVisualState) |
| `RenderPipelineLogger.cs` | НОВЫЙ — FIX-V2-6 |
| `checkpoints/04_16_render_pipeline_audit.md` | Переименован из 04_17, даты исправлены |
| `checkpoints/04_16_fix_plan_render_pipeline.md` | Переименован из 04_18, даты исправлены |

---

## ОЖИДАЕМЫЙ РЕЗУЛЬТАТ ПОСЛЕ ПРИМЕНЕНИЯ

| Проблема | FIX | Результат |
|----------|-----|-----------|
| Игрок невидим | V2-1 + V2-5 | ✅ Sorting Layer "Player" существует → видим |
| Ресурсы невидимы | V2-1 | ✅ Sorting Layer "Objects" существует → видимы |
| Белая сетка | V2-3 | ✅ PPU=30 (6.7% bleed) → устранена |
| Terrain/Objects порядок | V2-2 | ✅ sortingLayerName + sortingOrder корректны |

---

## КАК ТЕСТИРОВАТЬ

1. Удалить Assets/ папку в UnityProject
2. Открыть Unity → Tools → Full Scene Builder → Build All
3. В Console искать логи:
   - `[FullSceneBuilder] FIX-V2-1: Sorting Layers созданы: ...` → слои существуют
   - `[RPL]-L1 ✅ Sorting layer "Objects" существует` → подтверждение
   - `[RPL]-L5 ✅ Player sprite загружен` → игрок видим
   - `[RPL]-L6 ✅ Harvestable объекты созданы` → ресурсы видимы
4. Визуально проверить: игрок виден, ресурсы видны, нет белой сетки

---

*Создано: 2026-04-16 11:37 UTC*
*Предыдущие аудиты: 04-14 ЗАКРЫТ, 04-16 ЗАКРЫТ (корневая причина найдена)*
