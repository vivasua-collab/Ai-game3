# Чекпоинт: Аудит рендер-пайплайна v3

**Дата:** 2026-04-17
**Фаза:** Аудит после применения FIX-1..FIX-6 (визуал НЕ исправлен)
**Статус:** audit_complete
**Цель:** Найти корневую причину, почему предыдущие фиксы НЕ сработали

---

## ПРЕДЫДУЩИЕ ФИКСЫ — применены, но НЕ работают

| FIX | Что было сделано | Где | Статус |
|-----|-----------------|-----|--------|
| FIX-1 (КРИТ-1) | Unlit шейдер в HarvestableSpawner | HarvestableSpawner.cs:242-245 | ✅ В коде, но спрайты НЕ видны |
| FIX-2 (КРИТ-2) | sortingLayerName = "Objects" для PlayerVisual | PlayerVisual.cs:96 | ✅ В коде, но игрок НЕ виден |
| FIX-3 (КРИТ-3) | PPU=31 для terrain спрайтов | TileMapController.cs:267,303 | ✅ В коде, но сетка НЕ ушла |
| FIX-4 (СРЕД-1) | EnsureSpriteImportSettings в HarvestableSpawner | HarvestableSpawner.cs:394-416 | ✅ В коде |
| FIX-6 (СРЕД-1) | EnsureTileSpriteImportSettings в TileMapController | TileMapController.cs:262-283 | ✅ В коде |

**Все фиксы применены корректно. Код компилируется. Но визуал НЕ работает.**

---

## 🔴 НОВАЯ КОРНЕВАЯ ПРИЧИНА: Sorting Layer "Objects" НЕ СУЩЕСТВУЕТ

### Суть проблемы

Unity имеет **ДВЕ РАЗНЫЕ системы слоёв**:

1. **Physics Layers** (слои коллизий) — Tag Manager → Layers
   - Используются для: `Physics2D`, `LayerMask`, `go.layer`
   - FullSceneBuilder Phase 02 СОЗДАЁТ: Player=6, NPC=7, ..., Harvestable=13

2. **Sorting Layers** (слои рендеринга 2D) — Tag Manager → Sorting Layers
   - Используются для: `Renderer.sortingLayerName`, `SpriteRenderer.sortingLayerName`
   - FullSceneBuilder Phase 02 **НЕ СОЗДАЁТ** ни одного Sorting Layer!

### Что происходит при `sortingLayerName = "Objects"` когда слой не существует:

**В Unity 6+ (6000.3):**
- Unity логирует: `SortingLayer with name 'Objects' does not exist`
- SpriteRenderer **игнорирует** невалидное имя слоя
- Спрайт **НЕ РЕНДЕРИТСЯ** — становится полностью невидимым
- Это отличается от старого поведения Unity 5, где fallback был на "Default"

### Файлы, использующие sortingLayerName = "Objects":

| Файл | Строка | Компонент | Следствие |
|------|--------|-----------|-----------|
| PlayerVisual.cs | 96 | mainSprite | ❌ Игрок невидим |
| PlayerVisual.cs | 126 | shadowSprite | ❌ Тень невидима |
| HarvestableSpawner.cs | 235 | sr (SpriteRenderer) | ❌ ВСЕ ресурсы невидимы |
| ResourceSpawner.cs | 258 | sr (SpriteRenderer) | ❌ Ресурсы невидимы |
| DestructibleObjectController.cs | 317 | sr (SpriteRenderer) | ❌ Разрушаемые невидимы |

### Сравнение с TilemapRenderer

TilemapRenderer (Terrain и Objects) — используют **sorting layer "Default"** (по умолчанию). Они НЕ устанавливают sortingLayerName = "Objects". Поэтому terrain-тайлы ВИДНЫ (они на "Default").

---

## 🔴 BUG-A: Sorting Layer "Objects" не создаётся (КРИТИЧЕСКИЙ)

**Причина:** `FullSceneBuilder.ExecuteTagsLayers()` создаёт только Physics Layers через `TagManager.asset → layers[]`. Sorting Layers находятся в `TagManager.asset → m_SortingLayers` — этот массив **никогда не заполняется**.

