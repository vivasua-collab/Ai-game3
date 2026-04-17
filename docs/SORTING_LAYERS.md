# 🎨 Sorting Layers — Порядок рендеринга 2D

**Версия:** 1.0  
**Дата:** 2026-04-17  
**Проект:** Cultivation World Simulator (Unity 6.3 URP 2D)

---

## 📐 Принцип рендеринга

Unity URP 2D рендерит спрайты **снизу вверх** по Sorting Layers.  
Слой с **меньшим индексом** рендерится **позади**.  
Внутри одного слоя порядок определяется `sortingOrder` (меньше = позади).

```
Камера (Z = -10, смотрит в +Z)
  │
  ▼ Порядок рендеринга (снизу → вверх, позади → впереди):

  ┌─────────────────────────────────────────────────────┐
  │  [0] Default     — НЕ ИСПОЛЬЗОВАТЬ! Fallback        │
  │  [1] Background  — Фоновые декорации                │
  │  [2] Terrain     — Поверхность земли/воды/лавы      │
  │  [3] Objects     — Деревья, камни, ресурсы, дроп    │
  │  [4] Player      — Игрок и тень                     │
  │  [5] UI          — UI элементы в мировом простр.     │
  └─────────────────────────────────────────────────────┘
```

---

## 📋 Полная таблица: Слой → Объекты → sortingOrder

### Слой [0] Default — ❌ ЗАПРЕЩЁН для использования

| Объект | sortingOrder | Примечание |
|--------|-------------|------------|
| — | — | Если SpriteRenderer/TilemapRenderer не назначил sortingLayerName, Unity ставит "Default" → рендер поверх всех остальных! Это **баг** |

> **⚠️ КРИТИЧЕСКИ:** Любой рендерер на "Default" слое рендерится **ПОСЛЕ** всех остальных слоёв (поверх Player, UI). Если в логах `[RPL]-L9` видны объекты на Default — это баг, который нужно немедленно исправить.

### Слой [1] Background — Фоновые декорации

| Объект | Компонент | sortingOrder | Источник |
|--------|-----------|-------------|----------|
| (пока не используется) | — | — | — |

> Зарезервирован для фоновых параллакс-слоёв, неба, далёких гор.

### Слой [2] Terrain — Поверхность

| Объект | Компонент | sortingOrder | Спрайт | Источник |
|--------|-----------|-------------|--------|----------|
| TerrainTilemap | TilemapRenderer | 0 | Процедурный (Sprite.Create) | TileMapController.cs |
| Grass | GameTile | — | 64×64 PPU=32 Point | ForceProceduralTerrainTile |
| Dirt | GameTile | — | 64×64 PPU=32 Point | ForceProceduralTerrainTile |
| Stone | GameTile | — | 64×64 PPU=32 Point | ForceProceduralTerrainTile |
| Water_Shallow | GameTile | — | 64×64 PPU=32 Point (α=0.8) | ForceProceduralTerrainTile |
| Water_Deep | GameTile | — | 64×64 PPU=32 Point (α=0.9) | ForceProceduralTerrainTile |
| Sand | GameTile | — | 64×64 PPU=32 Point | ForceProceduralTerrainTile |
| Void | GameTile | — | 64×64 PPU=32 Point | ForceProceduralTerrainTile |
| Snow | GameTile | — | 64×64 PPU=32 Point | ForceProceduralTerrainTile |
| Ice | GameTile | — | 64×64 PPU=32 Point | ForceProceduralTerrainTile |
| Lava | GameTile | — | 64×64 PPU=32 Point | ForceProceduralTerrainTile |

> **ВСЕ terrain-спрайты — процедурные** (Sprite.Create в рантайме). PNG через Import Pipeline вызывают белую сетку между тайлами. См. TileMapController.EnsureTileAssets().

### Слой [3] Objects — Объекты окружения, ресурсы, дроп

