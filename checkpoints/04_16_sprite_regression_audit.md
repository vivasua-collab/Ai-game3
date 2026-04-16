# Чекпоинт: Анализ регрессии спрайтов после Harvest System

**Дата:** 2026-04-16 06:36:40 UTC
**Статус:** complete
**Цель:** Выяснить, почему качественные AI-спрайты деревьев/камней/руды перестали использоваться после внедрения Harvest System (чекпоинт 04_15)

---

## Описание проблемы

После внедрения Harvest System (чекпоинт 04_15 и доработки 04_16) деревья и другие объекты стали отображаться как «калечья» — низкокачественные программные спрайты вместо красивых сгенерированных AI-спрайтов.

---

## Хронология поломки

### Этап 1: ДО Harvest System — всё работало

**Рендер-цепочка (рабочая):**
1. `TileMapController.GenerateTestMap()` → генерирует карту с объектами `Tree_Oak`, `Tree_Pine`, `Rock_Small`, `OreVein` и т.д.
2. `TileMapController.RenderMap()` → для КАЖДОГО объекта вызывает `GetObjectTile(objectType)`
3. `GetObjectTile()` → маппит `Tree_Oak/Pine/Birch` → `treeTile`, `Rock_Small` → `rockSmallTile`, `OreVein` → `oreVeinTile`
4. `treeTile` создаётся через `EnsureTile(treeTile, "obj_tree", ...)` → `LoadTileSprite("obj_tree")` → **`Assets/Sprites/Tiles/obj_tree.png`** ✅
5. Спрайт загружен, рендерится через tilemap — красивый AI-спрайт

### Этап 2: Внедрение Harvest System (чекпоинт 04_15)

**Ключевое изменение в `TileMapController.RenderMap()`:**
```csharp
// Строка 986-991: пропускать harvestable-объекты
if (obj.isHarvestable)
    continue;  // ← НЕ рендерить в tilemap!
```

**Результат:** Деревья (Tree_Oak/Pine/Birch), камни (Rock_Small/Medium), руда (OreVein), кусты (Bush_Berry), трава (Herb) — **перестали рендериться через tilemap**.

Вместо этого теперь их рендерит **HarvestableSpawner** как отдельные GameObject.

### Этап 3: КРИТИЧЕСКАЯ ОШИБКА — неверный маппинг спрайтов в HarvestableSpawner

**HarvestableSpawner.GetSpritePath()** — строки 298-312:
```csharp
TileObjectType.Tree_Oak   => "Sprites/Tiles/tree_oak",    // ← ФАЙЛА НЕТ!
TileObjectType.Tree_Pine  => "Sprites/Tiles/tree_pine",   // ← ФАЙЛА НЕТ!
TileObjectType.Tree_Birch => "Sprites/Tiles/tree_birch",  // ← ФАЙЛА НЕТ!
TileObjectType.Rock_Small  => "Sprites/Tiles/rock_small",  // ← ФАЙЛА НЕТ!
TileObjectType.Rock_Medium => "Sprites/Tiles/rock_medium", // ← ФАЙЛА НЕТ!
TileObjectType.OreVein    => "Sprites/Tiles/ore_vein",    // ← ФАЙЛА НЕТ!
TileObjectType.Bush       => "Sprites/Tiles/bush",        // ← ФАЙЛА НЕТ!
TileObjectType.Bush_Berry => "Sprites/Tiles/bush_berry",  // ← ФАЙЛА НЕТ!
TileObjectType.Herb       => "Sprites/Tiles/herb",        // ← ФАЙЛА НЕТ!
```

**НИ ОДИН путь в HarvestableSpawner не совпадает с реальными файлами.** Все загрузки возвращают `null`, срабатывает fallback на `CreateProceduralSprite()` — отсюда «калечья».

### Этап 4: Для сравнения — ResourceSpawner работает ПРАВИЛЬНО

`ResourceSpawner.LoadResourceSprite()` использует префикс `obj_`: `"ore" => "obj_ore_vein"`, `"wood" => "obj_tree"` — **всё верно**.

---

## Корневая причина (итог)

| # | Проблема | Где | Влияние |
|---|----------|-----|---------|
| **R1** | **Неверный маппинг имён спрайтов** — без префикса `obj_` | `HarvestableSpawner.GetSpritePath()` строки 298-312 | 100% harvestable-объектов используют процедурный fallback |
| **R2** | **Неучёт подтипов деревьев** — отдельные пути для Tree_Oak/Pine/Birch при одном файле `obj_tree.png` | `HarvestableSpawner.GetSpritePath()` | Нет шансов загрузить даже если исправить префикс |
| **R3** | **Удаление рендера через tilemap** — `RenderMap()` пропускает `isHarvestable` | `TileMapController.RenderMap()` строка 990 | Старый рабочий путь рендера полностью отключён |
| **R4** | **Depleted-спрайты не существуют** — stump/rock_depleted/ore_depleted/bush_depleted | `HarvestableSpawner.GetDepletedSpritePath()` строки 318-332 | Depleted-состояние всегда показывает normal-спрайт |