**Решение:** В FullSceneBuilder Phase 02 добавить создание Sorting Layers:

```
Sorting Layers (по порядку, снизу вверх):
  1. "Background"  — фоновые элементы
  2. "Terrain"     — terrain TilemapRenderer
  3. "Objects"     — объекты TilemapRenderer, SpriteRenderer объектов
  4. "Player"      — игрок и его тень
  5. "UI"          — UI элементы в мировом пространстве
```

Это делается через `SerializedObject` на `TagManager.asset`, поле `m_SortingLayers`.

---

## 🟡 BUG-B: TilemapRenderer.sortOrder — путаница с enum (ВЫСОКИЙ)

**Причина:** В FullSceneBuilder Phase 08:
```csharp
terrainRenderer.sortOrder = (TilemapRenderer.SortOrder)0;  // ← Это SortOrder enum!
objectRenderer.sortOrder = (TilemapRenderer.SortOrder)1;   // ← НЕ sortingOrder!
```

`TilemapRenderer.SortOrder` — это **enum направления сортировки**:
- 0 = Bottom Left
- 1 = Top Right
- 2 = Top Left
- 3 = Bottom Right

Это **НЕ** свойство `Renderer.sortingOrder` (int), которое определяет приоритет рендеринга!

**Следствие:** Оба TilemapRenderer имеют `sortingOrder = 0` (дефолт) на слое "Default". Порядок между ними не гарантирован.

**Решение:** Использовать `Renderer.sortingOrder` вместо `sortOrder`:
```csharp
terrainRenderer.sortingOrder = 0;   // Terrain — нижний слой
objectRenderer.sortingOrder = 1;    // Objects — поверх terrain
```

А `sortOrder` (направление) оставить дефолтным или установить осознанно.

---

## 🟡 BUG-C: PPU=31 даёт недостаточное перекрытие (ВЫСОКИЙ)

**Текущее состояние:**
- Grid.cellSize = (2.0, 2.0, 1.0)
- AI terrain спрайты: 64×64, PPU=31 → 64/31 = **2.065u** → перекрытие **0.032u** (1.6%)
- Процедурные terrain спрайты: 68×68, PPU=31 → 68/31 = **2.194u** → перекрытие **0.194u** (9.7%)

**Проблема:** 1.6% перекрытия недостаточно для Bilinear фильтрации. Субпиксельные зазоры всё ещё видны при определённых условиях камеры.

**Рекомендация:** PPU=30 для terrain AI-спрайтов → 64/30 = **2.133u** (6.7% перекрытие). Это надёжнее.

---

## 🟡 BUG-D: EnsureTileSpriteImportSettings не хватает AssetDatabase.Refresh() (СРЕДНИЙ)

**Проблема:** После `ImportAsset()` в `EnsureTileSpriteImportSettings()` нет `AssetDatabase.Refresh()`. Спрайт может ещё не обновиться в кэше.

**Решение:** Добавить `AssetDatabase.Refresh()` после `ImportAsset()`.

---

## 🟢 BUG-E: TilemapRenderer не на Sorting Layer "Terrain" (НИЗКИЙ)

**Проблема:** TilemapRenderer для Terrain использует дефолтный "Default" sorting layer. После создания Sorting Layers, Terrain TilemapRenderer должен быть на "Terrain" слое.

**Решение:** В FullSceneBuilder Phase 08 установить:
```csharp
terrainRenderer.sortingLayerName = "Terrain";
terrainRenderer.sortingOrder = 0;
objectRenderer.sortingLayerName = "Objects";
objectRenderer.sortingOrder = 0;
```

---

## ПЛАН ИСПРАВЛЕНИЯ (пошаговый)

### Шаг 1: Создание Sorting Layers в FullSceneBuilder [BUG-A]

**Файл:** `FullSceneBuilder.cs` — Phase 02 (ExecuteTagsLayers)

**Добавить:** После создания Physics Layers — создание Sorting Layers через SerializedObject.

**Sorting Layers (порядок важен — снизу вверх):**
1. "Default" (уже существует — дефолтный)
2. "Background"
3. "Terrain"
4. "Objects"
5. "Player"
6. "UI"