| Объект | Компонент | sortingOrder | Спрайт | Источник |
|--------|-----------|-------------|--------|----------|
| ObjectTilemap | TilemapRenderer | 0 | PNG/процедурный | TileMapController.cs |
| OverlayTilemap | TilemapRenderer | 10 | — | TileMapController.cs |
| Herb (harvestable) | SpriteRenderer | 1 | PNG | HarvestableSpawner.cs |
| Bush (harvestable) | SpriteRenderer | 2 | PNG | HarvestableSpawner.cs |
| Rock_Small (harvestable) | SpriteRenderer | 3 | PNG | HarvestableSpawner.cs |
| Rock_Medium (harvestable) | SpriteRenderer | 3 | PNG | HarvestableSpawner.cs |
| OreVein (harvestable) | SpriteRenderer | 4 | PNG | HarvestableSpawner.cs |
| Tree_Oak (harvestable) | SpriteRenderer | 5 | PNG | HarvestableSpawner.cs |
| Tree_Pine (harvestable) | SpriteRenderer | 5 | PNG | HarvestableSpawner.cs |
| Tree_Birch (harvestable) | SpriteRenderer | 5 | PNG | HarvestableSpawner.cs |
| Resource pickups | SpriteRenderer | 5 | PNG | ResourceSpawner.cs |
| Drop items | SpriteRenderer | 5 | PNG | DestructibleObjectController.cs |

> **⚠️ ИЗВЕСТНАЯ ПРОБЛЕМА:** HarvestableSpawner назначает sortingOrder по **типу объекта** (Herb=1, Tree=5), а не по Y-позиции. Это означает, что ВСЕ деревья рендерятся поверх ВСЕХ камней, независимо от их позиции на экране. Правильное решение — Y-сортировка: `order = -Y * 10`. Это задача на следующий этап.

### Слой [4] Player — Игрок

| Объект | Компонент | sortingOrder | Спрайт | Источник |
|--------|-----------|-------------|--------|----------|
| Player Visual | SpriteRenderer | 0 | AI/PNG (128×128 PPU=64) | PlayerVisual.cs |
| Player Shadow | SpriteRenderer | -1 | Программный круг (64×64 PPU=64) | PlayerVisual.cs |

> **Fallback:** Если слой "Player" не существует или его индекс меньше Terrain/Objects, PlayerVisual переводит спрайт на слой "Objects" с sortingOrder=100. Это гарантирует, что игрок виден поверх terrain и objects.

### Слой [5] UI — UI элементы

| Объект | Компонент | sortingOrder | Источник |
|--------|-----------|-------------|----------|
| Formation UI | SpriteRenderer | 1000 | FormationUI.cs |
| Formation UI prefabs | SpriteRenderer | 1000 | FormationUIPrefabsGenerator.cs |
| Harvest Feedback | SpriteRenderer | 100 | HarvestFeedbackUI.cs |

---

## 🔧 Где настраиваются Sorting Layers

### Создание слоёв (Editor-only)

| Место | Файл | Метод |
|-------|------|-------|
| FullSceneBuilder | FullSceneBuilder.cs | `EnsureSortingLayers()` — создаёт слои через TagManager |
| TileMapController | TileMapController.cs | `EnsureSortingLayersExist()` — создаёт недостающие |

### Проверка ПОРЯДКА слоёв (NEW — FIX-SORT)

| Место | Файл | Метод |
|-------|------|-------|
| TileMapController | TileMapController.cs | `EnsureSortingLayerOrder()` — проверяет и исправляет порядок |
| PlayerVisual | PlayerVisual.cs | `EnsureCorrectSortingLayer()` — проверяет, что Player выше Objects |

### Назначение слоёв рендерерам