---

## Выполненная работа — Вариант B (генерация недостающих спрайтов)

### Сгенерированные AI-спрайты (8 новых)

**Normal-спрайты (подтипы деревьев и ягодный куст):**

| Файл | Описание | Размер (AI) | Размер (обработ.) |
|------|----------|-------------|-------------------|
| `obj_tree_oak.png` | Дуб — широкая крона, толстый ствол | 135 KB | 9.4 KB |
| `obj_tree_pine.png` | Сосна — треугольная хвоя, тёмно-зелёная | 126 KB | 8.3 KB |
| `obj_tree_birch.png` | Берёза — белая кора, светло-зелёные листья | 94 KB | 8.8 KB |
| `obj_bush_berry.png` | Ягодный куст — зелёный с красными ягодами | 106 KB | 8.0 KB |

**Depleted-спрайты (исчерпанное состояние):**

| Файл | Описание | Размер (AI) | Размер (обработ.) |
|------|----------|-------------|-------------------|
| `obj_stump.png` | Пень — спил с кольцами, следы топора | 92 KB | 7.9 KB |
| `obj_rock_depleted.png` | Обломки камня — мелкие серые куски | 99 KB | 8.7 KB |
| `obj_ore_depleted.png` | Пустая жила — треснувшая порода без минералов | 77 KB | 7.5 KB |
| `obj_bush_depleted.png` | Сухой куст — коричневые засохшие ветки | 84 KB | 8.7 KB |

### Полная инвентаризация спрайтов (после генерации)

**Assets/Sprites/Tiles/ (обработанные 64×64 RGBA с прозрачностью):**

| Файл | Тип | Статус |
|------|-----|--------|
| `obj_tree.png` | Дерево (универсальное) | ✅ было |
| `obj_tree_oak.png` | Дуб | ✅ **новый** |
| `obj_tree_pine.png` | Сосна | ✅ **новый** |
| `obj_tree_birch.png` | Берёза | ✅ **новый** |
| `obj_rock_small.png` | Маленький камень | ✅ было |
| `obj_rock_medium.png` | Средний камень | ✅ было |
| `obj_ore_vein.png` | Жила руды | ✅ было |
| `obj_bush.png` | Куст | ✅ было |
| `obj_bush_berry.png` | Ягодный куст | ✅ **новый** |
| `obj_herb.png` | Трава | ✅ было |
| `obj_chest.png` | Сундук | ✅ было |
| `obj_stump.png` | Пень (depleted дерево) | ✅ **новый** |
| `obj_rock_depleted.png` | Обломки (depleted камень) | ✅ **новый** |
| `obj_ore_depleted.png` | Пустая жила (depleted руда) | ✅ **новый** |
| `obj_bush_depleted.png` | Сухой куст (depleted куст) | ✅ **новый** |
| terrain_*.png (10 шт) | Поверхности | ✅ было |

**Итого:** 15 object-спрайтов + 10 terrain-спрайтов = **25 файлов**.

---

## План внедрения (Вариант B — полный)

### Шаг 1: Исправить маппинг GetSpritePath() в HarvestableSpawner

**Файл:** `HarvestableSpawner.cs` строки 298-312

**Было (сломано):**
```csharp
TileObjectType.Tree_Oak   => "Sprites/Tiles/tree_oak",
TileObjectType.Tree_Pine  => "Sprites/Tiles/tree_pine",
TileObjectType.Tree_Birch => "Sprites/Tiles/tree_birch",
TileObjectType.Rock_Small  => "Sprites/Tiles/rock_small",
TileObjectType.Rock_Medium => "Sprites/Tiles/rock_medium",
TileObjectType.OreVein    => "Sprites/Tiles/ore_vein",
TileObjectType.Bush       => "Sprites/Tiles/bush",
TileObjectType.Bush_Berry => "Sprites/Tiles/bush_berry",
TileObjectType.Herb       => "Sprites/Tiles/herb",
```

**Стало (исправлено — с префиксом obj_ и реальными именами файлов):**
```csharp
TileObjectType.Tree_Oak   => "Sprites/Tiles/obj_tree_oak",
TileObjectType.Tree_Pine  => "Sprites/Tiles/obj_tree_pine",
TileObjectType.Tree_Birch => "Sprites/Tiles/obj_tree_birch",
TileObjectType.Rock_Small  => "Sprites/Tiles/obj_rock_small",
TileObjectType.Rock_Medium => "Sprites/Tiles/obj_rock_medium",
TileObjectType.OreVein    => "Sprites/Tiles/obj_ore_vein",
TileObjectType.Bush       => "Sprites/Tiles/obj_bush",
TileObjectType.Bush_Berry => "Sprites/Tiles/obj_bush_berry",
TileObjectType.Herb       => "Sprites/Tiles/obj_herb",
```