**Метод:** Через `TagManager.asset → m_SortingLayers` (SerializedProperty array).

### Шаг 2: Исправление sortOrder → sortingOrder [BUG-B]

**Файл:** `FullSceneBuilder.cs` — Phase 08 (ExecuteTilemap)

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

### Шаг 3: Обновление Sorting Layer для PlayerVisual [BUG-A follow-up]

**Файл:** `PlayerVisual.cs`

**Изменить:**
```csharp
// Было: mainSprite.sortingLayerName = "Objects";
mainSprite.sortingLayerName = "Player";
mainSprite.sortingOrder = 0; // Единственный на этом слое

// Было: shadowSprite.sortingLayerName = "Objects";
shadowSprite.sortingLayerName = "Player";
shadowSprite.sortingOrder = -1; // Ниже игрока на том же слое
```

### Шаг 4: PPU=30 для terrain спрайтов [BUG-C]

**Файл:** `TileMapController.cs`

**Изменить:**
- EnsureTileSpriteImportSettings: `int targetPPU = isObject ? 160 : 30;` (было 31)
- CreateProceduralTileSprite: `int ppu = isObject ? 160 : 30;` (было 31)

**Файл:** `HarvestableSpawner.cs`

**Изменить:**
- EnsureSpriteImportSettings: `int targetPPU = isObject ? 160 : 30;` (было 31)

### Шаг 5: AssetDatabase.Refresh() после реимпорта [BUG-D]

**Файл:** `TileMapController.cs` — EnsureTileSpriteImportSettings

**Добавить после ImportAsset():**
```csharp
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
```

**Файл:** `HarvestableSpawner.cs` — EnsureSpriteImportSettings

**Добавить то же самое.**

---

## СИСТЕМА ЛОГИРОВАНИЯ (RenderPipelineLogger)

### Назначение
Компонент, логирующий полную диагностику рендер-пайплайна. Добавляется как ContextMenu на любой GameObject.

### Этапы логирования

| ID | Этап | Что логировать | Когда |
|----|------|---------------|-------|
| L1 | Sorting Layers | Список всех Sorting Layers, ID, имена | FullSceneBuilder Phase 02 (после создания) |
| L2 | Tilemap Setup | cellSize, cellGap, sortingLayerName, sortingOrder, sortOrder для каждого TilemapRenderer | FullSceneBuilder Phase 08 (после создания) |
| L3 | Sprite Import | PPU, alphaIsTransparency, textureType, filterMode, sprite.rect для каждого загруженного спрайта | TileMapController.EnsureTileAssets (после загрузки) |
| L4 | Tile Rendering | Кол-во отрендеренных тайлов, TileBase != null, sprite != null | TileMapController.RenderMap (после рендеринга) |
| L5 | Player Visual | sortingLayerName, sortingOrder, material.shader.name, sprite != null, sprite.bounds, sprite.pixelsPerUnit | PlayerVisual.CreateVisual (после создания) |
| L6 | Harvestable Spawn | Кол-во созданных GO, sprite != null, sortingLayerName, material.shader.name | HarvestableSpawner.SpawnHarvestables (после спавна) |
| L7 | Camera | orthographic, position.z, orthographicSize, backgroundColor | PlayerVisual.Start |
| L8 | Light2D | Наличие GlobalLight2D, тип, интенсивность, цвет | FullSceneBuilder Phase 04 |

### Формат лога