| Компонент | Файл | Метод | Слой |
|-----------|------|-------|------|
| Terrain TilemapRenderer | TileMapController.cs | `FixTilemapSortingLayers()` | "Terrain" |
| Objects TilemapRenderer | TileMapController.cs | `FixTilemapSortingLayers()` | "Objects" |
| Overlay TilemapRenderer | TileMapController.cs | `FixTilemapSortingLayers()` | "Objects" (order=10) |
| **ЛЮБОЙ** TilemapRenderer | TileMapController.cs | `FixAllTilemapRenderers()` | по имени GO |
| Player SpriteRenderer | PlayerVisual.cs | `CreateVisual()` + `EnsureCorrectSortingLayer()` | "Player" |
| Harvestable SpriteRenderer | HarvestableSpawner.cs | `CreateHarvestableSprite()` | "Objects" |
| Resource SpriteRenderer | ResourceSpawner.cs | `CreateResourceSprite()` | "Objects" |
| Drop SpriteRenderer | DestructibleObjectController.cs | `CreateDropSprite()` | "Objects" |

---

## 🐛 Диагностика: RenderPipelineLogger

| Уровень | Метод | Что проверяет |
|---------|-------|---------------|
| L1 | `LogSortingLayers()` | Существование слоёв |
| L2 | `LogTilemapState()` | TilemapRenderer: слой, order, кол-во тайлов |
| L3 | `LogSpriteImportState()` | PPU, alphaIsTransparency |
| L4 | `LogTileRendered()` | Кол-во terrain/object тайлов |
| L5 | `LogPlayerVisualState()` | Player SpriteRenderer: слой, шейдер |
| L6 | `LogHarvestableState()` | Harvestable SpriteRenderer: слой, order |
| L7 | `LogCameraState()` | Камера: ortho, position, size |
| L8 | `LogLightState()` | GlobalLight2D: тип, интенсивность |
| **L9** | **`LogAllRendererState()`** | **Все рендереры: порядок слоёв, Default-баг, Player видимость** |

---

## 🚨 Типичные баги и решения

### Баг: Terrain рендерится поверх Player

**Причина:** Sorting Layer "Player" имеет индекс меньше, чем "Terrain"  
**Решение:** `TileMapController.EnsureSortingLayerOrder()` переставляет слои  
**Fallback:** `PlayerVisual.EnsureCorrectSortingLayer()` переводит Player на "Objects" с order=100

### Баг: Объекты на "Default" слое

**Причина:** SpriteRenderer/TilemapRenderer не назначил sortingLayerName  
**Решение:** `TileMapController.FixAllTilemapRenderers()` ищет ВСЕ TilemapRenderer по типу  
**Диагностика:** `RenderPipelineLogger.LogAllRendererState()` показывает объекты на Default

### Баг: Спрайт поверхности (terrain) поверх спрайтов объектов

**Причина:** TilemapRenderer terrain на "Default" (id=0), а не на "Terrain" (id=2)  
**Решение:** `FixTilemapSortingLayers()` + `FixAllTilemapRenderers()` принудительно назначают "Terrain"

### Баг: Белая сетка между terrain-тайлами

**Причина:** PNG-спрайты через Unity Import Pipeline имеют субпиксельные зазоры  
**Решение:** Все terrain-спрайты — процедурные (Sprite.Create), PPU=32, FilterMode.Point

---

## 📐 Формат спрайтов

| Тип | Размер | PPU | Filter | Alpha | Источник |
|------|--------|-----|--------|-------|----------|
| Terrain (процедурный) | 64×64 | 32 | Point | Сплошная заливка | Sprite.Create |
| Objects (PNG) | 64×64 | 32 | Point | Прозрачный фон | AssetDatabase |
| Player (PNG) | 128×128 | 64 | Bilinear | RGBA с прозрачностью | AssetDatabase |
| Player Fallback | 64×64 | 64 | Bilinear | RGBA | Sprite.Create |

---

## 📎 Связанные документы

- [ARCHITECTURE.md](./ARCHITECTURE.md) — Общая архитектура
- [TILE_SYSTEM.md](./TILE_SYSTEM.md) — Тайловая система
- [UNITY_DOCS_LINKS.md](./UNITY_DOCS_LINKS.md) — Документация Unity 6.3

---

*Создано: 2026-04-17 12:50 UTC*  
*Проект: Cultivation World Simulator*