### Шаг 2: Исправить маппинг GetDepletedSpritePath()

**Файл:** `HarvestableSpawner.cs` строки 318-332

**Было (несуществующие файлы):**
```csharp
TileObjectType.Tree_Oak   => "Sprites/Tiles/stump",
TileObjectType.Tree_Pine  => "Sprites/Tiles/stump",
TileObjectType.Tree_Birch => "Sprites/Tiles/stump",
TileObjectType.Rock_Small  => "Sprites/Tiles/rock_depleted",
TileObjectType.Rock_Medium => "Sprites/Tiles/rock_depleted",
TileObjectType.OreVein    => "Sprites/Tiles/ore_depleted",
TileObjectType.Bush       => "Sprites/Tiles/bush_depleted",
TileObjectType.Bush_Berry => "Sprites/Tiles/bush_depleted",
```

**Стало (с префиксом obj_):**
```csharp
TileObjectType.Tree_Oak   => "Sprites/Tiles/obj_stump",
TileObjectType.Tree_Pine  => "Sprites/Tiles/obj_stump",
TileObjectType.Tree_Birch => "Sprites/Tiles/obj_stump",
TileObjectType.Rock_Small  => "Sprites/Tiles/obj_rock_depleted",
TileObjectType.Rock_Medium => "Sprites/Tiles/obj_rock_depleted",
TileObjectType.OreVein    => "Sprites/Tiles/obj_ore_depleted",
TileObjectType.Bush       => "Sprites/Tiles/obj_bush_depleted",
TileObjectType.Bush_Berry => "Sprites/Tiles/obj_bush_depleted",
```

### Шаг 3: Передать depletedSprite в Harvestable.Initialize()

**Файл:** `HarvestableSpawner.cs` строка 258-265

Сейчас depleted-спрайт загружается, но **не передаётся** в Harvestable:
```csharp
Sprite depletedSprite = LoadDepletedSprite(objData.objectType);
// depletedSprite устанавливается через Initialize если доступен  ← комментарий, но Initialize НЕ принимает depletedSprite!
```

**Нужно:**
1. Проверить `Harvestable.Initialize()` — принимает ли depletedSprite?
2. Если нет — добавить параметр `Sprite depletedSprite = null` в Initialize()
3. Сохранить depletedSprite в поле Harvestable и использовать в Deplete() для смены спрайта

### Шаг 4: Обновить TileMapController.EnsureTile() — новые подтипы деревьев

**Файл:** `TileMapController.cs` строки 152-158

Сейчас все деревья маппятся на один `treeTile`:
```csharp
treeTile = EnsureTile(treeTile, "obj_tree", false, TerrainType.Grass);
```

**Добавить подтипы:**
```csharp
treeTile = EnsureTile(treeTile, "obj_tree", false, TerrainType.Grass);
treeOakTile = EnsureTile(treeOakTile, "obj_tree_oak", false, TerrainType.Grass);
treePineTile = EnsureTile(treePineTile, "obj_tree_pine", false, TerrainType.Grass);
treeBirchTile = EnsureTile(treeBirchTile, "obj_tree_birch", false, TerrainType.Grass);
bushBerryTile = EnsureTile(bushBerryTile, "obj_bush_berry", false, TerrainType.Grass);
```

И обновить `GetObjectTile()` чтобы возвращал подтипы:
```csharp
TileObjectType.Tree_Oak => treeOakTile ?? treeTile,    // Fallback на универсальный
TileObjectType.Tree_Pine => treePineTile ?? treeTile,
TileObjectType.Tree_Birch => treeBirchTile ?? treeTile,
TileObjectType.Bush_Berry => bushBerryTile ?? bushTile,
```

### Шаг 5: Верификация в Unity Editor

1. Открыть Unity, дождаться компиляции
2. Проверить, что AssetDatabase видит все 15 `obj_*.png` в Assets/Sprites/Tiles/
3. Запустить сцену — проверить, что деревья, камни, руда отображаются AI-спрайтами
4. Проверить, что depleted-состояние показывает соответствующий depleted-спрайт
5. Проверить, что ResourcePickup-ресурсы (herb, mushroom и т.д.) НЕ сломаны

---

## Порядок выполнения (безопасный)

1. **Шаг 1** + **Шаг 2** — правка маппингов (минимальный риск, максимальный эффект)
2. **Шаг 3** — интеграция depleted-спрайтов (средний риск)
3. **Шаг 4** — подтипы в TileMapController (низкий риск, есть fallback)
4. **Шаг 5** — ручная верификация в Unity

**Критерий успеха:** После Шага 1+2 все harvestable-объекты должны отображаться качественными AI-спрайтами. После Шага 3 — depleted-состояние тоже. Шаг 4 — бонус для не-harvestable контекста.

---

*Создано: 2026-04-16 06:26:27 UTC*
*Редактировано: 2026-04-16 07:07:00 UTC*