```
[L1] ====== SORTING LAYERS ======
[L1]   [0] "Default" (id=0)
[L1]   [1] "Background" (id=1)
[L1]   [2] "Terrain" (id=2)
[L1]   [3] "Objects" (id=3)
[L1]   [4] "Player" (id=4)
[L1]   [5] "UI" (id=5)
[L1] ✅ Sorting layer "Objects" exists (id=3)

[L2] ====== TILEMAP STATE ======
[L2] Terrain: sortingLayer="Terrain"(id=2), sortingOrder=0, mode=Chunk
[L2] Objects: sortingLayer="Objects"(id=3), sortingOrder=0, mode=Chunk
[L2] Grid: cellSize=(2.0, 2.0, 1.0), cellGap=(0, 0, 0)

[L5] ====== PLAYER VISUAL ======
[L5] Sprite: null=false, PPU=64, bounds=(1.0, 1.0), name="player_variant1_cultivator"
[L5] Sorting: layer="Player"(id=4), order=0
[L5] Material: shader="Universal Render Pipeline/2D/Sprite-Unlit-Default"
[L5] ✅ Player should be visible

[L6] ====== HARVESTABLE SPAWN ======
[L6] Spawned: 427 objects, 0 skipped
[L6] Sample: Harvestable_Tree_Oak_10_20 → layer="Objects"(id=3), order=5, shader="Sprite-Unlit-Default"
[L6] Sample: Harvestable_Rock_Small_30_40 → layer="Objects"(id=3), order=3, shader="Sprite-Unlit-Default"
[L6] ✅ Harvestable objects should be visible
```

### Реализация

Создать `RenderPipelineLogger.cs` в `Assets/Scripts/Core/`:

```csharp
namespace CultivationGame.Core
{
    public static class RenderPipelineLogger
    {
        public static void LogSortingLayers() { ... }
        public static void LogTilemapState() { ... }
        public static void LogSpriteImportState(string path) { ... }
        public static void LogPlayerVisualState(SpriteRenderer sr) { ... }
        public static void LogHarvestableState(List<GameObject> spawned) { ... }
        public static void LogCameraState() { ... }
        public static void LogLightState() { ... }
        public static void LogFullDiagnostics() { ... } // Все этапы
    }
}
```

Вызовы вставляются в ключевые точки:
- FullSceneBuilder.ExecuteTagsLayers → LogSortingLayers()
- FullSceneBuilder.ExecuteTilemap → LogTilemapState()
- TileMapController.EnsureTileAssets → LogSpriteImportState() для каждого спрайта
- PlayerVisual.CreateVisual → LogPlayerVisualState()
- HarvestableSpawner.SpawnHarvestables → LogHarvestableState()
- и т.д.

---

## ПРИОРИТЕТ ИСПРАВЛЕНИЙ

| Приоритет | BUG | Влияние | Сложность |
|-----------|-----|---------|-----------|
| **P0** | BUG-A: Sorting Layer "Objects" не создан | ВСЕ спрайты невидимы | Средняя (SerializedObject) |
| **P1** | BUG-B: sortOrder vs sortingOrder | Неверный порядок terrain/objects | Простой (1 строка) |
| **P2** | BUG-C: PPU=31 → PPU=30 | Белая сетка | Простой (3 строки) |
| **P3** | BUG-D: AssetDatabase.Refresh | Ненадёжная загрузка спрайтов | Простой (1 строка) |
| **P4** | BUG-E: TilemapRenderer sorting layers | Оптимизация рендеринга | Простой (2 строки) |
| **P5** | Система логирования | Диагностика | Средняя (новый файл) |

---

## ПОЧЕМУ ПРЕДЫДУЩИЕ ФИКСЫ НЕ СРАБОТАЛИ

| FIX | Почему не сработал |
|-----|-------------------|
| FIX-1 (Unlit shader) | ✅ Шейдер правильный, НО Sorting Layer "Objects" не существует → спрайт не рендерится |
| FIX-2 (sortingLayerName="Objects") | ❌ Слой "Objects" не существует → Unity 6+ игнорирует → спрайт невидим |
| FIX-3 (PPU=31) | ⚠️ Слишком малое перекрытие (1.6%), нужно PPU=30 (6.7%) |
| FIX-4 (EnsureSpriteImportSettings) | ✅ Работает, но спрайты всё равно не видны из-за Sorting Layer |
| FIX-6 (EnsureTileSpriteImportSettings) | ✅ Работает для terrain, но белая сетка из-за PPU=31 |

**Вывод:** Все предыдущие фиксы были технически корректными, но не решали **главную проблему** — отсутствие Sorting Layer "Objects". Без этого слоя НИКАКОЙ SpriteRenderer с `sortingLayerName = "Objects"` не будет рендериться в Unity 6+.

---

*Создано: 2026-04-17*
*Аудит: render pipeline v3 — корневая причина найдена*
